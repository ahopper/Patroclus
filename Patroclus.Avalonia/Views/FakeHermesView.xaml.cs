using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Patroclus.Avalonia.Views
{
    public class FakeHermesView : UserControl
    {
        public FakeHermesView()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
