using System.Collections.Frozen;
using System.Globalization;
using System.Reflection;
using System.Text;

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

	/// <summary>
	/// Construct a GET request URL to return a page of contents from a REST API utilizing User Queries
	/// </summary>
	public static string ConstructRequest(string baseURL, string userFilter, string? orderBy, bool descending, int pageSize, int currentPage)
	{
		var builder = new StringBuilder(baseURL);

		if (!baseURL.EndsWith('/'))
			builder.Append('/');

		builder.Append("?query=");
		builder.Append(Uri.EscapeDataString(userFilter));

		if (orderBy != null)
		{
			builder.Append(Uri.EscapeDataString(descending ? " orderbydescending " : " orderby "));
			builder.Append(orderBy);
		}

		builder.Append("&pageSize=");
		builder.Append(pageSize);
		builder.Append("&currentPage=");
		builder.Append(currentPage);

		return builder.ToString();
	}

	/// <summary>
	/// Returns the queryable property names of a record in a set order.
	/// </summary>
	public static string[] GetQueryableProperties(Type modelType)
	{
		return [.. modelType.GetCustomAttributes<UserQueryableAttribute>().OrderBy(a => a.Order).Select(a => a.QueryName)];
	}

	/// <summary>
	/// Returns the values of a record in the same order as <see cref="GetQueryableProperties(Type)"/>
	/// </summary>
	public static string[] GetQueryablePropertiesRecord<TModel>(TModel record)
	{
		if (record == null) throw new ArgumentNullException(nameof(record));
		var type = typeof(TModel);
		var props = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
		var queryableAttrs = type.GetCustomAttributes<UserQueryableAttribute>().OrderBy(a => a.Order).ToArray();
		if (queryableAttrs.Length == 0)
			return [];

		var result = new string[queryableAttrs.Length];
		for (int i = 0; i < queryableAttrs.Length; i++)
		{
			var attr = queryableAttrs[i];
			var prop = props.FirstOrDefault(p => p.GetCustomAttribute<UserQueryableAttribute>()?.QueryName == attr.QueryName)
				?? throw new UserQueryableMisconfigurationException($"Property with QueryName '{attr.QueryName}' not found on type '{type.Name}'.");
			var value = prop.GetValue(record);
			result[i] = value?.ToString() ?? "";
		}
		return result;
	}


	/// <summary>
	/// Split a string into segments based on single or double quotes. Otherwise split by spaces. Removes quotes.
	/// </summary>
	public static IEnumerator<Token> Tokenize(string? input)
	{
		if (string.IsNullOrEmpty(input)) yield break;

		const int NON_LITERAL_MODE = 0;
		const int SINGLE_QUOTE_MODE = 1;
		const int DOUBLE_QUOTE_MODE = 2;
		const int LITERAL_MODE = 3;

		int mode = NON_LITERAL_MODE;

		int start = 0;
		StringBuilder currentSegment = new();
		string part;

		for (int i = 0; i < input.Length; i++)
		{
			char c = input[i];

			if (mode == SINGLE_QUOTE_MODE || mode == DOUBLE_QUOTE_MODE)
			{
				if (mode == SINGLE_QUOTE_MODE && c == '\'' || mode == DOUBLE_QUOTE_MODE && c == '\"')
				{
					currentSegment.Append(input[start..i]);
					start = i + 1;
					mode = LITERAL_MODE;
				}

				if ((mode == DOUBLE_QUOTE_MODE || mode == SINGLE_QUOTE_MODE) && c == '\\' &&
					(i < input.Length - 1))
				{
					// preserve previous characters of segment
					part = input[start..i];
					currentSegment.Append(part);
					// keep next character
					currentSegment.Append(input[++i]);
					start = i + 1;
				}
			}
			else
			{
				// normal mode
				switch (c)
				{
					case '\'':
						part = input[start..i];
						if (!string.IsNullOrWhiteSpace(part))
							currentSegment.Append(part);
						start = i + 1;
						mode = SINGLE_QUOTE_MODE;
						break;
					case '\"':
						part = input[start..i];
						if (!string.IsNullOrWhiteSpace(part))
							currentSegment.Append(part); start = i + 1;
						mode = DOUBLE_QUOTE_MODE;
						break;
					case ',':
					case '&':
					case ' ':
						part = input[start..i];
						if (currentSegment.Length != 0 || !string.IsNullOrWhiteSpace(part))
						{
							currentSegment.Append(part);
							yield return new Token(currentSegment.ToString(), mode == LITERAL_MODE);
							mode = NON_LITERAL_MODE;
							currentSegment.Clear();
						}
						start = i + 1;
						if (c == ',' || c == '&') yield return new Token(c.ToString(), false);
						break;
					case '\\':
						// preserve previous characters of segment
						part = input[start..i];
						currentSegment.Append(part);
						// keep next character
						currentSegment.Append(input[++i]);
						start = i + 1;
						break;
					default:
						// words starting with a number are considered literal
						if (i == start && char.IsNumber(c)) mode = LITERAL_MODE;
						break;
				}
			}
		}

		part = input[start..];
		currentSegment.Append(part);
		yield return new Token(currentSegment.ToString(), mode != NON_LITERAL_MODE);
	}
}

public record Token(string Value, bool IsLiteral);

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
			if (type.IsEnum) return Enum.Parse(type, literal);
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
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class PrimaryUserQueryableAttribute(string propertyName) : Attribute
{
	public string PropertyName { get; } = propertyName;
}

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class UserQueryableAttribute(string queryName) : Attribute
{
	public string QueryName { get; } = queryName;
	public int Order { get; set; } = 0;
}

public class UserQueryableMisconfigurationException(string? message) : Exception(message) { }

public class InvalidUserQueryException : Exception
{
	public InvalidUserQueryException(string message) : base(message) { }

	public InvalidUserQueryException() { }

	public InvalidUserQueryException(string? message, Exception? innerException) : base(message, innerException) { }
}
