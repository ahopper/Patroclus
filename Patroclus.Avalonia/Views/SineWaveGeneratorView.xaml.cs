using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Patroclus.Avalonia.Views
{
    public class SineWaveGeneratorView : UserControl
    {
        public SineWaveGeneratorView()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
