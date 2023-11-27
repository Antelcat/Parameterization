using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Antelcat.Parameterization.SourceGenerators.Models;

public class AttributeProxy : IReadOnlyDictionary<string, object?>
{
	public AttributeSyntax Attribute { get; }

	private readonly Dictionary<string, AttributeArgumentSyntax> arguments = new();

	public AttributeProxy(AttributeSyntax attribute)
	{
		Attribute = attribute;
		if (attribute.ArgumentList == null) return;
		foreach (var argument in attribute.ArgumentList.Arguments)
		{
			var argumentName = argument.NameEquals?.Name.Identifier.ValueText;
			if (argumentName == null) continue;
			arguments.Add(argumentName, argument);
		}
	}

	public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
	{
		return arguments.Select(item =>
			new KeyValuePair<string, object?>(item.Key, GetArgumentValue(item.Value))).GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	public int Count => arguments.Count;

	public bool ContainsKey(string key)
	{
		return arguments.ContainsKey(key);
	}

	public bool TryGetValue(string key, out object? value)
	{
		if (!arguments.TryGetValue(key, out var argument))
		{
			value = null;
			return false;
		}

		value = GetArgumentValue(argument);
		return true;
	}

	public object? this[string key]
	{
		get
		{
			TryGetValue(key, out var value);
			return value;
		}
	}

	public IEnumerable<string> Keys => arguments.Keys;

	public IEnumerable<object?> Values => arguments.Values.Select(GetArgumentValue);

	private static object? GetArgumentValue(AttributeArgumentSyntax argument) =>
		argument.Expression switch
		{
			LiteralExpressionSyntax literal => literal.Token.Value,
			TypeOfExpressionSyntax @typeof => @typeof.Type,
			_ => null
		};
}