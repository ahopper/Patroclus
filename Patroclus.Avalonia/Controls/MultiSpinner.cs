using Avalonia.Input.Platform;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Utils;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.Data;
using Avalonia;
using Avalonia.Controls;

namespace Patroclus.Avalonia.Controls
{ 
    public class MultiSpinner : TemplatedControl
    {
       
        public static readonly DirectProperty<MultiSpinner, int> CaretIndexProperty =
            AvaloniaProperty.RegisterDirect<MultiSpinner, int>(
                nameof(CaretIndex),
                o => o.CaretIndex,
                (o, v) => o.CaretIndex = v);

        public static readonly StyledProperty<bool> IsReadOnlyProperty =
            AvaloniaProperty.Register<MultiSpinner, bool>(nameof(IsReadOnly));

        
        public static readonly DirectProperty<MultiSpinner, double> ValueProperty =
            Slider.ValueProperty.AddOwner<MultiSpinner>(
                o => o.Value,
                (o, v) => o.Value = v,
                defaultBindingMode: BindingMode.TwoWay,
                enableDataValidation: true);

        public static readonly DirectProperty<MultiSpinner, double> MaximumProperty =
            Slider.MaximumProperty.AddOwner<MultiSpinner>(
                o => o.Maximum,
                (o, v) => o.Maximum = v,
                defaultBindingMode: BindingMode.TwoWay,
                enableDataValidation: true);

        public static readonly DirectProperty<MultiSpinner, double> MinimumProperty =
                    Slider.MinimumProperty.AddOwner<MultiSpinner>(
                        o => o.Minimum,
                        (o, v) => o.Minimum = v,
                        defaultBindingMode: BindingMode.TwoWay,
                        enableDataValidation: true);

        public static readonly StyledProperty<TextAlignment> TextAlignmentProperty =
            TextBlock.TextAlignmentProperty.AddOwner<MultiSpinner>();

 
        private double _value;
        private double _minimum;
        private double _maximum;

        private int _caretIndex;
        private NumericTextPresenter _presenter;
        private bool _ignoreTextChanges;
        private static readonly string[] invalidCharacters = new String[1] { "\u007f" };

        static MultiSpinner()
        {
            FocusableProperty.OverrideDefaultValue(typeof(MultiSpinner), true);
        }

        public MultiSpinner()
        {
        }

      
        public int CaretIndex
        {
            get
            {
                return _caretIndex;
            }

            set
            {
                value = CoerceCaretIndex(value);
                SetAndRaise(CaretIndexProperty, ref _caretIndex, value);
            }
        }

        public bool IsReadOnly
        {
            get { return GetValue(IsReadOnlyProperty); }
            set { SetValue(IsReadOnlyProperty, value); }
        }
          
        [Content]
        public double Value
        {
            get { return _value; }
            set
            {
                if (!_ignoreTextChanges)
                {
                 //todo   CaretIndex = CoerceCaretIndex(CaretIndex, value?.Length ?? 0);
                    SetAndRaise(ValueProperty, ref _value, value);
                }
            }
        }
        public Double Maximum
        {
            get { return _maximum; }
            set { SetAndRaise(MaximumProperty, ref _maximum, value); }
        }
        public Double Minimum
        {
            get { return _minimum; }
            set { SetAndRaise(MinimumProperty, ref _minimum, value); }
        }

        public TextAlignment TextAlignment
        {
            get { return GetValue(TextAlignmentProperty); }
            set { SetValue(TextAlignmentProperty, value); }
        }

        
        protected override void OnTemplateApplied(TemplateAppliedEventArgs e)
        {
            _presenter = e.NameScope.Get<NumericTextPresenter>("PART_TextPresenter");
            _presenter.Cursor = new Cursor(StandardCursorType.Arrow);

            if (IsFocused)
            {
                _presenter.ShowCaret();
            }
        }

        protected override void OnGotFocus(GotFocusEventArgs e)
        {
            base.OnGotFocus(e);
            _presenter?.ShowCaret();
            
        }

        protected override void OnLostFocus(RoutedEventArgs e)
        {
            base.OnLostFocus(e);
            
            _presenter?.HideCaret();
        }

        protected override void OnTextInput(TextInputEventArgs e)
        {
            HandleTextInput(e.Text);
        }
        
        private void HandleTextInput(string input)
        {
            if (!IsReadOnly)
            {
                input = RemoveInvalidCharacters(input);
                int caretIndex = CaretIndex;
                if (!string.IsNullOrEmpty(input))
                {
                   caretIndex = CaretIndex;
                    setCol(input[0] - '0');
                    CaretIndex += input.Length;
                }
            }
        }
        void incCol(int column, int increment)
        {
            double newValue = Value + Math.Pow(10, places - column - 1) * increment;
            if(newValue>=Minimum && newValue <=Maximum) SetTextInternal(newValue);
        }
        void setCol(int num)
        {
            var i = places - CaretIndex;
            int mul = (int)Math.Pow(10, i - 1);
            int value = (int)Value;
            value += num * mul - value / mul % 10 * mul;
            SetTextInternal((double)value);
        }
        public string RemoveInvalidCharacters(string text)
        {
            for (var i = 0; i < invalidCharacters.Length; i++)
            {
                text = text.Replace(invalidCharacters[i], string.Empty);
            }
            string ret = "";
            foreach(var c in text)
            {
                if("01234567890".Contains(c))
                {
                    ret += c;
                }
            }
            return ret;
        }

        private async void Copy()
        {
            await ((IClipboard)AvaloniaLocator.Current.GetService(typeof(IClipboard)))
                .SetTextAsync(Value.ToString());
        }

        private async void Paste()
        {
            var text = await ((IClipboard)AvaloniaLocator.Current.GetService(typeof(IClipboard))).GetTextAsync();
            if (text == null)
            {
                return;
            }
            HandleTextInput(text);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            int caretIndex = CaretIndex;
            bool movement = false;
            bool handled = false;
            var modifiers = e.Modifiers;

            switch (e.Key)
            {
                case Key.A:
                    if (modifiers == InputModifiers.Control)
                    {
                       handled = true;
                    }
                    break;
                case Key.C:
                    if (modifiers == InputModifiers.Control)
                    {
                        Copy();
                        handled = true;
                    }
                    break;

                case Key.X:
                    if (modifiers == InputModifiers.Control)
                    {
                        Copy();
                        handled = true;
                    }
                    break;

                case Key.V:
                    if (modifiers == InputModifiers.Control)
                    {
                        Paste();
                        handled = true;
                    }

                    break;

                case Key.Z:
                    if (modifiers == InputModifiers.Control)
                    {
                        handled = true;
                    }
                    break;
                case Key.Y:
                    if (modifiers == InputModifiers.Control)
                    {
                        handled = true;
                    }
                    break;
                case Key.Left:
                    MoveHorizontal(-1, modifiers);
                    movement = true;
                    break;

                case Key.Right:
                    MoveHorizontal(1, modifiers);
                    movement = true;
                    break;

                case Key.Up:
                    movement = MoveVertical(1, modifiers);
                    break;

                case Key.Down:
                    movement = MoveVertical(-1, modifiers);
                    break;

                case Key.Home:
                    MoveHome(modifiers);
                    movement = true;
                    break;

                case Key.End:
                    MoveEnd(modifiers);
                    movement = true;
                    break;

                case Key.Back:
                    setCol(0);
                    CaretIndex -= 1;
                    handled = true;
                    break;

                case Key.Delete:
                    setCol(0);
                    handled = true;
                    break;

                case Key.Enter:
                    

                    break;

                case Key.Tab:
                   
                    base.OnKeyDown(e);
                   

                    break;

                             

                default:
                    handled = false;
                    break;
            }

            if (movement && ((modifiers & InputModifiers.Shift) != 0))
            {
                
            }
            else if (movement)
            {
                
            }

            if (handled || movement)
            {
                e.Handled = true;
            }
            
        }
        private Point _lastPoint;
        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            var point = e.GetPosition(_presenter);
            var index = CaretIndex = _presenter.GetCaretIndex(point);
            _lastPoint = point;
            
            if (point.Y < Bounds.Height * 0.25) incCol(CaretIndex, +1);
            else if (point.Y > Bounds.Height * 0.75) incCol(CaretIndex, -1);

            e.Device.Capture(_presenter);
            e.Handled = true;
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            if (_presenter != null && e.Device.Captured == _presenter)
            {
                int sensitivity = 8;
                var point = e.GetPosition(_presenter);
                //  CaretIndex =  _presenter.GetCaretIndex(point);
                if (Math.Abs(point.Y - _lastPoint.Y) >= sensitivity)
                {
                    incCol(CaretIndex, -(int)(point.Y - _lastPoint.Y) / sensitivity);
                    _lastPoint = point;
                }
            }
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            if (_presenter != null && e.Device.Captured == _presenter)
            {
                var point = e.GetPosition(_presenter);
            //    if (point == _lastPoint)
            //    {
            //        if (point.Y < Bounds.Height * 0.25) incCol(CaretIndex, +1);
            //        else if (point.Y > Bounds.Height * 0.75) incCol(CaretIndex, -1);
            //    }
                e.Device.Capture(null);
            }
        }
        protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
        {
            if (_presenter != null)
            {
                var point = e.GetPosition(_presenter);
                var caretIndex = _presenter.GetCaretIndex(point);
                incCol(caretIndex, (int)e.Delta.Y);
                e.Handled = true;
            }
        }

        protected override void UpdateDataValidation(AvaloniaProperty property, BindingNotification status)
        {
            if (property == ValueProperty)
            {
                DataValidationErrors.SetError(this, status.Error);
            }
        }

        private int CoerceCaretIndex(int value) => CoerceCaretIndex(value, places-1);

        private int CoerceCaretIndex(int value, int length)
        {
            if (value < 0)
            {
                return 0;
            }
            else if (value > length)
            {
                return length;
            }       
            else
            {
                return value;
            }
        }

        private int DeleteCharacter(int index)
        {
            //todo
            return 0;
        }
        private int places
        {
            get
            {
                return 1 + (int)Math.Log10(Maximum);
            }
        }
        private void MoveHorizontal(int direction, InputModifiers modifiers)
        {
            var caretIndex = CaretIndex;

            var index = caretIndex + direction;

            if (index < 0 || index > places)
            {
                return;
            }
                
            CaretIndex = index;
            return;                
        }

        private bool MoveVertical(int count, InputModifiers modifiers)
        {
            incCol(_caretIndex, count);

            return true;
        }

        private void MoveHome(InputModifiers modifiers)
        {
            
            int caretIndex = 0;
            
            CaretIndex = caretIndex;
        }

        private void MoveEnd(InputModifiers modifiers)
        {
            CaretIndex = places-1;
        }     
        
        private void SetTextInternal(double value)
        {
            try
            {
                _ignoreTextChanges = true;
                SetAndRaise(ValueProperty, ref _value, value);
            }
            finally
            {
                _ignoreTextChanges = false;
            }
        }

    }
    
}
