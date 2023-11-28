using System.ComponentModel;
using Antelcat.Parameterization.Demo.Converters;

namespace Antelcat.Parameterization.Demo.Models;

[TypeConverter(typeof(ImageConverter))]
public record Image(string Name, Version? Version)
{
    public override string ToString()
    {
        return $"{Name}:{Version?.ToString() ?? "latest"}";
    }
}