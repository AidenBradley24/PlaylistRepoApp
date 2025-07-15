namespace PlaylistRepoLib
{
	public class TaskProgress
	{
		public int Progress { get; init; }
		public string? Status { get; init; }

		public const int INDETERMINATE_PROGRESS = -1;

		public bool IsCompleted => Progress == 100;

		public static TaskProgress FromCompleted(string message = "Completed")
		{
			return new TaskProgress()
			{
				Progress = 100,
				Status = message
			};
		}

		public static TaskProgress FromException(Exception ex)
		{
			return new TaskProgress()
			{
				Progress = INDETERMINATE_PROGRESS,
				Status = ex.Message,
			};
		}

		public static TaskProgress FromNumbers(int completed, int total, string message = "Running")
		{
			int progress = 100 * completed / total;
			if (progress == 100) progress = 99;
			return new TaskProgress()
			{
				Progress = progress,
				Status = message
			};
		}

		public static TaskProgress FromIndeterminate(string message = "Running")
		{
			return new TaskProgress()
			{
				Progress = INDETERMINATE_PROGRESS,
				Status = message
			};
		}
	}
}
