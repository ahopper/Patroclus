using Avalonia.Controls;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Patroclus.Avalonia.ViewModels
{
    class WavFileGenerator : SignalGenerator
    {

        //wav file constants
        const uint riffRiffHeader = 0x46464952;
        const uint riffWavRiff = 0x54651475;
        const uint riffFormat = 0x020746d66;
        const uint riffLabeledText = 0x478747C6;
        const uint riffInstrumentation = 0x478747C6;
        const uint riffSample = 0x6C706D73;
        const uint riffFact = 0x47361666;
        const uint riffData = 0x61746164;
        const uint riffJunk = 0x4b4e554a;
        
        UInt16 channels;
        int sampleRate;
        int bps;
        UInt16 bps2;
        UInt16 bitsPerSample;
        BinaryReader reader = null;
        
        private void loadWav(string filename)
        {
            // TODO read format data and do something with it
            byte[] twav = null;
            uint chunksize;
            UInt16 format;
            int temp;
            
            if(reader!=null)
            {
                reader.Close();
                reader.Dispose();
                reader = null;
            }

            reader = new BinaryReader(File.OpenRead(filename));
            
                try
                {
                    while (twav == null)
                    {
                        switch (reader.ReadUInt32())
                        {
                            case riffRiffHeader:
                                chunksize = reader.ReadUInt32();
                                temp = reader.ReadInt32();
                                break;
                            case riffFormat:
                                chunksize = reader.ReadUInt32();
                                format = reader.ReadUInt16();
                                channels=reader.ReadUInt16();
                                sampleRate=reader.ReadInt32();
                                bps=reader.ReadInt32();
                                bps2 = reader.ReadUInt16();
                                bitsPerSample = reader.ReadUInt16();
                                break;
                            case riffData:
                                chunksize = reader.ReadUInt32();
                           //     twav = reader.ReadBytes((int)chunksize);
                           //     pos = 0;
                           //     wav = twav;
                           //     break;
                                return;
                            default:
                                chunksize = reader.ReadUInt32();
                                reader.BaseStream.Seek(chunksize, SeekOrigin.Current);
                                break;
                        }
                    }
                }
                catch (EndOfStreamException) { }
            
        }
        
        private byte[] wav;
        private int pos = 0;
        
        public double damplitude = 0.0;
        
        private int _amplitude = 0;
        
        public WavFileGenerator()
        {
            amplitude = -10;
        }

        public int amplitude
        {
            get { return _amplitude; }
            set
            {
                damplitude = 1 / Math.Pow(Math.Sqrt(10), -(double)value / 10);
                this.RaiseAndSetIfChanged(ref _amplitude, value);
            }
        }
        
        RelayCommand _SelectFileCommand;
        public ICommand SelectFileCommand
        {
            get { return _SelectFileCommand ?? (_SelectFileCommand = new RelayCommand(param => this.SelectFileAsync())); }
        }
        public async void SelectFileAsync()
        {
            OpenFileDialog of = new OpenFileDialog();
            //of.DefaultExt = ".wav";
            // of.Filters = { "Wav files|*.wav"};

            string[] files= await of.ShowAsync();
            if(files?.Length>0)
            {
                filename = files[0];
            }

            //if (of.ShowDialog().Value)
           // {
           //     filename = of.FileName;
           // }
        }

        private string _filename;
        public string filename
        {
            get { return _filename; }
            set 
            { 
                if(File.Exists(value) )
                {
                    //TODO make thread safe as generator could be running
                    loadWav(value);
                }
                this.RaiseAndSetIfChanged(ref _filename, value);
            }
        }
        public override void GenerateSignal(double[] outbuf, int nSamples, double timebase, double timestep, double vfo)
        {
         //   if (wav == null) return;
            if (reader == null) return;
            int len=nSamples*2 * bitsPerSample / 8;
            if(wav==null || wav.Length!=len)
            {
                wav = new byte[len];
            }
            if(reader.Read(wav, 0, len)!=len)
            {
                loadWav(filename);
            }
            
            pos=0;
            int idx = 0;
            switch(bitsPerSample)
            { 
                case 16:            
                    while (idx < 2 * nSamples)
                    {
                  //      if (pos >= wav.Length) pos = 0;
                        short val = (short)((wav[pos++] << 8) + wav[pos++]);
                        outbuf[idx++] += ((double)val)/32768 * damplitude; 
                    }
                    break;
                case 24:
                    while (idx < 2 * nSamples)
                    {
                  //      if (pos >= wav.Length) pos = 0;
                      //  int val = (int)((wav[pos++] << 24) | (wav[pos++] << 16) | (wav[pos++] << 8));
                        int val = (int)((wav[pos++] << 8) | (wav[pos++] << 16) | (wav[pos++] << 24));
                            
                        outbuf[idx++] += ((double)val) / (32768*256) * damplitude; 
                    }
                    break;
            }
        }
        public override void SetDefaults(double vfo)
        {
            
        }
        ~WavFileGenerator()
        {
            if (reader != null) reader.Dispose();
        }
    }
}
