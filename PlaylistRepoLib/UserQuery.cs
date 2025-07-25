using System;
using System.Collections;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PlaylistRepoLib;

/*
	   QUERYABLES:
	   id -- implicit                  int
	   remote                          int
	   name                            string
	   artists                         string
	   album                           string
	   description                     string
	   rating                          int
	   time                            int

	   OPERATORS:
	   =   equals
	   !=  not equals
	   ^   starts with                 string only
	   !^  does not start with         string only
	   $   ends with                   string only
	   !$  does not end with           string only
	   *   contains                    string only
	   !*  does not contain            string only
	   <   less than                   int only
	   >   greater than                int only
	   <=  less than or equal to       int only
	   >=  greater than or equal to    int only

	   &   and (another term)

	   commas act as a seperate section which are ored together    
	   quotes inside values are parsed as strings, \" and \\ can be used
	   everything is case insensitive
	   whitespace outside of quotes is ignored

*/

/// <summary>
/// A safe way of allowing users to specify a string to query a database indirectly.
/// </summary>
public sealed class UserQueryProvider<T>
{
	private readonly IQueryable<T> root;
	private readonly FrozenDictionary<string, PropertyInfo> queryableProperties;
	private readonly Token? defaultTarget;

	public UserQueryProvider(IQueryable<T> root)
	{
		this.root = root;
		queryableProperties = FrozenDictionary.ToFrozenDictionary(typeof(T).GetProperties()
			.Where(p => p.GetCustomAttributes<UserQueryableAttribute>(true).Any())
			.Select(p =>
			{
				if (!p.CanRead) throw new UserQueryableMisconfigurationException($"Type {typeof(T).FullName} {p.Name} is not readable.");
				return new KeyValuePair<string, PropertyInfo>(p.GetCustomAttributes<UserQueryableAttribute>(true).First().QueryName, p);
			}));
		string? defaultPropName = typeof(T).GetCustomAttribute<PrimaryUserQueryableAttribute>()?.PropertyName;
		var defaultProperty = defaultPropName != null ? typeof(T).GetProperty(defaultPropName) : null;
		if (defaultProperty == null && defaultPropName != null) 
			throw new UserQueryableMisconfigurationException($"Type {typeof(T).FullName} {defaultPropName} not found.");
		if (defaultProperty != null && !defaultProperty.CanRead) 
			throw new UserQueryableMisconfigurationException($"Type {typeof(T).FullName} {defaultProperty.Name} is not readable.");
		if (defaultProperty != null)
		{
			var att = defaultProperty.GetCustomAttribute<UserQueryableAttribute>() 
				?? throw new UserQueryableMisconfigurationException($"Type {typeof(T).FullName} {defaultProperty.Name} is not user queryable.");
			defaultTarget = new Token(att.QueryName, false);
		}
	}

	public IQueryable<T> EvaluateUserQuery(string queryText)
	{
		Mode mode = Mode.first;
		IQueryable<T> query = root;
		var tokens = Tokenize(queryText);
		while (tokens.MoveNext())
		{
			switch (mode)
			{
				case Mode.first:
					if (tokens.Current.IsLiteral)
					{
						ArgumentNullException.ThrowIfNull(defaultTarget);
						query = query.Where(GetPredicate(defaultTarget, tokens.Current, null));
					}
					else
					{
						Token target = tokens.Current;
						if (!tokens.MoveNext()) throw new InvalidUserQueryException($"Incomplete query: {queryText} ...");
						Token op = tokens.Current;
						if (!tokens.MoveNext()) throw new InvalidUserQueryException($"Incomplete query: {queryText} ...");
						query = query.Where(GetPredicate(target, tokens.Current, op));
					}
				break;
			}

		}

		return query;
	}

	private PropertyInfo GetProperty(Token token)
	{
		bool exists = queryableProperties.TryGetValue(token.Value.ToLowerInvariant(), out var property);
		if (!exists) throw new InvalidUserQueryException($"Property \"{token.Value}\" not in type {typeof(T).FullName}\n" +
			$"Use {nameof(UserQueryableAttribute)} to specify properties as queryable.");
		return property!;
	}

	private Expression<Func<T, bool>> GetPredicate(Token target, Token compared, Token? op)
	{
		op ??= new Token("*", true);

		var param = Expression.Parameter(typeof(T), "x");

		PropertyInfo targetProp = GetProperty(target);
		Expression left = Expression.Property(param, targetProp);

		Expression right;
		if (compared.IsLiteral)
		{
			object? parsed = ParseValue(targetProp.PropertyType, compared.Value);
			right = Expression.Constant(parsed, targetProp.PropertyType);
		}
		else
		{
			PropertyInfo comparedProp = GetProperty(compared);
			right = Expression.Property(param, comparedProp);
		}

		// Normalize for string-insensitive comparison where applicable
		bool isString = targetProp.PropertyType == typeof(string);
		bool isInt = targetProp.PropertyType == typeof(int);

		Expression body = op.Value switch
		{
			"=" => Expression.Equal(left, right),
			"!=" => Expression.NotEqual(left, right),

			"<" when isInt => Expression.LessThan(left, right),
			"<=" when isInt => Expression.LessThanOrEqual(left, right),
			">" when isInt => Expression.GreaterThan(left, right),
			">=" when isInt => Expression.GreaterThanOrEqual(left, right),

			"^" when isString => CallInsensitive(left, right, nameof(string.StartsWith)),
			"!^" when isString => Expression.Not(CallInsensitive(left, right, nameof(string.StartsWith))),
			"$" when isString => CallInsensitive(left, right, nameof(string.EndsWith)),
			"!$" when isString => Expression.Not(CallInsensitive(left, right, nameof(string.EndsWith))),
			"*" when isString => CallInsensitive(left, right, nameof(string.Contains)),
			"!*" when isString => Expression.Not(CallInsensitive(left, right, nameof(string.Contains))),

			_ => throw new InvalidUserQueryException($"Unsupported or type-mismatched operator \"{op.Value}\" for property \"{target.Value}\"")
		};

		return Expression.Lambda<Func<T, bool>>(body, param);
	}


	private static MethodCallExpression CallInsensitive(Expression left, Expression right, string methodName)
	{
		var comparison = Expression.Constant(StringComparison.OrdinalIgnoreCase, typeof(StringComparison));
		return Expression.Call(left, typeof(string).GetMethod(methodName, new[] { typeof(string), typeof(StringComparison) })!, right, comparison);
	}

	private static object ParseValue(Type targetType, string value)
	{
		try
		{
			if (targetType == typeof(int))
				return int.Parse(value);
			if (targetType == typeof(string))
				return value;
			throw new InvalidUserQueryException($"Unsupported target type: {targetType.Name}");
		}
		catch (Exception ex)
		{
			throw new InvalidUserQueryException($"Could not parse value \"{value}\" as {targetType.Name}", ex);
		}
	}

	/// <summary>
	/// Split a string into segments based on single or double quotes. Otherwise split by spaces. Removes quotes.
	/// </summary>
	static IEnumerator<Token> Tokenize(string? input)
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

				if (mode == DOUBLE_QUOTE_MODE && c == '\\' &&
					(i < input.Length - 1 &&
					(input[i + 1] == '\\' ||
					input[i + 1] == '$' ||
					input[i + 1] == '\"') ||
					input[i + 1] == '\n'))
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
					case ' ':
						part = input[start..i];
						if (currentSegment.Length != 0 || !string.IsNullOrWhiteSpace(part))
						{
							currentSegment.Append(part);
							yield return new Token(currentSegment.ToString(), mode == LITERAL_MODE);
							currentSegment.Clear();
						}
						start = i + 1;
						break;
					case '\\':
						// preserve previous characters of segment
						part = input[start..i];
						currentSegment.Append(part);
						// keep next character
						currentSegment.Append(input[++i]);
						start = i + 1;
						break;
				}
			}
		}

		part = input[start..];
		currentSegment.Append(part);
		yield return new Token(currentSegment.ToString(), mode == LITERAL_MODE);
	}

	record Token(string Value, bool IsLiteral);

	enum Mode
	{
		first = 0,
		finish = 1,
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
	public InvalidUserQueryException(string message) : base(message)
	{
	}

	public InvalidUserQueryException()
	{
	}

	public InvalidUserQueryException(string? message, Exception? innerException) : base(message, innerException)
	{
	}
}

