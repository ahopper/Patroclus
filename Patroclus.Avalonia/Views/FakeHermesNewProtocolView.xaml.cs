using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Patroclus.Avalonia.Views
{
    public class FakeHermesNewProtocolView : UserControl
    {
        public FakeHermesNewProtocolView()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
