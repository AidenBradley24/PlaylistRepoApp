using PlaylistRepoLib;
using PlaylistRepoLib.Models;
using Xunit.Abstractions;

namespace Tests
{
	public class UserQueryTests
	{
		private static readonly IEnumerable<Media> root =
		[
			new Media() { Title = "Item1" },
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
		}
	}
}
