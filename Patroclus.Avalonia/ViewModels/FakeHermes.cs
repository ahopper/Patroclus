using Avalonia.Threading;
using ReactiveUI;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
//using System.Windows.Data;
using System.Windows.Input;

namespace Patroclus.Avalonia.ViewModels
{
    public class FakeHermes : FakeRadio
    {
        private UdpClient client;
        private Thread handleCommsThread;

        public int port { get; set; } //Port for the Client to use
        public byte boardID { get; set; }
        public byte hermesCodeVersion { get; set; }
        
        ConcurrentQueue<receivedPacket> msgQueue = new ConcurrentQueue<receivedPacket>();
        
        IPEndPoint ClientIpEndPoint;

        const byte sync = 0x7f;
        const int max24int = 0x7fffff;
        const int min24int = -0x800000;


        byte[] databuf = new byte[1024 + 8];
        byte[] databufBs = new byte[1024 + 8];
        uint seqNo = 0;
        uint seqNoBs = 0;
        uint txseqNo = 0;
  
        int progSeqNo = 0;
        double timebase = 0.0;
        
        DateTime startTime;
        bool running = false;
        bool bsrunning = false;
        volatile bool txing = false;

        Stopwatch stopwatch = new Stopwatch();
        
        private static int[] bandwidths={48000,96000,192000,384000};

        volatile bool closing = false;

        public FakeHermes()
        {
          
      //      BindingOperations.CollectionRegistering += BindingOperations_CollectionRegistering;
           
            // make sine wave that fits perfectly in 512 samples so it works for all bandscope lengths
            // TODO make adjustable generator for this
            for(int i=0;i<512;i++)
            {
                double v = ((double)i) * Math.PI/2; 
                Int16 val =  (Int16)Math.Round(Math.Sin(v) * 20000);
                databufBs[9 + i * 2] = (byte)(val >> 8);
                databufBs[8 + i * 2] = (byte)(val & 0xff);
                
            }
            txIQ = new double[63 * 32 * 2];
            txAudio = new double[63 * 8 * 2];
            stopwatch.Start();
        }

  //      void BindingOperations_CollectionRegistering(object sender, CollectionRegisteringEventArgs e)
  //      {
  //          if(e.Collection==receivers)BindingOperations.EnableCollectionSynchronization(receivers, _receiversLock);
  //          else if(e.Collection==ccbits)BindingOperations.EnableCollectionSynchronization(ccbits, _ccbitsLock);
  //      }


        private object _ccbitsLock = new object();
        private ObservableCollection<uint> _ccbits = new ObservableCollection<uint>(new List<uint>() { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 });
        public ObservableCollection<uint> ccbits
        {
            get { return _ccbits; }
            set { this.RaiseAndSetIfChanged(ref _ccbits, value); }
        }

        private object _receiversLock = new object();
        private ObservableCollection<receiver> _receivers = new ObservableCollection<receiver>();
        public ObservableCollection<receiver> receivers
        {
            get{return _receivers;}
            set{this.RaiseAndSetIfChanged(ref _receivers,value); }
        }

        private int _bandwidth = 0;
        public int bandwidth
        {
            get { return _bandwidth; }
            set { this.RaiseAndSetIfChanged(ref _bandwidth, value); }
        }

        private int _txNCO = 0;
        public int txNCO
        {
            get{ return _txNCO;}
            set{ this.RaiseAndSetIfChanged(ref _txNCO,value); }
        }

        private bool _duplex=false;
        public bool duplex
        {
            get { return _duplex; }
            set { this.RaiseAndSetIfChanged(ref _duplex, value); }
        }
        private bool _adc1clip = false;
        public bool adc1clip
        {
            get { return _adc1clip; }
            set { this.RaiseAndSetIfChanged(ref _adc1clip, value); }
        }

        private string _status ="Off";
        public string status
        {
            get { return _status; }
            set { this.RaiseAndSetIfChanged(ref _status, value); }
        }
        private string _log = "";
        public string log
        {
            get { return _log; }
            set { this.RaiseAndSetIfChanged(ref _log, value); }
        }
        
        private int _packetsSent = 0;
        public int packetsSent
        {
            get { return _packetsSent; }
            set { this.RaiseAndSetIfChanged(ref _packetsSent, value); }
        }
        private int _packetsReceived = 0;
        public int packetsReceived
        {
            get { return _packetsReceived; }
            set { this.RaiseAndSetIfChanged(ref _packetsReceived, value); }
        }
        private int _seqErrors = 0;
        public int seqErrors
        {
            get { return _seqErrors; }
            set { this.RaiseAndSetIfChanged(ref _seqErrors, value); }
        }
        private int _clockError = 1000;
        public int clockError
        {
            get { return _clockError; }
            set 
            {
                //prevent system trying to correct timing from original start time

                resetTransmission();
                this.RaiseAndSetIfChanged(ref _clockError, value); 
            }
        }
        public void start()
        {
            client = new UdpClient(port);

            const int SIO_UDP_CONNRESET = -1744830452;
            byte[] inValue = new byte[] { 0 };
            byte[] outValue = new byte[] { 0 };
            client.Client.IOControl(SIO_UDP_CONNRESET, inValue, outValue);

            ///client.BeginReceive(new AsyncCallback(incomming), null);
            readUDP(client);

            handleCommsThread = new Thread(handleComms);
            handleCommsThread.IsBackground = true;
            handleCommsThread.Priority = ThreadPriority.AboveNormal;
            handleCommsThread.Start();
       
        }
        public override void Stop()
        {
            //  handleCommsThread.Abort();
            closing = true;

            client.Close();
            
            base.Stop();
        }
        long actualPacketCount = 0;
        int txReturnState = 0;
        int bandScopeHoldoff = 0;
        void handleComms()
        {
            while (!closing)
            {
                while (!msgQueue.IsEmpty)
                {
                    receivedPacket packet;
                    if (msgQueue.TryDequeue(out packet)) handlePacket(packet);
                }
                //send any output
                if (running)
                {
                    adc1clip = false;
                    int channels = receivers.Count();
                    int stride = channels * 6 + 2;
                    int nSamples = (512 - 8) / stride;
                    double timeStep = 1.0 / bandwidth;


                    //calculate number of packets to maintain sync
                    DateTime now = DateTime.Now;
                    long totalTime = (long)(now - startTime).TotalMilliseconds;
                    long nPacketsCalculated = 1+bandwidth / (nSamples * 2) * totalTime / clockError;

                    long packetsToSend = nPacketsCalculated - actualPacketCount;

                    databuf[0] = 0xef;
                    databuf[1] = 0xfe;
                    databuf[2] = 0x01;

                    databuf[3] = 0x06;

                    for (int i = 0; i < packetsToSend; i++)
                    {
                        databuf[4] = (byte)(seqNo >> 24);
                        databuf[5] = (byte)((seqNo >> 16) & 0xff);
                        databuf[6] = (byte)((seqNo >> 8) & 0xff);
                        databuf[7] = (byte)(seqNo & 0xff);

                        int bufStart = 8;
                        for (int block = 0; block < 2; block++)
                        {
                            if (txing)
                            {
                                // send back received iq samples
                                // really need to do something better
                                // for bandwidths other than 48k
                                int bidx=bufStart+8;
                                for(int t=0;t<nSamples;t++)
                                {
                                    int txq = (int)txIQ[txIQReadIdx]<<8;
                                    int txi = -(int)txIQ[txIQReadIdx+1]<<8;
                                    txReturnState++;
                                    if(txReturnState==bandwidth/48000)
                                    {
                                        txReturnState=0;
                                        txIQReadIdx+=2;
                                        if (txIQReadIdx >= txIQ.Length) txIQReadIdx = 0;
                                    
                                    }

                                    for (int c = 0; c < channels; c++)
                                    {
                                        databuf[bidx++] = (byte)(txi >> 16);
                                        databuf[bidx++] = (byte)((txi >> 8) & 0xff);
                                        databuf[bidx++] = (byte)((txi) & 0xff);
                                        databuf[bidx++] = (byte)(txq >> 16);
                                        databuf[bidx++] = (byte)((txq >> 8) & 0xff);
                                        databuf[bidx++] = (byte)((txq) & 0xff);
                                    }
                                    bidx += 2;
                                }
                            }
                            else
                            {
                                for (int c = 0; c < channels; c++)
                                {
                                    GenerateSignal(databuf, bufStart + 8 + c * 6, stride, nSamples, timebase, timeStep, receivers[c]);
                                }
                            }

                            GenerateComandControl(databuf, bufStart, 0);
                            timebase += nSamples * timeStep;
                            bufStart += 512;
                        }

                        client.Send(databuf, databuf.Length, ClientIpEndPoint);
                        seqNo++;
                        actualPacketCount++;
                        packetsSent++;
                    }
                }
                //   long nBsPacketsCalculated = 48000 / 512 * totalTime / 1000;
                //   uint BsPacketsToSend = ((uint)nBsPacketsCalculated) - seqNoBs;
                //   for (int i = 0; i < BsPacketsToSend; i++)
                if (bsrunning && bandScopeHoldoff++ > 10)
                {
                    bandScopeHoldoff = 0;
                    sendBandscope();
                }
                Thread.Sleep(1);
            }
        }

        void sendBandscope()
        {
            databufBs[0] = 0xef;
            databufBs[1] = 0xfe;
            databufBs[2] = 0x01;

            databufBs[3] = 0x04;

                    
            databufBs[4] = (byte)(seqNoBs >> 24);
            databufBs[5] = (byte)((seqNoBs >> 16) & 0xff);
            databufBs[6] = (byte)((seqNoBs >> 8) & 0xff);
            databufBs[7] = (byte)(seqNoBs & 0xff);

            seqNoBs++;
            client.Send(databufBs, databufBs.Length, ClientIpEndPoint);

        }
        void GenerateComandControl(byte[] databuf, int startOffset, int seq)
        {
            //todo create all message types
            databuf[startOffset] = sync;
            databuf[startOffset + 1] = sync;
            databuf[startOffset + 2] = sync;

            databuf[startOffset + 3] = 0;
            databuf[startOffset + 4] = adc1clip ? (byte)1 : (byte)0;
            databuf[startOffset + 5] = 0;
            databuf[startOffset + 6] = 0;
            databuf[startOffset + 7] = hermesCodeVersion;

        }
        void GenerateSignal(byte[] outputbuf, int startOffset, int stride, int nSamples, double timebase,double timestep,receiver rx)
        {
            double[] mixbuf = new double[nSamples * 2];//auto cleared to 0
            // combine all signal generatiors
            rx.GenerateSignal(mixbuf, nSamples, timebase, timestep);
            //convert to formatted 24 bit
            int idx = 0;
            for (int i = 0; i < nSamples * stride; i += stride)
            {
                int iamp = (int)Math.Round(mixbuf[idx++]*0x7fffff);
                if (iamp > max24int)
                {
                    iamp = max24int;
                    adc1clip = true;
                }
                else if (iamp < min24int)
                {
                    iamp = min24int;
                    adc1clip = true;
                }
                databuf[startOffset + i] = (byte)(iamp >> 16);
                databuf[startOffset + i + 1] = (byte)((iamp >> 8) & 0xff);
                databuf[startOffset + i + 2] = (byte)((iamp) & 0xff);

                iamp = (int)Math.Round(mixbuf[idx++]*0x7fffff);

                if (iamp > max24int)
                {
                    iamp = max24int;
                    adc1clip = true;
                }
                else if (iamp < min24int)
                {
                    iamp = min24int;
                    adc1clip = true;
                }

                databuf[startOffset + i + 3] = (byte)(iamp >> 16);
                databuf[startOffset + i + 4] = (byte)((iamp >> 8) & 0xff);
                databuf[startOffset + i + 5] = (byte)(iamp & 0xff);
            }
           timebase += nSamples*timestep;
                
        }

        ConcurrentStack<receivedPacket> rxBuffers = new ConcurrentStack<receivedPacket>();
        private void readUDP(UdpClient udpClient)
        {
            
            Task.Run(async () =>
            {
                while (!closing)
                {

                    //todo reuse buffers 
                    receivedPacket buff = null;
                    if(!rxBuffers.TryPop(out buff))
                    { 
                        buff= new receivedPacket() { received = new byte[1032] };
                    }

                    //  var received = await udpClient.Client.ReceiveAsync(buff, SocketFlags.None );
                    var remEndPoint = new IPEndPoint(IPAddress.Any,0);
                    var received = await udpClient.Client.ReceiveMessageFromAsync(buff.received, SocketFlags.None, remEndPoint);


                    //     var received = await udpClient.ReceiveAsync();
                    buff.endPoint = (IPEndPoint)received.RemoteEndPoint;
                    msgQueue.Enqueue(buff);
                }
            });
        }

        private void incomming(IAsyncResult res)
        {
            IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, port);
            byte[] received = null;
            packetsReceived++;
            try
            {
                received = client.EndReceive(res, ref RemoteIpEndPoint);
            }
            catch (Exception )
            {

            }
            try
            {
                client.BeginReceive(new AsyncCallback(incomming), null);
                if (received != null)
                {
                    msgQueue.Enqueue(new receivedPacket() { received = received, endPoint = RemoteIpEndPoint, timeStamp = stopwatch.Elapsed });
                }
            }
            catch (Exception )
            {
            }
        }
        //System.IO.StreamWriter logf = null;
        int logLen = 20;

        public void handlePacket(receivedPacket packet)
        {
            byte[] received = packet.received;
            /*
            if (logLen > 0)
            {
                if(logf==null)logf=System.IO.File.CreateText("hllog.txt");

                logf.Write(received.Length + ", ");
                for (int i = 0; i < 16; i++) logf.Write(received[i] + ", ");
                if (received.Length > 700) for (int i = 0; i < 5; i++) logf.Write(received[512 + 11+i] + ", ");

                logf.Write("\r\n");

                logLen--;
                if (logLen == 0) logf.Dispose();
            }
            */
            if (received[2] == 2)
            {
                //discovery
                byte[] response = new byte[60];
                response[0] = 0xef;
                response[1] = 0xfe;
                response[2] = 0x02;
                //add mac address - kiss does not like blank one
                response[3] = 0x00;
                response[4] = 0x00;
                response[5] = 0x00;
                response[6] = 0x00;
                response[7] = 0x00;
                response[8] = 0x01;

                response[9] = hermesCodeVersion;//code version
                response[10] = boardID;//board type
                
                status = "Discovered";
                seqNo = 1;
                seqNoBs = 1;
                client.Send(response, response.Length, packet.endPoint);
                packetsSent++;

            }
            else if (received[2] == 4)
            {
                if (received[3] > 0)
                {
                    ClientIpEndPoint = packet.endPoint;

                    int channels = receivers.Count();
                    int stride = channels * 6 + 2;
                    int nSamples = (512 - 8) / stride;
                    
                    resetTransmission();    
                    running = true;
                    if ((received[3] & 0x02) != 0) bsrunning = true;
                    else bsrunning = false;
                    status = "Running";
       //             Console.WriteLine("Start "+packet.timeStamp.TotalMilliseconds);
                }
                else
                {
                    running = false;
                    bsrunning = false;
                    status = "Off";

                }
            }
            else if (received[2] == 1 && received[3] == 2)
            {
                uint seq = ((uint)received[4] << 24) | ((uint)received[5] << 16) | ((uint)received[6] << 8) | ((uint)received[7]);
                if(seq!=txseqNo+1)
                {
                    seqErrors++;
                }
                
                txseqNo = seq;
                //standard data packet
          //      Console.WriteLine(packet.timeStamp.TotalMilliseconds);

                handleCommandControl(received[11], received[12], received[13], received[14], received[15]);
                handleCommandControl(received[512 + 11], received[512 + 12], received[512 + 13], received[512 + 14], received[512 + 15]);

                handleTXIQandAudio(received, 16, 63);
                handleTXIQandAudio(received, 512+16, 63);
            }
            else if(received[2]==3)
            {
                if(received[3]==1)//program
                {
                    byte[] response = new byte[60];
                    response[0] = 0xef;
                    response[1] = 0xfe;
                    response[2] = 0x04;
                    response[3] = 0x00;
                    response[4] = 0x00;
                    response[5] = 0x00;
                    response[6] = 0x00;
                    response[7] = 0x00;
                    response[8] = 0x01;

                    status = "program " + progSeqNo++;
                    Thread.Sleep(50);
                    client.Send(response, response.Length, packet.endPoint);
                    packetsSent++;
                }
                else if(received[3]==2)//erase
                {
                    byte[] response = new byte[60];
                    response[0] = 0xef;
                    response[1] = 0xfe;
                    response[2] = 0x03;
                    response[3] = 0x00;
                    response[4] = 0x00;
                    response[5] = 0x00;
                    response[6] = 0x00;
                    response[7] = 0x00;
                    response[8] = 0x01;
                    progSeqNo = 0;
                    status = "erase";
                    Thread.Sleep(1000);
             
                    client.Send(response, response.Length, packet.endPoint);
                    packetsSent++;
                }
            }
            else { }

            rxBuffers.Push(packet);
        }
        void resetTransmission()
        {
            startTime = DateTime.Now;
            actualPacketCount = 0;       
        }
        public void handleCommandControl(byte c0,byte c1, byte c2, byte c3, byte c4)
        {

       //     Console.WriteLine(string.Format("{0}\t{1}\t{2}\t{3}\t{4}", c0, c1, c2, c3, c4));

            int index=c0>>1;
            if (index < ccbits.Count)
            {
                uint newVal= ((uint)c1 << 24) | ((uint)c2 << 16) | ((uint)c3 << 8) | (uint)c4;

                if (newVal != ccbits[index])
                {
                    Dispatcher.UIThread.InvokeAsync(new Action(() => { ccbits[index] = newVal; }));
                }
//   ccbits[index] = ((uint)c1 << 24) | ((uint)c2 << 16) | ((uint)c3 << 8) | (uint)c4;
            }
            bool tx = ((c0 & 1) == 1);
            if (tx != txing)
            {
                txing = tx;
                int temptxIQReadIdx = txIQIdx - txIQ.Length / 2;
                if (temptxIQReadIdx < 0) temptxIQReadIdx += txIQ.Length;
                txIQReadIdx = temptxIQReadIdx;
            }
            switch(c0 & 0x7e)
            {
                case 0:
                    int bw = bandwidths[c1 & 0x03];
                    if(bandwidth!=bw && running)
                    {
                        resetTransmission();            
                    }
                    bandwidth = bw;
                    duplex = (c4 & 0x4) != 0;
                    int nReceivers = ((c4 >> 3) & 0x07) + 1;
                    if(boardID==6)
                    {
                        nReceivers = ((c4 >> 3) & 0x1f) + 1;
                    }
                    
                    if(nReceivers!=receivers.Count)
                    {
                     //   lock (_receiversLock)
                     //   {
                            Dispatcher.UIThread.InvokeAsync(new Action(() => { 
                                while (receivers.Count > nReceivers) receivers.Remove(receivers.Last());
                                while (receivers.Count < nReceivers) receivers.Add(new receiver("RX" + (receivers.Count + 1)));
                            }));
                        //   }

                        resetTransmission();    
                    }
                    break;
                case 2: int t= (((int)c1) << 24) + (((int)c2) << 16) + (((int)c3) << 8) + (int)c4;            
                        txNCO = t;
                        if (!duplex)
                        {
                            receivers[0].vfo = txNCO;
                            receivers[0].generators[0].SetDefaults( receivers[0].vfo);
                            receivers[0].generators[1].SetDefaults( receivers[0].vfo + 10000);
                  
                        }
                    break;
                case 4: 
                case 6: 
                case 8: 
                case 10:
                case 12:
                case 14:
                case 16: int rxIdx = (c0 >> 1) - 2;
                    if (receivers != null && receivers.Count > rxIdx)
                    {
                        receivers[rxIdx].vfo = (((int)c1) << 24) + (((int)c2) << 16) + (((int)c3) << 8) + (int)c4;
                        receivers[rxIdx].generators[0].SetDefaults(receivers[rxIdx].vfo);
                        receivers[rxIdx].generators[1].SetDefaults(receivers[rxIdx].vfo + 10000);
                    }
                    break;
                case 18:
                case 20:
                case 22:
                case 24:
                case 26:
                case 28:
                case 30:
                case 32:
                case 34:
                    break;

// hermes lite experimental extra receivers
                case 36:
                case 38:
                case 40:
                case 42:
                case 44:
                case 46:
                case 48:
                case 50:
                case 52:
                case 54:
                case 56:
                case 58:
                case 60:
                case 62:
                case 64:
                case 66:
                case 68:
                case 70:
                case 72:
                case 74:
                case 76:
                case 78:
                case 80:
                case 82:
                case 84:

                    if (boardID == 6)
                    {
                        int rxIdx2 = (c0 >> 1) - 11;
                        if (receivers != null && receivers.Count > rxIdx2)
                        {
                            receivers[rxIdx2].vfo = (((int)c1) << 24) + (((int)c2) << 16) + (((int)c3) << 8) + (int)c4;
                            receivers[rxIdx2].generators[0].SetDefaults(receivers[rxIdx2].vfo);
                            receivers[rxIdx2].generators[1].SetDefaults(receivers[rxIdx2].vfo + 10000);
                        }
                    }
                    break;

                case 86: break;
                case 120: log += string.Format("I2C1 {0:x2} {1:x2} {2:x2} {3:x2} {4:x2}\r\n", c0, c1, c2, c3, c4); break;
                case 122: log += string.Format("I2C2 {0:x2} {1:x2} {2:x2} {3:x2} {4:x2}\r\n", c0, c1, c2, c3, c4); break; 


 //               default: Console.WriteLine(string.Format("Unhandled Control message {0}\t{1}\t{2}\t{3}\t{4}", c0, c1, c2, c3, c4)); break;

            }
        }
        double[] txAudio;
        int txAudioIdx = 0;
        double[] txIQ;
        int txIQIdx = 0;
        int txIQReadIdx = 0;
        public void handleTXIQandAudio(byte[] received, int start, int length)
        {
            // L1 – Bits  15-8 of Left audio sample 
            // L0 – Bits   7-0 of Left audio sample 
            // R1 – Bits  15-8 of Right audio sample 
            // R0 – Bits   7-0 of Right audio sample 
            // I1 – Bits  15-8 of I sample 
            // I0 – Bits   7-0 of I sample
            // Q1 - Bits  15-8 of Q sample 
            // Q0 - Bits   7-0 of Q sample
            
            for(int i=start;i<length*8+start;i+=8)
            {
                txAudio[txAudioIdx++] = (double)(short)((received[i] <<8) | (received[i + 1]));
                txAudio[txAudioIdx++] = (double)(short)((received[i + 2] << 8) | (received[i + 3]));
                txIQ[txIQIdx++] = (double)(short)((received[i + 4] << 8) | (received[i + 5]));
                txIQ[txIQIdx++] = (double)(short)((received[i + 6] << 8) | (received[i + 7]));

                if (txAudioIdx >= txAudio.Length) txAudioIdx = 0;
                if (txIQIdx >= txIQ.Length) txIQIdx = 0;

            }
        }   
    }
    
    
    public class receivedPacket
    {
        public TimeSpan timeStamp { get; set; }
        public IPEndPoint endPoint { get; set; }
        public byte[] received { get; set; }
    }
}
