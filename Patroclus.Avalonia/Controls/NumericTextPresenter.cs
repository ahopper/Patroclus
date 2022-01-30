using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Data;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Metadata;
using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;

namespace Patroclus.Avalonia.Controls
{
    public class NumericTextPresenter:Control
    {
        public static readonly DirectProperty<NumericTextPresenter, int> CaretIndexProperty =
           TextBox.CaretIndexProperty.AddOwner<NumericTextPresenter>(
               o => o.CaretIndex,
               (o, v) => o.CaretIndex = v);

        public static readonly DirectProperty<NumericTextPresenter, double> ValueProperty =
            AvaloniaProperty.RegisterDirect<NumericTextPresenter, double>(
                nameof(Value),
                o => o.Value,
                (o, v) => o.Value = v);

        //   public static readonly DirectProperty<NumericTextPresenter, double> ValueProperty =
        //      Slider.ValueProperty.AddOwner<MultiSpinner>(
        //          o => o.Value,
        //          (o, v) => o.Value = v,
        //          defaultBindingMode: BindingMode.TwoWay,
        //          enableDataValidation: true);


        public static readonly DirectProperty<NumericTextPresenter, double> MaximumProperty =
          AvaloniaProperty.RegisterDirect<NumericTextPresenter, double>(
                nameof(Maximum),
                o => o.Maximum,
                (o, v) => o.Maximum = v);

        public static readonly DirectProperty<NumericTextPresenter, double> MinimumProperty =
             AvaloniaProperty.RegisterDirect<NumericTextPresenter, double>(
                nameof(Minimum),
                o => o.Minimum,
                (o, v) => o.Minimum = v);

        private bool _caretBlink = false;
        private int _caretIndex;
        private double _charWidth = 16;
        private double _commaWidth = 4;
        
   
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
                InvalidateVisual();
            }
        }
        private int CoerceCaretIndex(int value)
        {
              return Math.Max(0, Math.Min(places-1, value));
        }
        public int GetCaretIndex(Point point)
        {
            double x = _charWidth;
            int index = 0;
            int place = places - 1;
            while (x<point.X)
            {
                x += _charWidth + (place % 3 == 0 ? _commaWidth : 0);
                index++;
                place--;
            }          
            return index;
        }
        public double GetCaretStart(int caretIndex)
        {
            double x = 0;
            int index = 0;
            int place = places - 1;
            while (index<caretIndex)
            {
                x += _charWidth + (place % 3 == 0 ? _commaWidth : 0);
                index++;
                place--;
            }
            return x;
        }
        public void ShowCaret()
        {
            _caretBlink = true;
         //   _caretTimer.Start();
            InvalidateVisual();
        }

        public void HideCaret()
        {
            _caretBlink = false;
         //   _caretTimer.Stop();
            InvalidateVisual();
        }

        private double _value;
        private double _minimum=0.0;
        private double _maximum=100.0;
        /// <summary>
        /// Gets or sets the text.
        /// </summary>
        [Content]
        public double Value
        {
            get { return _value; }
            set { SetAndRaise(ValueProperty, ref _value, value); }
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

        public override void Render(DrawingContext context)
        {
            var background = Background;

            if (background != null)
            {
                context.FillRectangle(background, new Rect(Bounds.Size));
            }
            double xt = 0;
            bool leadingZero = true;
            for(int i=0;i<FormattedText.Length;i++)
            {
                if (leadingZero && FormattedText[i].Text != "0" && FormattedText[i].Text != ",") leadingZero = false;
                
                if (FormattedText[i].Text == ",")
                {
                    context.DrawText(leadingZero ? Brushes.Gray : Foreground, new Point(xt-_commaWidth/2, 0), FormattedText[i]);
                    xt += _commaWidth;
                }
                else
                {
                    context.DrawText(leadingZero ? Brushes.Gray : Foreground, new Point(xt, 0), FormattedText[i]);
                    xt += _charWidth;
                }
            }

            if (_caretBlink)
            {

                var s = FormattedText[0].Bounds;//.Measure();
                //places -_caretIndex
                

                var x = GetCaretStart(_caretIndex);

                 var y = Math.Floor(s.Height) + 0.5;
                var caretBrush = Brushes.Black;

                context.DrawLine(
                    new Pen(caretBrush, 1),
                    new Point(x, y),
                    new Point(x+_charWidth, y));
            }
            // todo remove bodge to make dirty rect fill control
         /*   context.DrawLine(
                    new Pen(Brushes.Black, 1),
                    new Point(xt, 0),
                    new Point(xt+1 , 1));
         */   
        }
        static string[] numbers = { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };
            
        protected virtual FormattedText[] CreateFormattedText(Size constraint)
        {

            var typeface = new Typeface(FontFamily, FontStyle, FontWeight);
            FormattedText measure = new FormattedText
            {
                Constraint = constraint,
                Typeface = typeface,
                Text = "8",
                TextAlignment = TextAlignment,
                FontSize = FontSize,
                TextWrapping = TextWrapping,
            };

            _charWidth = measure.Bounds.Width;//   Measure().Width;
            measure.Text = ",";
            _commaWidth = measure.Bounds.Width*0.5;// Measure().Width;
            
            FormattedText[] text = new FormattedText[places + (places +2)/ 3 - 1];
            int column = 0;
            int place = places - 1;
            for (int i = 0; i < places; i++)
            {
                int mul = (int)Math.Pow(10, place);
                long value = (long)Value;
                value = value / mul % 10;

                if(value>9 || value<0)
                {
                    value = 0;
                }
                string c = numbers[value];// value.ToString();

                text[column++] = new FormattedText
                {
                    Constraint = constraint,
                    Typeface = typeface,
                    Text = c,
                    FontSize = FontSize,
                    TextAlignment = TextAlignment,
                    TextWrapping = TextWrapping,
                };
                if (place % 3 == 0 && place > 0)
                {
                    text[column++] = new FormattedText
                    {
                        Constraint = constraint,
                        Typeface = typeface,
                        Text = ",",
                        FontSize = FontSize,
                        TextAlignment = TextAlignment,
                        TextWrapping = TextWrapping,
                    };
                }
                place--;
            }
            return text;
        }
        /// <summary>
        /// Defines the <see cref="Background"/> property.
        /// </summary>
        public static readonly StyledProperty<IBrush> BackgroundProperty =
            Border.BackgroundProperty.AddOwner<NumericTextPresenter>();

 
        /// <summary>
        /// Defines the <see cref="FontFamily"/> property.
        /// </summary>
  
        public static readonly AttachedProperty<FontFamily> FontFamilyProperty =
            TextBlock.FontFamilyProperty.AddOwner<NumericTextPresenter>();
     
        /// <summary>
        /// Defines the <see cref="FontSize"/> property.
        /// </summary>
        public static readonly StyledProperty<double> FontSizeProperty =
            TextBlock.FontSizeProperty.AddOwner<NumericTextPresenter>();

        /// <summary>
        /// Defines the <see cref="FontStyle"/> property.
        /// </summary>
        public static readonly StyledProperty<FontStyle> FontStyleProperty =
            TextBlock.FontStyleProperty.AddOwner<NumericTextPresenter>();

        /// <summary>
        /// Defines the <see cref="FontWeight"/> property.
        /// </summary>
        public static readonly StyledProperty<FontWeight> FontWeightProperty =
            TextBlock.FontWeightProperty.AddOwner<NumericTextPresenter>();

        /// <summary>
        /// Defines the <see cref="Foreground"/> property.
        /// </summary>
        public static readonly StyledProperty<IBrush> ForegroundProperty =
                  TextBlock.ForegroundProperty.AddOwner<NumericTextPresenter>();

        /// <summary>
        /// Defines the <see cref="TextAlignment"/> property.
        /// </summary>
        public static readonly StyledProperty<TextAlignment> TextAlignmentProperty =
            AvaloniaProperty.Register<NumericTextPresenter, TextAlignment>(nameof(TextAlignment));

        /// <summary>
        /// Defines the <see cref="TextWrapping"/> property.
        /// </summary>
        public static readonly StyledProperty<TextWrapping> TextWrappingProperty =
            AvaloniaProperty.Register<NumericTextPresenter, TextWrapping>(nameof(TextWrapping));

        private FormattedText[] _formattedText;
 //       private Size _constraint;

        /// <summary>
        /// Initializes static members of the <see cref="NumericTextPresenter"/> class.
        /// </summary>
        static NumericTextPresenter()
        {
            ClipToBoundsProperty.OverrideDefaultValue<NumericTextPresenter>(true);
            AffectsRender<NumericTextPresenter>(ForegroundProperty,FontWeightProperty,FontSizeProperty,FontStyleProperty,ValueProperty);

            Observable.Merge<AvaloniaPropertyChangedEventArgs>(
                ValueProperty.Changed,
                TextAlignmentProperty.Changed,
                FontSizeProperty.Changed,
                FontStyleProperty.Changed,
                FontWeightProperty.Changed).AddClassHandler<NumericTextPresenter>((x, _) => x.InvalidateFormattedText());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NumericTextPresenter"/> class.
        /// </summary>
        public NumericTextPresenter()
        {
            
        }

        /// <summary>
        /// Gets or sets a brush used to paint the control's background.
        /// </summary>
        public IBrush Background
        {
            get { return GetValue(BackgroundProperty); }
            set { SetValue(BackgroundProperty, value); }
        }

        

        /// <summary>
        /// Gets or sets the font family.
        /// </summary>
        public FontFamily FontFamily
        {
            get { return GetValue(FontFamilyProperty); }
            set { SetValue(FontFamilyProperty, value); }
        }

        /// <summary>
        /// Gets or sets the font size.
        /// </summary>
        public double FontSize
        {
            get { return GetValue(FontSizeProperty); }
            set { SetValue(FontSizeProperty, value); }
        }

        /// <summary>
        /// Gets or sets the font style.
        /// </summary>
        public FontStyle FontStyle
        {
            get { return GetValue(FontStyleProperty); }
            set { SetValue(FontStyleProperty, value); }
        }

        /// <summary>
        /// Gets or sets the font weight.
        /// </summary>
        public FontWeight FontWeight
        {
            get { return GetValue(FontWeightProperty); }
            set { SetValue(FontWeightProperty, value); }
        }

        /// <summary>
        /// Gets or sets a brush used to paint the text.
        /// </summary>
        public IBrush Foreground
        {
            get { return GetValue(ForegroundProperty); }
            set { SetValue(ForegroundProperty, value); }
        }

        /// <summary>
        /// Gets the <see cref="FormattedText"/> used to render the text.
        /// </summary>
        public FormattedText[] FormattedText
        {
            get
            {
                if (_formattedText == null)
                {
                    _formattedText = CreateFormattedText(Size.Empty);
                }

                return _formattedText;
            }
        }

        /// <summary>
        /// Gets or sets the control's text wrapping mode.
        /// </summary>
        public TextWrapping TextWrapping
        {
            get { return GetValue(TextWrappingProperty); }
            set { SetValue(TextWrappingProperty, value); }
        }

        /// <summary>
        /// Gets or sets the text alignment.
        /// </summary>
        public TextAlignment TextAlignment
        {
            get { return GetValue(TextAlignmentProperty); }
            set { SetValue(TextAlignmentProperty, value); }
        }

        /// <summary>
        /// Gets the value of the attached <see cref="FontFamilyProperty"/> on a control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <returns>The font family.</returns>
        public static FontFamily GetFontFamily(Control control)
        {
            return control.GetValue(FontFamilyProperty);
        }

        /// <summary>
        /// Gets the value of the attached <see cref="FontSizeProperty"/> on a control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <returns>The font family.</returns>
        public static double GetFontSize(Control control)
        {
            return control.GetValue(FontSizeProperty);
        }

        /// <summary>
        /// Gets the value of the attached <see cref="FontStyleProperty"/> on a control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <returns>The font family.</returns>
        public static FontStyle GetFontStyle(Control control)
        {
            return control.GetValue(FontStyleProperty);
        }

        /// <summary>
        /// Gets the value of the attached <see cref="FontWeightProperty"/> on a control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <returns>The font family.</returns>
        public static FontWeight GetFontWeight(Control control)
        {
            return control.GetValue(FontWeightProperty);
        }

        /// <summary>
        /// Gets the value of the attached <see cref="ForegroundProperty"/> on a control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <returns>The foreground.</returns>
        public static IBrush GetForeground(Control control)
        {
            return control.GetValue(ForegroundProperty);
        }

        /// <summary>
        /// Sets the value of the attached <see cref="FontFamilyProperty"/> on a control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="value">The property value to set.</param>
        /// <returns>The font family.</returns>
        public static void SetFontFamily(Control control, string value)
        {
            control.SetValue(FontFamilyProperty, value);
        }

        /// <summary>
        /// Sets the value of the attached <see cref="FontSizeProperty"/> on a control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="value">The property value to set.</param>
        /// <returns>The font family.</returns>
        public static void SetFontSize(Control control, double value)
        {
            control.SetValue(FontSizeProperty, value);
        }

        /// <summary>
        /// Sets the value of the attached <see cref="FontStyleProperty"/> on a control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="value">The property value to set.</param>
        /// <returns>The font family.</returns>
        public static void SetFontStyle(Control control, FontStyle value)
        {
            control.SetValue(FontStyleProperty, value);
        }

        /// <summary>
        /// Sets the value of the attached <see cref="FontWeightProperty"/> on a control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="value">The property value to set.</param>
        /// <returns>The font family.</returns>
        public static void SetFontWeight(Control control, FontWeight value)
        {
            control.SetValue(FontWeightProperty, value);
        }

        /// <summary>
        /// Sets the value of the attached <see cref="ForegroundProperty"/> on a control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="value">The property value to set.</param>
        /// <returns>The font family.</returns>
        public static void SetForeground(Control control, IBrush value)
        {
            control.SetValue(ForegroundProperty, value);
        }

        

        

        /// <summary>
        /// Invalidates <see cref="FormattedText"/>.
        /// </summary>
        protected void InvalidateFormattedText()
        {
            if (_formattedText != null)
            {
           //     _constraint = _formattedText.Constraint;
                _formattedText = null;
            }

            InvalidateMeasure();
        }

        /// <summary>
        /// Measures the control.
        /// </summary>
        /// <param name="availableSize">The available size for the control.</param>
        /// <returns>The desired size.</returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            var formattedText = FormattedText;

            return new Size(places * _charWidth + (places / 3 - 1) + _commaWidth, formattedText[0].Bounds.Height);// Measure().Height);

           // return new Size();
        }

        protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            base.OnAttachedToLogicalTree(e);
            InvalidateFormattedText();
        }
        private int places
        {
            get
            {
                return 1 + (int)Math.Log10(Maximum);
            }
        }
    }
}
