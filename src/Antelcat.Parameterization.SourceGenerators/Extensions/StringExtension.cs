namespace Antelcat.Parameterization.SourceGenerators.Extensions;

public static class StringExtension
{
    public static string Escape(this string? s)
    {
        return s == null ? "null" : $"\"{s.Replace("\"", "\\\"")}\"";
    }
}