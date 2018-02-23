using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Patroclus.Avalonia.Views
{
    public class WavFileGeneratorView : UserControl
    {
        public WavFileGeneratorView()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
