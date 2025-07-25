using PlaylistRepoLib.UserQueries;
using Xunit.Abstractions;

namespace Tests
{
	public class UserQueryTests
	{
		private static readonly IEnumerable<TestModel> root =
		[
			new TestModel() { Name = "Item1", IntValue = 1 },
			new TestModel() { Name = "Item2", IntValue = 5 },
			new TestModel() { Name = "Item3", IntValue = 10 },
		];

		private readonly ITestOutputHelper output;

		public UserQueryTests(ITestOutputHelper output)
		{
			this.output = output;
		}

		private static UserQueryProvider<TestModel> GetProvider()
		{
			return new UserQueryProvider<TestModel>(root.AsQueryable());
		}

		[Theory]
		[InlineData("ivalue = 1", new[] { "Item1" })]
		[InlineData("ivalue != 5", new[] { "Item1", "Item3" })]
		[InlineData("ivalue < 5", new[] { "Item1" })]
		[InlineData("ivalue <= 5", new[] { "Item1", "Item2" })]
		[InlineData("ivalue > 1", new[] { "Item2", "Item3" })]
		[InlineData("ivalue >= 10", new[] { "Item3" })]
		[InlineData("name = 'Item1'", new[] { "Item1" })]
		[InlineData("name != 'Item1'", new[] { "Item2", "Item3" })]
		[InlineData("name ^ 'Item'", new[] { "Item1", "Item2", "Item3" })]
		[InlineData("name !^ 'Item3'", new[] { "Item1", "Item2" })]
		[InlineData("name * 'em2'", new[] { "Item2" })]
		[InlineData("name !* '3'", new[] { "Item1", "Item2" })]
		public void Operator_Tests(string query, string[] expectedNames)
		{
			var provider = GetProvider();
			var result = provider.EvaluateUserQuery(query);
			var actualNames = result.Select(i => i.Name).ToArray();
			Assert.Equal(expectedNames.OrderBy(n => n), actualNames.OrderBy(n => n));
		}

		[Theory]
		[InlineData("ivalue = 1 & name = 'Item1'", new[] { "Item1" })]
		[InlineData("ivalue > 1 & ivalue < 10", new[] { "Item2" })]
		[InlineData("name * 'Item' & ivalue >= 5", new[] { "Item2", "Item3" })]
		[InlineData("ivalue = 1 & ivalue = 5", new string[] { })] // no match
		public void AndOperator_Tests(string query, string[] expectedNames)
		{
			var result = GetProvider().EvaluateUserQuery(query);
			var actualNames = result.Select(i => i.Name).ToArray();
			Assert.Equal(expectedNames.OrderBy(n => n), actualNames.OrderBy(n => n));
		}

		[Theory]
		[InlineData("ivalue = 1, ivalue = 5", new[] { "Item1", "Item2" })]
		[InlineData("name = 'Item2', name = 'Item3'", new[] { "Item2", "Item3" })]
		[InlineData("ivalue = 1, ivalue = 5 & name = 'Item2'", new[] { "Item1", "Item2" })]
		[InlineData("ivalue = 1, ivalue = 5 & name = 'Item3'", new[] { "Item1" })] // Item2 fails inner AND
		public void OrOperator_Tests(string query, string[] expectedNames)
		{
			var result = GetProvider().EvaluateUserQuery(query);
			var actualNames = result.Select(i => i.Name).ToArray();
			Assert.Equal(expectedNames.OrderBy(n => n), actualNames.OrderBy(n => n));
		}

		public static IEnumerable<object[]> InvalidQueries =>
		[
			["ivalue =="],
			["name ^"],
			["name = "],
			["ivalue ^ 10"], // invalid operator on int
			["name > 'z'"],  // invalid operator on string (if not allowed)
		];

		[Theory]
		[MemberData(nameof(InvalidQueries))]
		public void Invalid_Queries_Throw(string query)
		{
			var provider = GetProvider();
			var ex = Assert.Throws<InvalidUserQueryException>(() => provider.EvaluateUserQuery(query).ToList());
			output.WriteLine(ex.Message);
		}
	}
}
