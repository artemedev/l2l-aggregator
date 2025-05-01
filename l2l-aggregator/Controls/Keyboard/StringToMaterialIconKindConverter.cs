using Avalonia.Data.Converters;
using Material.Icons;
using System;
using System.Globalization;

namespace l2l_aggregator.Controls.Keyboard
{
    public class StringToMaterialIconKindConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string iconName && Enum.TryParse(iconName, out MaterialIconKind iconKind))
                return iconKind;

            return default(MaterialIconKind); // Или MaterialIconKind.QuestionMark
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
