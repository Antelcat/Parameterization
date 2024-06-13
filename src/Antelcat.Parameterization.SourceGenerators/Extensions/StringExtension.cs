namespace Antelcat.Parameterization.SourceGenerators.Extensions;

public static class StringExtension
{
    public static string Escape(this string? s) => s == null ? "null" : $"\"{s.Replace("\"", "\\\"")}\"";
    
    public static string If(this string s, bool condition) => condition ? s : string.Empty;
    
    public static string? IfNullOrEmpty(this string? s, string? replacement) => string.IsNullOrEmpty(s) ? replacement : s;
}