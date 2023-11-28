using System;

namespace Antelcat.Parameterization;

[AttributeUsage(AttributeTargets.Parameter)]
public class ArgumentAttribute : Attribute
{
	/// <summary>
	/// FullName switch, e.g. --name
	/// </summary>
	public string? FullName { get; set; }

	/// <summary>
	/// ShortName switch, e.g. -n
	/// </summary>
	public string? ShortName { get; set; }

	public string? Description { get; set; }

	/// <summary>
	/// Must derive from <see cref="System.ComponentModel.StringConverter"/>
	/// </summary>
	/// <remarks>
	/// It's better to use a <see cref="System.ComponentModel.TypeConverterAttribute"/> on your custom type, which will be applied globally.
	/// </remarks>
	public Type? Converter { get; set; }

	/// <summary>
	/// When there is only a named switch but no value is provided, this string value will be used and converted to the target type.
	/// </summary>
	public string? DefaultValue { get; set; }
}