using System.ComponentModel;
using System.Globalization;
using Antelcat.Parameterization.Demo.Models;

namespace Antelcat.Parameterization.Demo.Converters;

public class ImageConverter : StringConverter
{
	public override object ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object? value)
	{
		if (value is not string str) throw new ArgumentException("Invalid image string format");
		var args = str.Split(':');
		return args switch
		{
			{ Length: 1 } => new Image(str, null),
			{ Length: 2 } => new Image(args[0], Version.Parse(args[1])),
			_ => throw new ArgumentException("Invalid image string format")
		};
	}
}