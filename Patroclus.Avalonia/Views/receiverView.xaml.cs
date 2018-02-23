using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Patroclus.Avalonia.Views
{
    public class receiverView : UserControl
    {
        public receiverView()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
