using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using System.Windows.Data;
using System.Windows.Input;

namespace Patroclus.Avalonia.ViewModels
{
    public class receiver : ViewModelBase
    {
        const int max24int = 0x7fffff;
        const int min24int = -0x800000;

        public receiver(string name)
        {
    //        BindingOperations.CollectionRegistering += BindingOperations_CollectionRegistering;
            this.name = name;
            AddSine();
            AddSine();
            AddWav();
        }

  //      void BindingOperations_CollectionRegistering(object sender, CollectionRegisteringEventArgs e)
   //     {
   //         BindingOperations.EnableCollectionSynchronization(generators, _generatorsLock);
    //    }
        private object _generatorsLock = new object();

        ObservableCollection<SignalGenerator> _generators = new ObservableCollection<SignalGenerator>();
        public ObservableCollection<SignalGenerator> generators
        {
            get { return _generators; }
            set { this.RaiseAndSetIfChanged(ref _generators, value); }
        }

        private int _seq=0;
        public int seq
        {
            get { return _seq; }
            set { this.RaiseAndSetIfChanged(ref _seq, value); }
        }
        private long _packetCount = 0;
        public long packetCount
        {
            get { return _packetCount; }
            set { this.RaiseAndSetIfChanged(ref _packetCount, value); }
        }
        private double _timebase = 0;
        public double timebase
        {
            get { return _timebase; }
            set { this.RaiseAndSetIfChanged(ref _timebase, value); }
        }
        private int _bandwidth = 0;
        public int bandwidth
        {
            get { return _bandwidth; }
            set { this.RaiseAndSetIfChanged(ref _bandwidth, value); }
        }
        private int _vfo;
        public int vfo
        {
            get { return _vfo; }
            set { this.RaiseAndSetIfChanged(ref _vfo, value); }
        }
        private string _name;
        public string name
        {
            get { return _name; }
            set { this.RaiseAndSetIfChanged(ref _name, value); }
        }
        private int _sampleSize=24;
        public int sampleSize 
        {
            get { return _sampleSize; }
            set { this.RaiseAndSetIfChanged(ref _sampleSize, value); }
        }

        private ulong _timestamp =0;
        public ulong timestamp
        {
            get { return _timestamp; }
            set { this.RaiseAndSetIfChanged(ref _timestamp, value); }
        }

        private bool _adcClip=false;
        public bool adcClip
        {
            get { return _adcClip; }
            set { this.RaiseAndSetIfChanged(ref _adcClip, value); }
        }
        RelayCommand _AddSineCommand;
        public ICommand AddSineCommand
        {
            get { return _AddSineCommand ?? (_AddSineCommand = new RelayCommand(param => this.AddSine())); }
        }
        public void AddSine()
        {
            lock (_generatorsLock)
            {
                generators.Add(new SineWaveGenerator());
            }
        }

        RelayCommand _AddWavCommand;
        public ICommand AddWavCommand
        {
            get { return _AddWavCommand ?? (_AddWavCommand = new RelayCommand(param => this.AddWav())); }
        }
        public void AddWav()
        {
            lock (_generatorsLock)
            {
                generators.Add(new WavFileGenerator());
            }
        }
        public void GenerateSignal(double[] outbuf, int nSamples, double timebase, double timestep)
        {
            //if collection is modified on other thread just continue with missing data rather than lock
            try 
            {
                foreach (SignalGenerator generator in generators)
                {
                    generator.GenerateSignal(outbuf, nSamples, timebase, timestep, vfo);
                }
            }
            catch(Exception)
            {

            }
        }
        public void GenerateSignal(byte[] outbuf,int startOffset,int stride, int nSamples, double timebase, double timestep)
        {
            bool clip = false;
            double[] buf = new double[nSamples*2];
            GenerateSignal(buf, nSamples, timebase, timestep);
            switch(sampleSize)
            {
                case 24:
                    int idx = 0;
                    for (int i = 0; i < nSamples * stride; i += stride)
                    {
                        int iamp = (int)Math.Round(buf[idx++]*0x7fffff);
                        if (iamp > max24int)
                        {
                            iamp = max24int;
                            clip = true;
                        }
                        else if (iamp < min24int)
                        {
                            iamp = min24int;
                            clip = true;
                        }
                        outbuf[startOffset + i] = (byte)(iamp >> 16);
                        outbuf[startOffset + i + 1] = (byte)((iamp >> 8) & 0xff);
                        outbuf[startOffset + i + 2] = (byte)((iamp) & 0xff);

                        iamp = (int)Math.Round(buf[idx++]*0x7fffff);

                        if (iamp > max24int)
                        {
                            iamp = max24int;
                            clip = true;
                        }
                        else if (iamp < min24int)
                        {
                            iamp = min24int;
                            clip = true;
                        }

                        outbuf[startOffset + i + 3] = (byte)(iamp >> 16);
                        outbuf[startOffset + i + 4] = (byte)((iamp >> 8) & 0xff);
                        outbuf[startOffset + i + 5] = (byte)(iamp & 0xff);
                    }
                    break;
            }
            adcClip = clip;
        }
    }
}
