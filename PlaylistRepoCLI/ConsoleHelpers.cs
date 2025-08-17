using PlaylistRepoLib;

namespace PlaylistRepoCLI
{
	public static class ConsoleHelpers
	{
		static int linesWritten = 0;

		public static void WriteProgress(TaskProgress? progress)
		{
			linesWritten = progress?.Status?.Count(c => c == '\n') ?? 0;
			Console.WriteLine(progress?.Status);
			linesWritten++;
			if (progress?.Progress > 0)
			{
				Console.WriteLine("{0,-50}", new string('░', progress?.Progress / 2 ?? 0) + new string('▓', 50 - (progress?.Progress / 2 ?? 0)));
				linesWritten++;
			}
			else
			{
				Console.WriteLine();
				linesWritten++;
			}
		}

		public static void RewriteProgress(TaskProgress? progress)
		{
			ClearProgress();
			WriteProgress(progress);
		}

		public static void ClearProgress()
		{
			int top = Console.CursorTop - linesWritten;
			Console.SetCursorPosition(0, top);
			for (int i = 0; i < linesWritten; i++) Console.WriteLine(new string(' ', Console.WindowWidth));
			Console.SetCursorPosition(0, top);
			linesWritten = 0;
		}
	}
}
