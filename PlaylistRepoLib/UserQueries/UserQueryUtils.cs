using System.Collections.Frozen;
using System.Globalization;

namespace PlaylistRepoLib.UserQueries;

public static class UserQueryExtensions
{
	/// <inheritdoc cref="UserQueryProvider{TModel}.EvaluateUserQuery(string)"/>
	/// <exception cref="UserQueryableMisconfigurationException"></exception>
	public static IQueryable<TModel> EvaluateUserQuery<TModel>(this IQueryable<TModel> baseQueryable, string userQuery)
	{
		var provider = new UserQueryProvider<TModel>(baseQueryable);
		return provider.EvaluateUserQuery(userQuery);
	}
}

internal static class UserQueryTypeParsers
{
	public static readonly Dictionary<Type, Func<string, object>> Parsers = new()
	{
		[typeof(int)] = s => int.Parse(s, CultureInfo.InvariantCulture),
		[typeof(double)] = s => double.Parse(s, CultureInfo.InvariantCulture),
		[typeof(float)] = s => float.Parse(s, CultureInfo.InvariantCulture),
		[typeof(TimeSpan)] = s => TimeSpan.Parse(s, CultureInfo.InvariantCulture),
		[typeof(string)] = s => s,
		// add more types here
	};

	public static object Parse(Type type, string literal)
	{
		if (!Parsers.TryGetValue(type, out var parser))
			throw new InvalidUserQueryException($"Unsupported type: {type.Name}");

		try
		{
			return parser(literal);
		}
		catch (FormatException ex)
		{
			throw new InvalidUserQueryException($"Unable to parse literal \"{literal}\" as type {type.FullName}", ex);
		}
	}
}

internal static class UserQueryOperatorCapabilities
{
	private static readonly FrozenSet<Type> ComparableTypes =
	[
		typeof(int), typeof(double), typeof(float), typeof(TimeSpan)
	];

	private static readonly FrozenSet<Type> StringCompatibleOperators = [typeof(string)];

	public static bool SupportsComparison(Type type)
	{
		return ComparableTypes.Contains(type);
	}

	public static bool SupportsStringMatch(Type type)
	{
		return StringCompatibleOperators.Contains(type);
	}
}

/// <summary>
/// When there is no specified property target, assume this property is the target.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple=false, Inherited=false)]
public sealed class PrimaryUserQueryableAttribute(string propertyName) : Attribute
{
	public string PropertyName { get; } = propertyName;
}

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class UserQueryableAttribute(string queryName) : Attribute
{
	public string QueryName { get; } = queryName;
}

public class UserQueryableMisconfigurationException(string? message) : Exception(message) { }

public class InvalidUserQueryException : Exception
{
	public InvalidUserQueryException(string message) : base(message) { }

	public InvalidUserQueryException() { }

	public InvalidUserQueryException(string? message, Exception? innerException) : base(message, innerException) { }
}
