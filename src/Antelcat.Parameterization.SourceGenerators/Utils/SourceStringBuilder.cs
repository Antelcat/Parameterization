using System;
using System.Text;

namespace Antelcat.Parameterization.SourceGenerators.Utils;

public class SourceStringBuilder
{
    private readonly string indentString;
    private readonly StringBuilder stringBuilder;

    private int currentIndentCount;
    private bool isLineStart = true;

    public SourceStringBuilder(string indentString = "    ", int initialIndentCount = 0, int capacity = 16)
    {
        this.indentString = indentString;

        if (initialIndentCount < 0) throw new ArgumentOutOfRangeException(nameof(initialIndentCount));
        currentIndentCount = initialIndentCount;

        stringBuilder = new StringBuilder(capacity);
    }

    public SourceStringBuilder Indent(int count = 1)
    {
        if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count));
        currentIndentCount += count;
        return this;
    }

    public SourceStringBuilder OutDent(int count = 1)
    {
        if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count));
        currentIndentCount -= count;
        if (currentIndentCount < 0) currentIndentCount = 0;
        return this;
    }

    public SourceStringBuilder Append(string value)
    {
        var lines = value.Split('\n');
        if (lines.Length == 1)
        {
            AppendIndent();
            stringBuilder.Append(lines[0]);
        }
        else
        {
            foreach (var line in lines)
            {
                AppendIndent();
                stringBuilder.AppendLine(line);
                isLineStart = true;
            }
        }

        return this;
    }

    public SourceStringBuilder AppendLine(string value)
    {
        Append(value).Append('\n');
        isLineStart = true;
        return this;
    }

    public SourceStringBuilder Append(char value)
    {
        AppendIndent();

        stringBuilder.Append(value);
        isLineStart = value == '\n';

        return this;
    }

    public SourceStringBuilder AppendLine(char value)
    {
        Append(value).Append('\n');
        isLineStart = true;
        return this;
    }

    public override string ToString()
    {
        return stringBuilder.ToString();
    }

    private void AppendIndent()
    {
        if (!isLineStart) return;
        for (var i = 0; i < currentIndentCount; i++)
        {
            stringBuilder.Append(indentString);
        }

        isLineStart = false;
    }
}