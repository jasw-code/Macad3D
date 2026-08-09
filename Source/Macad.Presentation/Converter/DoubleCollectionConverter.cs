using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;

namespace Macad.Presentation;

[ContentProperty("Converter")]
[ValueConversion(typeof(double[]), typeof(DoubleCollection))]
public class DoubleCollectionConverter : ConverterMarkupExtension<DoubleCollectionConverter>
{
    public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double[] array)
        {
            IEnumerable<double> enumerable = array;
            switch (parameter)
            {
                case double dscale:
                    enumerable = enumerable.Select(d => d * dscale);
                    break;
                case string str:
                    if(double.TryParse(str, NumberStyles.Float, CultureInfo.InvariantCulture, out double sscale))
                    {
                        enumerable = enumerable.Select(d => d * sscale);
                    }
                    break;
            }
            return new DoubleCollection(enumerable);
        }
        return new DoubleCollection();
    }

    //--------------------------------------------------------------------------------------------------

    public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is DoubleCollection collection)
        {
            return collection.ToArray();
        }
        return new double[0];
    }

    //--------------------------------------------------------------------------------------------------

}