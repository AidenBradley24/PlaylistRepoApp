using System.Collections.Frozen;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace PlaylistRepoLib.UserQueries;

/// <summary>
/// A safe way of allowing users to specify a string to query a database.
/// </summary>
/// <remarks>
/// Use <see cref="UserQueryableAttribute"/> to specify queryable public properties in the model class. <br/>
/// Use <see cref="PrimaryUserQueryableAttribute"/> to specify the primary property to compare on the model class.
/// </remarks>
/// <typeparam name="TModel">The type of queryable model</typeparam>
public interface IUserQueryProvider<TModel>
{
	/// <summary>
	/// Parse a user defined query of the <typeparamref name="TModel"/> type.
	/// </summary>
	/// <remarks>
	/// <para><b>Supported Operators:</b></para>
	/// <list type="table">
	///   <listheader>
	///     <term>Operator</term>
	///     <description>Description</description>
	///   </listheader>
	///   <item><term><c>=</c></term><description>equals</description></item>
	///   <item><term><c>!=</c></term><description>not equals</description></item>
	///   <item><term><c>^</c></term><description>starts with (string only)</description></item>
	///   <item><term><c>!^</c></term><description>does not start with (string only)</description></item>
	///   <item><term><c>$</c></term><description>ends with (string only)</description></item>
	///   <item><term><c>!$</c></term><description>does not end with (string only)</description></item>
	///   <item><term><c>*</c></term><description>contains (string only)</description></item>
	///   <item><term><c>!*</c></term><description>does not contain (string only)</description></item>
	///   <item><term><c>&lt;</c></term><description>less than (int only)</description></item>
	///   <item><term><c>&gt;</c></term><description>greater than (int only)</description></item>
	///   <item><term><c>&lt;=</c></term><description>less than or equal to (int only)</description></item>
	///   <item><term><c>&gt;=</c></term><description>greater than or equal to (int only)</description></item>
	///   <item><term><c>&amp;</c></term><description>logical AND with another term</description></item>
	/// </list>
	///
	/// <para><b>Notes:</b></para>
	/// <list type="bullet">
	///   <item>Commas (<c>,</c>) separate sections that are ORed together.</item>
	///   <item>Quotes inside values are parsed as string literals. Escape characters: <c>\"</c> and <c>\\</c>.</item>
	///   <item>All comparisons are case-insensitive.</item>
	///   <item>Whitespace outside of quotes is ignored.</item>
	/// </list>
	/// </remarks>
	/// <exception cref="InvalidUserQueryException"></exception>
	public IQueryable<TModel> EvaluateUserQuery(string queryText);
}

/// <inheritdoc cref="IUserQueryProvider{TModel}"/>
public sealed class UserQueryProvider<TModel> : IUserQueryProvider<TModel>
{
	private readonly IQueryable<TModel> root;
	private readonly FrozenDictionary<string, PropertyInfo> queryableProperties;
	private readonly Token? defaultTarget;

	private static readonly FrozenSet<string> operators = 
	[
		"=",     // equals
		"!=",    // not equals
		"^",     // starts with (string only)
		"!^",    // does not start with (string only)
		"$",     // ends with (string only)
		"!$",    // does not end with (string only)
		"*",     // contains (string only)
		"!*",    // does not contain (string only)
		"<",     // less than (int only)
		"<=",    // less than or equal to (int only)
		">",     // greater than (int only)
		">="     // greater than or equal to (int only)
	];

	/// <summary>
	/// Initialize a new provider for <typeparamref name="TModel"/>
	/// </summary>
	/// <param name="root">The base <see cref="IQueryable"/> to evaluate queries off of.<br/>
	/// To restrict access to certain entities, provide those restictions to this root.</param>
	/// <exception cref="UserQueryableMisconfigurationException"></exception>
	public UserQueryProvider(IQueryable<TModel> root)
	{
		this.root = root;
		queryableProperties = FrozenDictionary.ToFrozenDictionary(typeof(TModel).GetProperties()
			.Where(p => p.GetCustomAttributes<UserQueryableAttribute>(true).Any())
			.Select(p =>
			{
				if (!p.CanRead) throw new UserQueryableMisconfigurationException($"Type {typeof(TModel).FullName} {p.Name} is not readable.");
				return new KeyValuePair<string, PropertyInfo>(p.GetCustomAttributes<UserQueryableAttribute>(true).First().QueryName, p);
			}));
		string? defaultPropName = typeof(TModel).GetCustomAttribute<PrimaryUserQueryableAttribute>()?.PropertyName;
		var defaultProperty = defaultPropName != null ? typeof(TModel).GetProperty(defaultPropName) : null;
		if (defaultProperty == null && defaultPropName != null) 
			throw new UserQueryableMisconfigurationException($"Type {typeof(TModel).FullName} {defaultPropName} not found.");
		if (defaultProperty != null && !defaultProperty.CanRead) 
			throw new UserQueryableMisconfigurationException($"Type {typeof(TModel).FullName} {defaultProperty.Name} is not readable.");
		if (defaultProperty != null)
		{
			var att = defaultProperty.GetCustomAttribute<UserQueryableAttribute>() 
				?? throw new UserQueryableMisconfigurationException($"Type {typeof(TModel).FullName} {defaultProperty.Name} is not user queryable.");
			defaultTarget = new Token(att.QueryName, false);
		}
	}

	private readonly ParameterExpression model = Expression.Parameter(typeof(TModel), "x");

	public IQueryable<TModel> EvaluateUserQuery(string queryText)
	{
		IEnumerator<Token> tokens = Tokenize(queryText);

		Mode mode = Mode.first;
		Stack<Expression> terms = [];
		Expression? currentTerm = null;
		
		while (tokens.MoveNext())
		{
			switch (mode)
			{
				case Mode.first:
					Expression newTerm;
					if (tokens.Current.IsLiteral)
					{
						ArgumentNullException.ThrowIfNull(defaultTarget);
						newTerm = EvaluateTerm(defaultTarget, tokens.Current, null);
					}
					else
					{
						Token target = tokens.Current;
						if (!tokens.MoveNext()) throw new InvalidUserQueryException($"Incomplete query: {queryText} ...");
						Token op = tokens.Current;
						if (!tokens.MoveNext()) throw new InvalidUserQueryException($"Incomplete query: {queryText} ...");
						newTerm = EvaluateTerm(target, tokens.Current, op);
					}
					// if current term exists logical AND with existing
					currentTerm = currentTerm == null ? newTerm : Expression.AndAlso(currentTerm, newTerm);
					mode = Mode.finish;
				break;
				case Mode.finish:
					if (tokens.Current.IsLiteral) 
						throw new InvalidUserQueryException($"Literal must be seperated with a comma or &: {tokens.Current.Value}");
					if (tokens.Current.Value == ",")
					{
						if (currentTerm != null) terms.Push(currentTerm);
						currentTerm = null;
						mode = Mode.first;
					}
					else if (tokens.Current.Value == "&")
					{
						mode = Mode.first;
					}
					else
					{
						throw new InvalidUserQueryException($"Invalid operator: {tokens.Current.Value}");
					}
				break;
			}
		}

		if (currentTerm != null) 
			terms.Push(currentTerm);

		// Logical OR together terms
		if (terms.TryPop(out currentTerm))
		{
			while (terms.TryPop(out var ex))
			{
				currentTerm = Expression.OrElse(currentTerm, ex);
			}
			var lambda = Expression.Lambda<Func<TModel, bool>>(currentTerm, model);
			return root.Where(lambda);
		}
		else
		{
			return root;
		}
	}

	private PropertyInfo GetProperty(Token token)
	{
		bool exists = queryableProperties.TryGetValue(token.Value.ToLowerInvariant(), out var property);
		if (!exists) throw new InvalidUserQueryException($"Property \"{token.Value}\" not in type {typeof(TModel).FullName}\n" +
			$"Use {nameof(UserQueryableAttribute)} to specify properties as queryable.");
		return property!;
	}

	private Expression EvaluateTerm(Token target, Token compared, Token? op)
	{
		op ??= new Token("*", true);

		PropertyInfo targetProp = GetProperty(target);
		Expression left = Expression.Property(model, targetProp);

		Expression right;
		if (compared.IsLiteral)
		{
			object? parsed = ParseValue(targetProp.PropertyType, compared.Value);
			right = Expression.Constant(parsed, targetProp.PropertyType);
		}
		else
		{
			PropertyInfo comparedProp = GetProperty(compared);
			right = Expression.Property(model, comparedProp);
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

		return body;
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

	record Token(string Value, bool IsLiteral);

	enum Mode
	{
		first = 0,
		finish = 1,
	}
}

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
