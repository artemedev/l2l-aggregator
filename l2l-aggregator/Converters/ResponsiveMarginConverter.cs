using Avalonia.Data.Converters;
using Avalonia;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace l2l_aggregator.Converters
{
    public class ResponsiveMarginConverter : IValueConverter
    {
        /// <summary>
        /// Минимальное значение отступа
        /// </summary>
        public double MinMargin { get; set; } = 4;

        /// <summary>
        /// Максимальное значение отступа
        /// </summary>
        public double MaxMargin { get; set; } = 20;

        /// <summary>
        /// Масштабный коэффициент (на сколько процентов от ширины рассчитывается отступ)
        /// </summary>
        public double ScaleFactor { get; set; } = 0.02; // 2%

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double width)
            {
                double min = MinMargin;
                double max = MaxMargin;

                if (parameter is string paramStr)
                {
                    var parts = paramStr.Split(',');
                    if (parts.Length == 2 &&
                        double.TryParse(parts[0], out var parsedMin) &&
                        double.TryParse(parts[1], out var parsedMax))
                    {
                        min = parsedMin;
                        max = parsedMax;
                    }
                }

                double margin = Math.Clamp(width * ScaleFactor, min, max);
                return new Thickness(margin);
            }

            return new Thickness(MinMargin);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }
}
