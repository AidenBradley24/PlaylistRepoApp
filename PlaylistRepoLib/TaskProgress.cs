namespace PlaylistRepoLib
{
	public class TaskProgress
	{
		public int Progress { get; init; }
		public string? Status { get; init; }

		public const int INDETERMINATE_PROGRESS = -1;
		public const int ERROR = -2;
		public const int COMPLETE = 100;

		public bool IsCompleted => IsSuccess || IsError;
		public bool IsSuccess => Progress == COMPLETE;
		public bool IsError => Progress == ERROR;

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
				Progress = ERROR,
				Status = ex.Message,
			};
		}

		public static TaskProgress FromNumbers(int completed, int total, string message = "Running")
		{
			int progress = 100 * completed / total;
			if (progress == COMPLETE) progress = COMPLETE - 1;
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

		public static TaskProgress FromComposite(int remainingTasks, params IEnumerable<TaskProgress> ongoingOrCompletedTasks)
		{
			TaskProgress? lastTask = null;
			int totalTasks = remainingTasks;
			int totalProgess = 0;
			foreach (var task in ongoingOrCompletedTasks)
			{
				totalTasks++;
				lastTask = task;
				totalProgess += task.Progress;
			}
			return FromNumbers(totalProgess, totalTasks * 100, lastTask?.Status ?? "Running");
		}
	}
}
