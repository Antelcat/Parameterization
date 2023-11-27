using System;

namespace Antelcat.Parameterization;

[AttributeUsage(AttributeTargets.Method)]
public class CommandAttribute : Attribute
{
	public string? FullName { get; set; }

	public string? ShortName { get; set; }

	public string? Description { get; set; }
}