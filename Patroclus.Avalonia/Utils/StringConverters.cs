using System;
using System.Globalization;
using Avalonia;
using Avalonia.Markup;
using Avalonia.Utilities;

namespace Patroclus
{
    /// <summary>
    /// Provides a set of useful <see cref="IValueConverter"/>s for working with string values.
    /// </summary>
    public static class StringConverters
    {
       
        /// <summary>
        /// A value converter that applies Sting.Format to the input
        /// </summary>
       
        public static readonly IValueConverter StringFormat =
             new FuncValueParameterConverter<object,string,string>((x,f) => String.Format(f,x));


    }
    // Copyright (c) The Avalonia Project. All rights reserved.
    // Licensed under the MIT license. See licence.md file in the project root for full license information.

    /// <summary>
    /// A general purpose <see cref="IValueConverter"/> that uses a <see cref="Func{T1,T2,TResult}"/>
    /// to provide the converter logic.
    /// </summary>
    /// <typeparam name="TIn">The input type.</typeparam>
    /// <typeparam name="TParam">The parameter type.</typeparam>
    /// <typeparam name="TOut">The output type.</typeparam>
    public class FuncValueParameterConverter<TIn, TParam, TOut> : IValueConverter
        {
            private readonly Func<TIn,TParam , TOut> _convert;

        /// <summary>
        /// Initializes a new instance of the <see cref="FuncValueConverter{TIn, TParam, TOut}"/> class.
        /// </summary>
        /// <param name="convert">The convert function.</param>
        public FuncValueParameterConverter(Func<TIn, TParam, TOut> convert)
        {
            _convert = convert;
        }

        /// <inheritdoc/>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TIn || (value == null && TypeUtilities.AcceptsNull(typeof(TIn))))
            {
                return _convert((TIn)value,(TParam)parameter);
            }
            else
            {
                return AvaloniaProperty.UnsetValue;
            }
        }

        /// <inheritdoc/>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
