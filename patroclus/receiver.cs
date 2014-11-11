using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;

namespace patroclus
{
    public class receiver : BindableBase
    {
        public receiver(string name)
        {
            BindingOperations.CollectionRegistering += BindingOperations_CollectionRegistering;
            this.name = name;
            AddSine();
            AddSine();
            AddWav();
        }

        void BindingOperations_CollectionRegistering(object sender, CollectionRegisteringEventArgs e)
        {
            BindingOperations.EnableCollectionSynchronization(generators, _generatorsLock);
        }
        private object _generatorsLock = new object();

        ObservableCollection<SignalGenerator> _generators = new ObservableCollection<SignalGenerator>();
        public ObservableCollection<SignalGenerator> generators
        {
            get { return _generators; }
            set { SetProperty(ref _generators, value); }
        }
       

        private int _vfo;
        public int vfo
        {
            get { return _vfo; }
            set { SetProperty(ref _vfo, value); }
        }
        private string _name;
        public string name
        {
            get { return _name; }
            set { SetProperty(ref _name, value); }
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
    }
}
