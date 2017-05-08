using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MahApps.Metro.Controls;

namespace patroclus
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public FakeRadio radio { get; set; }
        public MainWindow()
        {
            InitializeComponent();
            loadHermes();
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
            DataContext = hermes;
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
            DataContext = hermes;
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
            DataContext = hermes;
        }
        private void loadHermesNP()
        {
            if (radio != null) radio.Stop();
            
            var hermes = new FakeHermesNewProtocol();
           
            hermes.port = 1024;
            hermes.start();

            radio = hermes;
            DataContext = hermes;
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var cb = e.Source as ComboBox;
            var radioName = cb.SelectedValue;
            if (radioName != null)
            {
                switch (cb.SelectedValue.ToString())
                {
                    case "HPSDR Hermes": loadHermes(); break;
                    case "HPSDR Hermes new protocol": loadHermesNP(); break;
                    case "Hermes Lite": loadHermesLite(); break;
                    case "Hermes Lite 2": loadHermesLite2(); break;

                }
            }
        }
    }
    public class FakeRadio : BindableBase
    {
        public virtual void Stop()
        {

        }
    }
}
