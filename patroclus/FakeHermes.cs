using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace fakehermes
{
    public class FakeHermes : BindableBase
    {
        private UdpClient client;
        public int port { get; set; } //Port for the Client to use

        System.Windows.Threading.DispatcherTimer timer = new System.Windows.Threading.DispatcherTimer();
        ConcurrentQueue<receivedPacket> msgQueue = new ConcurrentQueue<receivedPacket>();
        
        IPEndPoint ClientIpEndPoint;

        const byte sync = 0x7f;
        const int max24int = 0x7fffff;
        const int min24int = -0x800000;


        byte[] databuf = new byte[1024 + 8];
        uint seqNo = 0;
        double timebase = 0.0;
        byte hermesCodeVersion = 30;
        DateTime startTime;
        bool running = false;

        private static int[] bandwidths={48000,96000,192000,384000};

        private ObservableCollection<receiver> _receivers = new ObservableCollection<receiver>();
        public ObservableCollection<receiver> receivers
        {
            get{return _receivers;}
            set{SetProperty(ref _receivers,value); }
        }

        private int _bandwidth = 0;
        public int bandwidth
        {
            get { return _bandwidth; }
            set { SetProperty(ref _bandwidth, value); }
        }

        private int _txNCO = 0;
        public int txNCO
        {
            get{ return _txNCO;}
            set{ SetProperty(ref _txNCO,value); }
        }

        private bool _duplex=false;
        public bool duplex
        {
            get { return _duplex; }
            set { SetProperty(ref _duplex, value); }
        }
        private bool _adc1clip = false;
        public bool adc1clip
        {
            get { return _adc1clip; }
            set { SetProperty(ref _adc1clip, value); }
        }

        private string _status ="Off";
        public string status
        {
            get { return _status; }
            set { SetProperty(ref _status, value); }
        }
        private int _packetsSent = 0;
        public int packetsSent
        {
            get { return _packetsSent; }
            set { SetProperty(ref _packetsSent, value); }
        }
        private int _packetsReceived = 0;
        public int packetsReceived
        {
            get { return _packetsReceived; }
            set { SetProperty(ref _packetsReceived, value); }
        }

        public void start()
        {
            client = new UdpClient(port);

            const int SIO_UDP_CONNRESET = -1744830452;
            byte[] inValue = new byte[] { 0 };
            byte[] outValue = new byte[] { 0 };
            client.Client.IOControl(SIO_UDP_CONNRESET, inValue, outValue);

            client.BeginReceive(new AsyncCallback(incomming), null);

            timer.Interval = TimeSpan.FromMilliseconds(10);
            timer.Tick += new EventHandler(timer_Tick);
            timer.Start();
       
        }

        long actualPacketCount = 0;
        void timer_Tick(object sender, EventArgs e)
        {
            //deal with any udp packets in main thread
            while(!msgQueue.IsEmpty)
            {
                receivedPacket packet;
                if(msgQueue.TryDequeue(out packet)) handlePacket(packet);    
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
                long nPacketsCalculated = bandwidth / (nSamples * 2) * totalTime / 1000;

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
                        for (int c = 0; c < channels; c++)
                        {
                            GenerateSignal(databuf, bufStart + 8 + c * 6, stride, nSamples, timebase, timeStep, receivers[c]);
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
            //todo handle multiple generators automatically

            int f1 = rx.vfo-rx.generators[0].frequency;
            int f2 = rx.vfo-rx.generators[1].frequency;
            for (int i = 0; i < nSamples * stride; i += stride)
            {
                double angle1=f1 * 2 * Math.PI * timebase;
                double angle2=f2 * 2 * Math.PI * timebase;
                    
                double amp = Math.Round(Math.Sin(angle1) * rx.generators[0].damplitude + Math.Sin(angle2) * rx.generators[1].damplitude);
                int iamp = (int)amp;
                if(iamp>max24int)
                {
                    iamp = max24int;
                    adc1clip = true;
                }
                else if(iamp<min24int)
                {
                    iamp = min24int;
                    adc1clip = true;
                }
                databuf[startOffset + i] = (byte)(iamp >> 16);
                databuf[startOffset + i + 1] = (byte)((iamp >> 8) & 0xff);
                databuf[startOffset + i + 2] = (byte)((iamp ) & 0xff);
                    
                amp = Math.Round(-Math.Cos(angle1) * rx.generators[0].damplitude -Math.Cos(angle2) * rx.generators[1].damplitude);
                iamp = (int)amp;

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

                timebase += timestep;
                
            }
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
                    msgQueue.Enqueue(new receivedPacket() { received = received, endPoint = RemoteIpEndPoint });
                }
            }
            catch (Exception )
            {
            }
        }
        public void handlePacket(receivedPacket packet)
        {
            byte[] received = packet.received;
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
                response[10] = 0x01;//board type
                status = "Discovered";
                seqNo = 1;
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
                    status = "Running";
                }
                else
                {
                    running = false;
                    status = "Off";
                }
            }
            else if (received[2] == 1 && received[3] == 2)
            {
                //standard data packet
                handleCommandControl(received[11], received[12], received[13], received[14], received[15]);
                handleCommandControl(received[512 + 11], received[512 + 12], received[512 + 13], received[512 + 14], received[512 + 15]);
            }
            else
            {

            }

        }
        void resetTransmission()
        {
            startTime = DateTime.Now;
            actualPacketCount = 0;       
        }
        public void handleCommandControl(byte c0,byte c1, byte c2, byte c3, byte c4)
        {
            //Console.WriteLine(string.Format("{0}\t{1}\t{2}\t{3}\t{4}", c0,c1,c2,c3,c4));
                        
            switch(c0 & 0xfe)
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
                    if(nReceivers!=receivers.Count)
                    {
                        while (receivers.Count > nReceivers) receivers.Remove(receivers.Last());
                        while (receivers.Count < nReceivers) receivers.Add(new receiver("RX" + (receivers.Count+1)));
                        resetTransmission();    
                    }
                    break;
                case 2: int t= (((int)c1) << 24) + (((int)c2) << 16) + (((int)c3) << 8) + (int)c4;            
                        txNCO = t;
                        if (!duplex)
                        {
                            receivers[0].vfo = txNCO;
                            if (receivers[0].generators[0].frequency == 0) receivers[0].generators[0].frequency = receivers[0].vfo;
                            if (receivers[0].generators[1].frequency == 0) receivers[0].generators[1].frequency = receivers[0].vfo + 10000;
                  
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
                        if (receivers[rxIdx].generators[0].frequency == 0) receivers[rxIdx].generators[0].frequency = receivers[rxIdx].vfo;
                        if (receivers[rxIdx].generators[1].frequency == 0) receivers[rxIdx].generators[1].frequency = receivers[rxIdx].vfo+10000;
                    }
                    break;
            //    default: Console.WriteLine(string.Format("Unhandled Control message {0}\t{1}\t{2}\t{3}\t{4}", c0, c1, c2, c3, c4)); break;

            }
        }
        
    }
    public class receiver : BindableBase
    {
        public receiver(string name)
        {
            this.name = name;
            generators.Add(new sineWave());
            generators.Add(new sineWave());
        }
        private int _vfo;
        public int vfo 
        {
            get{ return _vfo;}
            set { SetProperty(ref _vfo, value); } 
        }
        private string _name;
        public string name
        {
            get { return _name; }
            set { SetProperty(ref _name, value); }
        }
        ObservableCollection<sineWave> _generators =new ObservableCollection<sineWave>();
        public ObservableCollection<sineWave> generators
        {
            get { return _generators; }
            set { SetProperty(ref _generators, value); }
        }
    }
    public class sineWave : BindableBase
    {
        public sineWave()
        {
            amplitude = -10;
        }
        private int _frequency=0;
        public double damplitude = 0.0;
        public int frequency
        {
            get { return _frequency; }
            set { SetProperty(ref _frequency, value); }
        }
        private int _amplitude=0;
        public int amplitude
        {
            get { return _amplitude; }
            set 
            {
                damplitude = 0x7fffff / Math.Pow(Math.Sqrt(10), -(double)value / 10);
                SetProperty(ref _amplitude, value); 
            }
        }
            
    }
    public class receivedPacket
    {
        public IPEndPoint endPoint { get; set; }
        public byte[] received { get; set; }
    }
}
