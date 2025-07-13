namespace LocalPlaylistMasterLib
{
	public class TaskProgress
	{
		public int Progress { get; set; }
		public string? Status { get; set; }

		public void Complete()
		{
			Progress = 100;
			Status = "Complete";
		}

		public bool IsCompleted { get => Progress == 100; }

		public static TaskProgress CompletedTask
		{
			get
			{
				var tp = new TaskProgress();
				tp.Complete();
				return tp;
			}
		}
	}
}
