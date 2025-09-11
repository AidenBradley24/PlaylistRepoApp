using System;
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
	/// Synthesize tokens from a given <paramref name="input"/> span of characters.
	/// <para>The following are rules of synthesis:</para>
	/// <list type="bullet">
	/// <item>Tokens are seperated by whitespace.</item>
	/// <item>Literal phrases can be created within single or double quotes.</item>
	/// <item>Within literal phrases, certain escape characters are available <code>\' \" \n \t \\</code></item>
	/// <item>Tokens beginning with a number are automatically considered literal and can only contain digits and exactly one or zero decimal points.</item>
	/// <item>Single or double quotes must be closed.</item>
	/// <item>Non-literal tokens beginning with a letter are terminated by symbols.</item>
	/// <item>Non-literal tokens beginning with a symbol are terminated by letters and digits.</item>
	/// </list>
	/// </summary>
	/// <param name="input"></param>
	/// <returns>A lazy collection of tokens.</returns>
	/// <exception cref="ArgumentException">When input is invalid</exception>
	public static IEnumerable<Token> Tokenize(string? input)
	{
		if (string.IsNullOrEmpty(input)) yield break;

		StringBuilder chunk = new();
		var mode = TokenFlags.StartMode;
		int start = 0;

		bool CheckNullToken()
		{
			return chunk.Length == 0;
		}

		Token MakeToken()
		{
			string value = chunk.ToString();
			bool isLiteral = mode.HasFlag(TokenFlags.Literal);
			chunk.Clear();
			mode = TokenFlags.StartMode;
			return new Token(value, isLiteral);
		}
		
		for (int i = 0; i < input.Length; i++)
		{
			char c = input[i];
			if (mode == TokenFlags.StartMode)
			{
				switch (c)
				{
					case '\"':
						mode = TokenFlags.DoubleQuoteMode;
						start = i + 1;
						continue;
					case '\'':
						mode = TokenFlags.SingleQuoteMode;
						start = i + 1;
						continue;
					default:
						if (char.IsLetter(c))
						{
							mode = TokenFlags.LetterMode;
						}
						else if (char.IsDigit(c))
						{
							mode = TokenFlags.NumberMode;
						}
						else if (char.IsSymbol(c) || char.IsPunctuation(c))
						{
							mode = TokenFlags.SymbolMode;
						}
						else if (char.IsWhiteSpace(c))
						{
							start = i + 1;
						}
						continue;
				}
			}

			if (mode.HasFlag(TokenFlags.Quoted))
			{
				// INSIDE QUOTES
				if (c == '\\')
				{
					// escape chars
					if (i == input.Length - 1) throw new ArgumentException("Incomplete escape character.", nameof(input));
					char p1 = input[i + 1];
					char resultChar = p1 switch
					{
						'n' => '\n',
						't' => '\t',
						'\'' => '\'',
						'\"' => '\"',
						'\\' => '\\',
						_ => throw new ArgumentException("Invalid escape character: \\" + p1, nameof(input))
					};
					chunk.Append(input[start..i]);
					chunk.Append(resultChar);
					start = ++i + 1;
					continue;
				}

				if (mode == TokenFlags.SingleQuoteMode && c == '\'' || mode == TokenFlags.DoubleQuoteMode && c == '\"')
				{
					chunk.Append(input.AsSpan()[start..i]);
					start = i + 1;
					yield return MakeToken();
					continue;
				}

				continue;
			}

			if (char.IsWhiteSpace(c))
			{
				chunk.Append(input[start..i]);
				start = i + 1;
				if (CheckNullToken()) continue;
				yield return MakeToken();
				continue;
			}

			if (mode.HasFlag(TokenFlags.NumberMode))
			{
				if (char.IsLetter(c)) throw new ArgumentException("Numbers cannot contain a letter.", nameof(input));
				if (c == '.')
				{
					if (mode == TokenFlags.DecimalMode)
						throw new ArgumentException("Numbers cannot contain more than one decimal point.");
					mode = TokenFlags.DecimalMode;
				}
				else if (char.IsSymbol(c) || char.IsPunctuation(c))
				{
					chunk.Append(input.AsSpan()[start..i]);
					start = i--;
					yield return MakeToken();
				}
				continue;
			}

			if ((mode == TokenFlags.LetterMode || mode.HasFlag(TokenFlags.NumberMode)) && (char.IsSymbol(c) || char.IsPunctuation(c)))
			{
				chunk.Append(input.AsSpan()[start..i]);
				start = i--;
				yield return MakeToken();
				continue;
			}

			if (mode == TokenFlags.SymbolMode && (!(char.IsSymbol(c) || char.IsPunctuation(c)) || c == '\'' || c == '\"'))
			{
				chunk.Append(input.AsSpan()[start..i]);
				start = i--;
				yield return MakeToken();
				continue;
			}
		}

		if (mode.HasFlag(TokenFlags.Quoted)) throw new ArgumentException("Quotes must be terminated.", nameof(input));
		chunk.Append(input[start..]);
		if (CheckNullToken()) yield break;
		yield return MakeToken();
	}

	[Flags]
	private enum TokenFlags
	{
		NonLiteral = 0,
		Literal = 1,
		NonQuoted = 2,
		Quoted = 4,

		StartMode = NonLiteral | 8,
		NumberMode = NonQuoted | Literal | 8,
		DecimalMode = NumberMode | 16,
		LetterMode = NonQuoted | NonLiteral | 8,
		SymbolMode = NonQuoted | NonLiteral | 8 | 16,
		SingleQuoteMode = Quoted | Literal,
		DoubleQuoteMode = Quoted | Literal | 8,
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
public sealed class UserQueryableAttribute(string queryName) : Attribute
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
