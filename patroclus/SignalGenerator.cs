using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace patroclus
{
    public abstract class SignalGenerator : BindableBase
    {
        abstract public void GenerateSignal(double[] outbuf, int nSamples, double timebase, double timestep, double vfo);
        abstract public void SetDefaults(double vfo);
       
    }
}
