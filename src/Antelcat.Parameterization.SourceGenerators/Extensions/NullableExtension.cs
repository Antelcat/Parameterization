using System;

namespace Antelcat.Parameterization.SourceGenerators.Extensions;

public static class NullableExtension
{
	/// <summary>
	/// 将一个可能为空的转成不可空，如果为null将抛出<see cref="NullReferenceException"/>
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="t"></param>
	/// <returns></returns>
	/// <exception cref="NullReferenceException"></exception>
	public static T NotNull<T>(this T? t) where T : notnull => t ?? throw new NullReferenceException();

	/// <summary>
	/// 将一个可能为空的转成不可空，如果为null将抛出<see cref="NullReferenceException"/>
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <typeparam name="TException"></typeparam>
	/// <param name="t"></param>
	/// <returns></returns>
	/// <exception cref="NullReferenceException"></exception>
	public static T NotNull<T, TException>(this T? t)
		where T : notnull where TException : Exception, new() => t ?? throw new TException();

	/// <summary>
	/// 将一个可能为空的转成不可空，如果为null将抛出<see cref="NullReferenceException"/>
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="t"></param>
	/// <returns></returns>
	/// <exception cref="NullReferenceException"></exception>
	public static T NotNull<T>(this object? t) where T : notnull => (T?)t ?? throw new NullReferenceException();

	/// <summary>
	/// 将一个可能为空的转成不可空，如果为null将抛出<see cref="NullReferenceException"/>
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="t"></param>
	/// <param name="errorMessage"></param>
	/// <returns></returns>
	/// <exception cref="NullReferenceException"></exception>
	public static T NotNull<T>(this T? t, string errorMessage) =>
		t ?? throw new NullReferenceException(errorMessage);
}