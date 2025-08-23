using System.Collections.Frozen;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

using static PlaylistRepoLib.UserQueries.UserQueryExtensions;

namespace PlaylistRepoLib.UserQueries;

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

		Mode mode = Mode.inital;
		Stack<Expression> terms = [];
		Expression? currentTerm = null;

		Token? sortProperty = null;
		bool descending = false;

		while (tokens.MoveNext())
		{
			switch (mode)
			{
				case Mode.inital:
					if (!tokens.Current.IsLiteral)
					{
						if (tokens.Current.Value == "orderby")
						{
							if (!tokens.MoveNext()) throw new InvalidUserQueryException($"Incomplete: include property to sort by: {queryText} ...");
							sortProperty = tokens.Current;
							descending = false;
							break;
						}
						else if (tokens.Current.Value == "orderbydescending")
						{
							if (!tokens.MoveNext()) throw new InvalidUserQueryException($"Incomplete: include property to sort by: {queryText} ...");
							sortProperty = tokens.Current;
							descending = true;
							break;
						}
						else if (!queryableProperties.ContainsKey(tokens.Current.Value))
						{
							// CONSIDER THIS ENTIRE QUERY A LITERAL (except anything following orderby)
							// this allows simple searches on the default
							ArgumentNullException.ThrowIfNull(defaultTarget);

							StringBuilder bigToken = new();
							do
							{
								bigToken.Append(tokens.Current.Value);
							} while (tokens.MoveNext() && tokens.Current.Value != "orderby" && tokens.Current.Value != "orderbydescending");

							var term = EvaluateComparison(defaultTarget, new Token(bigToken.ToString(), true), null);
							var lambda = Expression.Lambda<Func<TModel, bool>>(term, model);
							var exp = root.Where(lambda);

							if (tokens.Current.Value == "orderby")
							{
								if (!tokens.MoveNext()) throw new InvalidUserQueryException($"Incomplete: include property to sort by: {queryText} ...");
								sortProperty = tokens.Current;
								var orderLambda = CreateOrderByExpression(sortProperty);
								return exp.OrderBy(orderLambda);
							}
							else if (tokens.Current.Value == "orderbydescending")
							{
								if (!tokens.MoveNext()) throw new InvalidUserQueryException($"Incomplete: include property to sort by: {queryText} ...");
								sortProperty = tokens.Current;
								var orderLambda = CreateOrderByExpression(sortProperty);
								return exp.OrderByDescending(orderLambda);
							}

							return exp;
						}
					}
					goto case Mode.ready;
				case Mode.ready:
					Expression newTerm;
					if (tokens.Current.IsLiteral)
					{
						ArgumentNullException.ThrowIfNull(defaultTarget);
						newTerm = EvaluateComparison(defaultTarget, tokens.Current, null);
					}
					else
					{
						Token target = tokens.Current;
						if (!tokens.MoveNext()) throw new InvalidUserQueryException($"Incomplete: include operator: {queryText} ...");
						Token op = tokens.Current;
						if (!tokens.MoveNext()) throw new InvalidUserQueryException($"Incomplete: include property or literal to compare to: {queryText} ...");
						newTerm = EvaluateComparison(target, tokens.Current, op);
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
						mode = Mode.ready;
					}
					else if (tokens.Current.Value == "&")
					{
						mode = Mode.ready;
					}
					else if (tokens.Current.Value == "orderby")
					{
						if (!tokens.MoveNext()) throw new InvalidUserQueryException($"Incomplete: include property to sort by: {queryText} ...");
						sortProperty = tokens.Current;
						descending = false;
						break;
					}
					else if (tokens.Current.Value == "orderbydescending")
					{
						if (!tokens.MoveNext()) throw new InvalidUserQueryException($"Incomplete: include property to sort by: {queryText} ...");
						sortProperty = tokens.Current;
						descending = true;
						break;
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
		IQueryable<TModel> returnValue;
		if (terms.TryPop(out currentTerm))
		{
			while (terms.TryPop(out var ex))
			{
				currentTerm = Expression.OrElse(currentTerm, ex);
			}
			var lambda = Expression.Lambda<Func<TModel, bool>>(currentTerm, model);
			returnValue = root.Where(lambda);
		}
		else
		{
			returnValue = root;
		}

		if (sortProperty != null)
		{
			var lambda = CreateOrderByExpression(sortProperty);
			return descending ? returnValue.OrderByDescending(lambda) : returnValue.OrderBy(lambda);
		}
		else
		{
			return returnValue;
		}
	}

	private PropertyInfo GetProperty(Token token)
	{
		if (token.Value == "orderby" || token.Value == "orderbydescending") throw new InvalidUserQueryException("Incomplete: include property to sort by");
		bool exists = queryableProperties.TryGetValue(token.Value.ToLowerInvariant(), out var property);
		if (!exists) throw new InvalidUserQueryException($"Property \"{token.Value}\" not in type {typeof(TModel).FullName}\n" +
			$"Use {nameof(UserQueryableAttribute)} to specify properties as queryable.");
		return property!;
	}

	private Expression EvaluateComparison(Token target, Token compared, Token? op)
	{
		op ??= new Token("*", true);

		PropertyInfo targetProp = GetProperty(target);
		Expression left = Expression.Property(model, targetProp);

		Expression right;
		if (compared.IsLiteral)
		{
			object? parsed = UserQueryTypeParsers.Parse(targetProp.PropertyType, compared.Value);
			right = Expression.Constant(parsed, targetProp.PropertyType);
		}
		else
		{
			PropertyInfo comparedProp = GetProperty(compared);
			right = Expression.Property(model, comparedProp);
		}

		// Normalize for string-insensitive comparison where applicable
		bool supportsString = UserQueryOperatorCapabilities.SupportsStringMatch(targetProp.PropertyType);
		bool supportsComparision = UserQueryOperatorCapabilities.SupportsComparison(targetProp.PropertyType);

		Expression body = op.Value switch
		{
			"=" => Expression.Equal(left, right),
			"!=" => Expression.NotEqual(left, right),

			"<" when supportsComparision => Expression.LessThan(left, right),
			"<=" when supportsComparision => Expression.LessThanOrEqual(left, right),
			">" when supportsComparision => Expression.GreaterThan(left, right),
			">=" when supportsComparision => Expression.GreaterThanOrEqual(left, right),

			"^" when supportsString => CallInsensitive(left, right, nameof(string.StartsWith)),
			"!^" when supportsString => Expression.Not(CallInsensitive(left, right, nameof(string.StartsWith))),
			"$" when supportsString => CallInsensitive(left, right, nameof(string.EndsWith)),
			"!$" when supportsString => Expression.Not(CallInsensitive(left, right, nameof(string.EndsWith))),
			"*" when supportsString => CallInsensitive(left, right, nameof(string.Contains)),
			"!*" when supportsString => Expression.Not(CallInsensitive(left, right, nameof(string.Contains))),

			_ => throw new InvalidUserQueryException($"Unsupported or type-mismatched operator \"{op.Value}\" for property \"{target.Value}\"")
		};

		return body;
	}

	private Expression<Func<TModel, object>> CreateOrderByExpression(Token sortProperty)
	{
		var property = Expression.Property(model, GetProperty(sortProperty));

		Expression conversion = property.Type.IsValueType
			? Expression.Convert(property, typeof(object))
			: (Expression)property;

		return Expression.Lambda<Func<TModel, object>>(conversion, model);
	}

	private static MethodCallExpression CallInsensitive(Expression left, Expression right, string methodName)
	{
		// Convert both expressions to lowercase: left.ToLower().method(right.ToLower())
		var toLowerMethod = typeof(string).GetMethod(nameof(string.ToLower), Type.EmptyTypes)!;

		var leftToLower = Expression.Call(left, toLowerMethod);
		var rightToLower = Expression.Call(right, toLowerMethod);

		var method = typeof(string).GetMethod(methodName, [typeof(string)])!;
		return Expression.Call(leftToLower, method, rightToLower);
	}

	enum Mode
	{
		ready = 0,
		finish = 1,
		inital = 2,
	}
}
