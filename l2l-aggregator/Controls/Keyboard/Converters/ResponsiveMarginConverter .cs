using Avalonia;
using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace l2l_aggregator.Controls.Keyboard.Converters
{
    public class ResponsiveMarginConverter : IMultiValueConverter
    {
        public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values[0] is double width)
            {
                double margin = Math.Max(2, width * 0.01); // Пример: 1% от ширины, минимум 2
                return new Thickness(margin);
            }
            return new Thickness(7); // Default fallback
        }
    }
}
