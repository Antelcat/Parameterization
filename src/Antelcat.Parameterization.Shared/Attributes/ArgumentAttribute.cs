using System;

namespace Antelcat.Parameterization;

[AttributeUsage(AttributeTargets.Parameter)]
public class ArgumentAttribute : Attribute
{
	public string? FullName { get; set; }

	public string? ShortName { get; set; }

	public string? Description { get; set; }

	/// <summary>
	/// Must derive from <see cref="System.ComponentModel.StringConverter"/>
	/// </summary>
	/// <remarks>
	/// It's better to use a <see cref="System.ComponentModel.TypeConverterAttribute"/> on your custom type.
	/// </remarks>
	public Type? Converter { get; set; }
	
	public string? DefaultValue { get; set; }
}