using PlaylistRepoLib.UserQueries;

namespace Tests
{
	[PrimaryUserQueryable(nameof(Name))]
	internal class TestModel
	{
		public int Id { get; set; }

		[UserQueryable("name")]
		public string Name { get; set; } = "";

		[UserQueryable("ivalue")]
		public int IntValue { get; set; } = 0;
	}
}
