using PlaylistRepoLib.UserQueries;
using Xunit.Abstractions;

namespace Tests
{
	public class UserQueryTests
	{
		private static readonly IEnumerable<TestModel> root =
		[
			new TestModel() { Name = "Item1", IntValue = 1, FloatValue = 0f, DoubleValue = 0.0, TimeSpanValue = TimeSpan.FromSeconds(0) },
			new TestModel() { Name = "Item2", IntValue = 5, FloatValue = 5f, DoubleValue = 5.0, TimeSpanValue = TimeSpan.FromSeconds(30) },
			new TestModel() { Name = "Item3", IntValue = 10, FloatValue = 10f, DoubleValue = 10.0, TimeSpanValue = TimeSpan.FromSeconds(120) },
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
		[InlineData("intvalue = 1", new[] { "Item1" })]
		[InlineData("intvalue != 5", new[] { "Item1", "Item3" })]
		[InlineData("intvalue < 5", new[] { "Item1" })]
		[InlineData("intvalue <= 5", new[] { "Item1", "Item2" })]
		[InlineData("intvalue > 1", new[] { "Item2", "Item3" })]
		[InlineData("intvalue >= 10", new[] { "Item3" })]
		[InlineData("floatvalue = 0.0", new[] { "Item1" })]
		[InlineData("floatvalue > 0.0", new[] { "Item2", "Item3" })]
		[InlineData("floatvalue < 10.0", new[] { "Item1", "Item2" })]
		[InlineData("doublevalue = 5.0", new[] { "Item2" })]
		[InlineData("doublevalue >= 10.0", new[] { "Item3" })]
		[InlineData("doublevalue <= 0.0", new[] { "Item1" })]
		[InlineData("timespanvalue = '00:00:30'", new[] { "Item2" })]
		[InlineData("timespanvalue > '00:00:00'", new[] { "Item2", "Item3" })]
		[InlineData("timespanvalue < '00:02:00'", new[] { "Item1", "Item2" })]
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
		[InlineData("intvalue = 1 & name = 'Item1'", new[] { "Item1" })]
		[InlineData("intvalue > 1 & intvalue < 10", new[] { "Item2" })]
		[InlineData("name * 'Item' & intvalue >= 5", new[] { "Item2", "Item3" })]
		[InlineData("floatvalue = 5.0 & doublevalue = 5.0", new[] { "Item2" })]
		[InlineData("timespanvalue > '00:00:00' & timespanvalue < '00:02:00'", new[] { "Item2" })]
		[InlineData("intvalue = 1 & intvalue = 5", new string[] { })]
		public void AndOperator_Tests(string query, string[] expectedNames)
		{
			var result = GetProvider().EvaluateUserQuery(query);
			var actualNames = result.Select(i => i.Name).ToArray();
			Assert.Equal(expectedNames.OrderBy(n => n), actualNames.OrderBy(n => n));
		}

		[Theory]
		[InlineData("intvalue = 1, intvalue = 5", new[] { "Item1", "Item2" })]
		[InlineData("name = 'Item2', name = 'Item3'", new[] { "Item2", "Item3" })]
		[InlineData("intvalue = 1, intvalue = 5 & name = 'Item2'", new[] { "Item1", "Item2" })]
		[InlineData("intvalue = 1, intvalue = 5 & name = 'Item3'", new[] { "Item1" })]
		[InlineData("floatvalue = 0.0, floatvalue = 10.0", new[] { "Item1", "Item3" })]
		[InlineData("doublevalue = 5.0, doublevalue = 10.0", new[] { "Item2", "Item3" })]
		[InlineData("timespanvalue = '00:00:00', timespanvalue = '00:02:00'", new[] { "Item1", "Item3" })]
		public void OrOperator_Tests(string query, string[] expectedNames)
		{
			var result = GetProvider().EvaluateUserQuery(query);
			var actualNames = result.Select(i => i.Name).ToArray();
			Assert.Equal(expectedNames.OrderBy(n => n), actualNames.OrderBy(n => n));
		}

		public static IEnumerable<object[]> InvalidQueries =>
		[
			["intvalue =="],
			["name ^"],
			["name = "],
			["intvalue ^ 10"],
			["name > 'z'"],
			["floatvalue ^ 1.0"],
			["doublevalue * 2.0"],
			["timespanvalue = 'notatimespan'"]
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
