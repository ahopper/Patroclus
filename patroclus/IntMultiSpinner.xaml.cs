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
using System.Collections.ObjectModel;
using System.Globalization;

namespace patroclus
{
    /// <summary>
    /// Interaction logic for IntMultiSpinner.xaml
    /// </summary>
    public partial class IntMultiSpinner : UserControl
    {
        private ObservableCollection<spinColumn> _columns=new ObservableCollection<spinColumn>();
        public ObservableCollection<spinColumn> columns
        {
            get { return _columns;}
            set {_columns =value;}
        }

        public IntMultiSpinner()
        {
   //         this.DataContext = this;
            InitializeComponent();
            LayoutRoot.DataContext = this;
        }

        //TODO make max and min dependency properties
        private int _MaxValue = int.MaxValue;
        public int MaxValue
        {
            get
            {
                return _MaxValue;
            }
            set
            {
                int cols = 1+(int)Math.Log10(value);
                if (cols != columns.Count)
                {
                    columns.Clear();
                    for (int i = 0; i < cols; i++)
                    {
                        columns.Add(new spinColumn() { first = (i == 0), col = cols - i });
                    }
                }
                _MaxValue = value;
            }
        }
        private int _MinValue = 0;
        public int MinValue
        {
            get
            {
                return _MinValue;
            }
            set
            {
                _MinValue = value;
            }
        }

        private void TextBox_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            var tb = sender as TextBox;
            spinNumber(tb,(int)Math.Sign(e.Delta));

        }
       
        void shiftLeft(TextBox tb)
        {
            var i = columns.Count - ((spinColumn)tb.DataContext).col + -1;

            if (i >=0)
            {
                var container = tbContainer.ItemContainerGenerator.ContainerFromIndex(i);
                var tbt = (TextBox)VisualTreeHelper.GetChild(container, 0);
                tbt.Focus();
            }
 
        }
        void shiftRight(TextBox tb)
        {
            var i = columns.Count-((spinColumn)tb.DataContext).col+1;

            if (i <columns.Count)
            {
                var container = tbContainer.ItemContainerGenerator.ContainerFromIndex(i);
                var tbt = (TextBox)VisualTreeHelper.GetChild(container, 0);
                tbt.Focus();
            }
 
        }
        void spinNumber(TextBox tb,int inc)
        {
            var i = ((spinColumn)tb.DataContext).col;
            
           int newval = Value +(int) (inc * Math.Pow(10,i-1 ));
            
            if(newval<=MaxValue && newval >=MinValue)Value = newval;
        }
        void setCol(TextBox tb, int num)
        {
            var i = ((spinColumn)tb.DataContext).col;
            int mul = (int)Math.Pow(10, i-1);
            Value+=num*mul-Value/mul%10*mul;
           
        }
        

        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            
        }
        private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var tb = sender as TextBox;
            switch (e.Key)
            {
                case Key.NumPad0:
                case Key.D0: setCol(tb,0); shiftRight(tb); break;
                case Key.NumPad1:
                case Key.D1: setCol(tb, 1); shiftRight(tb); break;
                case Key.NumPad2:
                case Key.D2: setCol(tb, 2); shiftRight(tb); break;
                case Key.NumPad3:
                case Key.D3: setCol(tb, 3); shiftRight(tb); break;
                case Key.NumPad4:
                case Key.D4: setCol(tb, 4); shiftRight(tb); break;
                case Key.NumPad5:
                case Key.D5: setCol(tb, 5); shiftRight(tb); break;
                case Key.NumPad6:
                case Key.D6: setCol(tb, 6); shiftRight(tb); break;
                case Key.NumPad7:
                case Key.D7: setCol(tb, 7); shiftRight(tb); break;
                case Key.NumPad8:
                case Key.D8: setCol(tb, 8); shiftRight(tb); break;
                case Key.NumPad9:
                case Key.D9: setCol(tb, 9); shiftRight(tb); break;
                

                case Key.Left: shiftLeft(tb); e.Handled = true; break;
                case Key.Right: shiftRight(tb); e.Handled = true; break;
                case Key.Delete: setCol(tb, 0); shiftRight(tb); e.Handled = true; break;
                case Key.Back: setCol(tb, 0); shiftLeft(tb); e.Handled = true; break;
                case Key.Up: spinNumber(tb, 1); e.Handled = true; break;
                case Key.Down: spinNumber(tb, -1); e.Handled = true; break;
                case Key.Tab: return;// MoveFocus(new TraversalRequest(FocusNavigationDirection.Next)); e.Handled = true; break;

            }
            
            tb.SelectAll();
            e.Handled = true;
        }
        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var tb = sender as TextBox;
            tb.SelectAll();
        }
        

        /// <summary> 
        /// Identifies the Value dependency property. 
        /// </summary> 
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                "Value", typeof(int), typeof(IntMultiSpinner),
                new FrameworkPropertyMetadata(0, new PropertyChangedCallback(OnValueChanged),
                                              new CoerceValueCallback(CoerceValue)));

        /// <summary> 
        /// Gets or sets the value assigned to the control. 
        /// </summary> 
        public int Value
        {
            get { return (int)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        private static object CoerceValue(DependencyObject element, object value)
        {
            int newValue = (int)value;
            IntMultiSpinner control = (IntMultiSpinner)element;

            newValue = Math.Max(control.MinValue, Math.Min(control.MaxValue, newValue));

            return newValue;
        }

        private static void OnValueChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            IntMultiSpinner control = (IntMultiSpinner)obj;

            RoutedPropertyChangedEventArgs<int> e = new RoutedPropertyChangedEventArgs<int>(
                (int)args.OldValue, (int)args.NewValue, ValueChangedEvent);
            control.OnValueChanged(e);
        }

        /// <summary> 
        /// Identifies the ValueChanged routed event. 
        /// </summary> 
        public static readonly RoutedEvent ValueChangedEvent = EventManager.RegisterRoutedEvent(
            "ValueChanged", RoutingStrategy.Bubble,
            typeof(RoutedPropertyChangedEventHandler<int>), typeof(IntMultiSpinner));

        /// <summary> 
        /// Occurs when the Value property changes. 
        /// </summary> 
        public event RoutedPropertyChangedEventHandler<int> ValueChanged
        {
            add { AddHandler(ValueChangedEvent, value); }
            remove { RemoveHandler(ValueChangedEvent, value); }
        }

        /// <summary> 
        /// Raises the ValueChanged event. 
        /// </summary> 
        /// <param name="args">Arguments associated with the ValueChanged event.</param>
        protected virtual void OnValueChanged(RoutedPropertyChangedEventArgs<int> args)
        {
            RaiseEvent(args);
        }
        

    }
   
    public class MultiNumberToColumnConverter : IMultiValueConverter
    {
        public object Convert(object[] value, Type targetType,
            object parameter, CultureInfo culture)
        {
            int col = (int)value[1];
            string val = "000000000000000"+(((int)value[0])).ToString();
             return val.Substring(val.Length - col, 1);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class ColToMarginConverter : IMultiValueConverter
    {
        public object Convert(object[] value, Type targetType,
            object parameter, CultureInfo culture)
        {
            double fontsize = (double)value[0];
            spinColumn scol = (spinColumn)value[1];
            return new Thickness((scol.col)%3==0  && !scol.first ? fontsize/4:-1,0,-1,0);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class spinColumn
    {
        public bool first { get; set; }
        public int col { get; set; }
    }
}
