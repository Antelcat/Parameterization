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
	public Type? Converter { get; set; }
}