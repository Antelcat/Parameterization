using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Antelcat.Parameterization.SourceGenerators.Extensions;

public static class EnumerableExtension
{
	/// <summary>
	/// Python: enumerator
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="enumerable"></param>
	/// <param name="startIndex"></param>
	/// <param name="step"></param>
	/// <returns></returns>
	public static IEnumerable<(int index, T value)> WithIndex<T>(
		this IEnumerable<T> enumerable,
		int startIndex = 0,
		int step = 1)
	{

		foreach (var item in enumerable)
		{
			yield return (startIndex, item);
			startIndex += step;
		}
	}

	public static IEnumerable<ValueTuple<T1, T2>> Zip<T1, T2>(
		this IEnumerable<T1> source1,
		IEnumerable<T2> source2)
	{
		return source1.Zip(source2, static (a, b) => (a, b));
	}

	public static ObservableCollection<T> ToObservableCollection<T>(this IEnumerable<T> enumerable)
	{
		return new(enumerable);
	}

	public static IEnumerable<T> Reversed<T>(this IList<T> list)
	{
		for (var i = list.Count - 1; i >= 0; i--)
		{
			yield return list[i];
		}
	}

	public static int FindIndexOf<T>(this IList<T> list, Predicate<T> predicate)
	{
		for (var i = 0; i < list.Count; i++)
		{
			if (predicate(list[i]))
			{
				return i;
			}
		}

		return -1;
	}

	/// <summary>
	/// 完全枚举一个 <see cref="IEnumerable"/>，并丢弃所有元素
	/// </summary>
	/// <param name="enumerable"></param>
	[MethodImpl(MethodImplOptions.NoOptimization)]
	public static void Discard(this IEnumerable enumerable)
	{
		foreach (var _ in enumerable) { }
	}

	/// <summary>
	/// 完全枚举一个 <see cref="IEnumerable{T}"/>，并丢弃所有元素
	/// </summary>
	/// <param name="enumerable"></param>
	/// <typeparam name="T"></typeparam>
	[MethodImpl(MethodImplOptions.NoOptimization)]
	[DebuggerNonUserCode]
	public static void Discard<T>(this IEnumerable<T> enumerable)
	{
		foreach (var _ in enumerable) { }
	}
}