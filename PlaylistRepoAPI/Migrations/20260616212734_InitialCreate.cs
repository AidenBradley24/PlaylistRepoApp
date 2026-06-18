using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlaylistRepoAPI.Migrations
{
	/// <inheritdoc />
	public partial class InitialCreate : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.CreateTable(
				name: "Playlists",
				columns: table => new
				{
					Id = table.Column<int>(type: "INTEGER", nullable: false)
						.Annotation("Sqlite:Autoincrement", true),
					Title = table.Column<string>(type: "TEXT", nullable: false),
					Description = table.Column<string>(type: "TEXT", nullable: false),
					UserQuery = table.Column<string>(type: "TEXT", nullable: false),
					WhiteList = table.Column<string>(type: "TEXT", nullable: false),
					BlackList = table.Column<string>(type: "TEXT", nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_Playlists", x => x.Id);
				});

			migrationBuilder.CreateTable(
				name: "RemotePlaylists",
				columns: table => new
				{
					Id = table.Column<int>(type: "INTEGER", nullable: false)
						.Annotation("Sqlite:Autoincrement", true),
					Name = table.Column<string>(type: "TEXT", nullable: false),
					Description = table.Column<string>(type: "TEXT", nullable: false),
					Link = table.Column<string>(type: "TEXT", nullable: false),
					MediaMime = table.Column<string>(type: "TEXT", nullable: false),
					Type = table.Column<int>(type: "INTEGER", nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_RemotePlaylists", x => x.Id);
				});

			migrationBuilder.CreateTable(
				name: "Medias",
				columns: table => new
				{
					Id = table.Column<int>(type: "INTEGER", nullable: false)
						.Annotation("Sqlite:Autoincrement", true),
					RemoteId = table.Column<int>(type: "INTEGER", nullable: true),
					RemoteUID = table.Column<string>(type: "TEXT", nullable: true),
					FilePath = table.Column<string>(type: "TEXT", nullable: true),
					Hash = table.Column<byte[]>(type: "BLOB", nullable: true),
					MimeType = table.Column<string>(type: "TEXT", nullable: false),
					Title = table.Column<string>(type: "TEXT", nullable: false),
					PrimaryArtist = table.Column<string>(type: "TEXT", nullable: false),
					Artists = table.Column<string>(type: "TEXT", nullable: true),
					Genre = table.Column<string>(type: "TEXT", nullable: false),
					Album = table.Column<string>(type: "TEXT", nullable: false),
					Description = table.Column<string>(type: "TEXT", nullable: false),
					Rating = table.Column<int>(type: "INTEGER", nullable: false),
					LengthMilliseconds = table.Column<long>(type: "INTEGER", nullable: false),
					Order = table.Column<int>(type: "INTEGER", nullable: false),
					Locked = table.Column<bool>(type: "INTEGER", nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_Medias", x => x.Id);
					table.ForeignKey(
						name: "FK_Medias_RemotePlaylists_RemoteId",
						column: x => x.RemoteId,
						principalTable: "RemotePlaylists",
						principalColumn: "Id");
				});

			migrationBuilder.CreateTable(
				name: "MediaPlayStats",
				columns: table => new
				{
					MediaId = table.Column<int>(type: "INTEGER", nullable: false),
					PlayCount = table.Column<int>(type: "INTEGER", nullable: false),
					LastPlayed = table.Column<DateTime>(type: "TEXT", nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_MediaPlayStats", x => x.MediaId);
					table.ForeignKey(
						name: "FK_MediaPlayStats_Medias_MediaId",
						column: x => x.MediaId,
						principalTable: "Medias",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.CreateIndex(
				name: "IX_Medias_RemoteId",
				table: "Medias",
				column: "RemoteId");
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropTable(
				name: "MediaPlayStats");

			migrationBuilder.DropTable(
				name: "Playlists");

			migrationBuilder.DropTable(
				name: "Medias");

			migrationBuilder.DropTable(
				name: "RemotePlaylists");
		}
	}
}
