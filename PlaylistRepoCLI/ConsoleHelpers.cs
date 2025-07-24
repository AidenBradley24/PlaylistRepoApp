using PlaylistRepoLib;

namespace PlaylistRepoCLI
{
	public static class ConsoleHelpers
	{
		public static void WriteProgress(TaskProgress? progress)
		{
			Console.WriteLine("{0,-50}", progress?.Status);
			if (progress?.Progress > 0)
			{
				Console.WriteLine("{0,-50}", new string('░', progress?.Progress / 2 ?? 0) + new string('▓', 50 - (progress?.Progress / 2 ?? 0)));
			}
			else
			{
				Console.WriteLine();
			}
		}

		public static void RewriteProgress(TaskProgress? progress)
		{
			ClearProgress();
			WriteProgress(progress);
		}

		public static void ClearProgress()
		{
			Console.SetCursorPosition(0, Console.CursorTop - 4);
			for (int i = 0; i < 4; i++) Console.WriteLine(new string(' ', Console.WindowWidth));
			Console.SetCursorPosition(0, Console.CursorTop - 4);
			Console.WriteLine(new string(' ', Console.WindowWidth));
			Console.WriteLine(new string(' ', Console.WindowWidth));
		}
	}
}
