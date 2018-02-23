using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Patroclus.Avalonia.ViewModels
{ 
    class SineWaveGenerator: SignalGenerator
    {
        public SineWaveGenerator()
        {
            amplitude = -10;
        }
        private int _frequency=0;
        private double damplitude = 0.0;
        public int frequency
        {
            get { return _frequency; }
            set { this.RaiseAndSetIfChanged(ref _frequency, value); }
        }
        private int _amplitude=0;
        public int amplitude
        {
            get { return _amplitude; }
            set 
            {
                damplitude = 1 / Math.Pow(Math.Sqrt(10), -(double)value / 10);
                this.RaiseAndSetIfChanged(ref _amplitude, value); 
            }
        }
        public override void GenerateSignal(double[] outbuf, int nSamples, double timebase, double timestep, double vfo)
        {
            int f1 = (int)vfo-frequency;
            int idx=0;
            while(idx<2*nSamples)
            {
                double angle1 = f1 * 2 * Math.PI * timebase;
               
                //add to whatever else is already in buffer
                if (amplitude > -200)
                {
                    outbuf[idx++] += Math.Sin(angle1) * damplitude;
                    outbuf[idx++] += -Math.Cos(angle1) * damplitude;
                }
                else idx += 2;
                timebase += timestep;
            }
        }
        public override void SetDefaults(double vfo)
        {
            if (frequency == 0) frequency = (int)vfo;
        }
       
            
    }
}
