using PlaylistRepoLib;
using PlaylistRepoLib.Models;
using Xunit.Abstractions;

namespace Tests
{
	public class UserQueryTests
	{
		private static readonly IEnumerable<Media> root =
		[
			new Media() { Title = "Item1", Rating = 1},
			new Media() { Title = "Item2", Rating = 5},
			new Media() { Title = "Item3", Rating = 10},
		];

		private readonly ITestOutputHelper output;
		public UserQueryTests(ITestOutputHelper output)
		{
			this.output = output;
		}

		private static UserQueryProvider<Media> GetProvider()
		{
			return new UserQueryProvider<Media>(root.AsQueryable());
		}

		private void Fail(string TEST)
		{
			var provider = GetProvider();
			var ex = Assert.Throws<InvalidUserQueryException>(() =>
			{
				var result = provider.EvaluateUserQuery(TEST);
				output.WriteLine(result.Count().ToString());
			});
			output.WriteLine(ex.Message);
		}

		[Fact]
		public void Test1()
		{
			const string TEST = "\"item\"";
			var result = GetProvider().EvaluateUserQuery(TEST);
			Assert.True(result.Any(i => i.Title == "Item1"));
			Assert.True(result.Any(i => i.Title == "Item2"));
			Assert.True(result.Any(i => i.Title == "Item3"));
		}

		[Fact]
		public void Test2()
		{
			const string TEST = "\"b\"";
			var result = GetProvider().EvaluateUserQuery(TEST);
			Assert.Empty(result);
		}

		[Fact]
		public void Test3()
		{
			const string TEST = "rating = 1";
			var result = GetProvider().EvaluateUserQuery(TEST);
			Assert.True(result.Any(i => i.Title == "Item1"));
			Assert.False(result.Any(i => i.Title == "Item2"));
			Assert.False(result.Any(i => i.Title == "Item3"));
		}

		[Fact]
		public void Test4()
		{
			const string TEST = "rating > 1";
			var result = GetProvider().EvaluateUserQuery(TEST);
			Assert.False(result.Any(i => i.Title == "Item1"));
			Assert.True(result.Any(i => i.Title == "Item2"));
			Assert.True(result.Any(i => i.Title == "Item3"));
		}

		[Fact]
		public void Test5()
		{
			const string TEST = "rating <= 5";
			var result = GetProvider().EvaluateUserQuery(TEST);
			Assert.True(result.Any(i => i.Title == "Item1"));
			Assert.True(result.Any(i => i.Title == "Item2"));
			Assert.False(result.Any(i => i.Title == "Item3"));
		}

		[Fact]
		public void Test6()
		{
			const string TEST = "rating = 1, rating = 5";
			var result = GetProvider().EvaluateUserQuery(TEST);
			Assert.True(result.Any(i => i.Title == "Item1"));
			Assert.True(result.Any(i => i.Title == "Item2"));
			Assert.False(result.Any(i => i.Title == "Item3"));
		}

		[Fact]
		public void Test7()
		{
			const string TEST = "rating = 1, rating = 5 & title * 'beans'";
			var result = GetProvider().EvaluateUserQuery(TEST);
			Assert.True(result.Any(i => i.Title == "Item1"));
			Assert.False(result.Any(i => i.Title == "Item2"));
			Assert.False(result.Any(i => i.Title == "Item3"));
		}
	}
}
