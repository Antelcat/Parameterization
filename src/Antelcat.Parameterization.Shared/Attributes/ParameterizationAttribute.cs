using System;

namespace Antelcat.Parameterization;

/// <summary>
/// Make this class parameterizable
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class ParameterizationAttribute : Attribute
{
	public bool CaseSensitive { get; set; }
}