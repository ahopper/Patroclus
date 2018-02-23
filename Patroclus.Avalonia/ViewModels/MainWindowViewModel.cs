using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Text;

namespace Patroclus.Avalonia.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private FakeRadio _radio;
        public FakeRadio radio
        {
            get { return _radio; }
            set { this.RaiseAndSetIfChanged(ref _radio, value); }
        }

        private int _testi=137;
        public int testi
        {
            get { return _testi; }
            set { this.RaiseAndSetIfChanged(ref _testi, value); }
        }

        private int _radioType=0;
        public int radioType
        {
            get { return _radioType; }
            set {
                if(value!=_radioType)
                {
                    switch(value)
                    {
                        case 0: loadHermes();break;
                        case 1: loadHermesNP(); break;
                        case 2: loadHermesLite(); break;
                        case 3: loadHermesLite2(); break;
                    }
                }
                this.RaiseAndSetIfChanged(ref _radioType, value);
            }
        }
        public MainWindowViewModel()
        {
            loadHermes();

            var s = String.Format("{0:x8}", testi);
            Console.WriteLine(s);
        }

        private void loadHermes()
        {
            if (radio != null) radio.Stop();
            var hermes = new FakeHermes();

            hermes.boardID = 1;
            hermes.hermesCodeVersion = 30;
            hermes.port = 1024;
            hermes.start();

            radio = hermes;
            
        }
        private void loadHermesLite()
        {
            if (radio != null) radio.Stop();
            var hermes = new FakeHermes();

            hermes.boardID = 6;
            hermes.hermesCodeVersion = 30;
            hermes.port = 1024;
            hermes.start();

            radio = hermes;
            
        }
        private void loadHermesLite2()
        {
            if (radio != null) radio.Stop();
            var hermes = new FakeHermes();

            hermes.boardID = 6;
            hermes.hermesCodeVersion = 40;
            hermes.port = 1024;
            hermes.start();

            radio = hermes;
            
        }
        private void loadHermesNP()
        {
            if (radio != null) radio.Stop();

            var hermes = new FakeHermesNewProtocol();

            hermes.port = 1024;
            hermes.start();

            radio = hermes;
            
        }
    }
}
