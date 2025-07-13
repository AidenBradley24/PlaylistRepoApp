using LocalPlaylistMasterLib;
using System.Collections.Concurrent;

namespace LocalPlaylistMasterAPI
{
	public interface ITaskService
	{
		/// <summary>
		/// Starts a new task with tracked progress.
		/// </summary>
		Guid StartTask(Func<IProgress<TaskProgress>, Task> taskFunc);

		/// <summary>
		/// Gets the progress of a currently running task
		/// </summary>
		TaskProgress? GetProgress(Guid taskId);
	}

	public class TaskService : ITaskService
	{
		private readonly ConcurrentDictionary<Guid, TaskProgress> ongoingTasks = [];

		public Guid StartTask(Func<IProgress<TaskProgress>, Task> taskFunc)
		{
			var id = Guid.NewGuid();
			Progress<TaskProgress> progress = new();
			progress.ProgressChanged += (o, p) => ongoingTasks[id] = p;
			((IProgress<TaskProgress>)progress).Report(new TaskProgress { Progress = -1, Status = "Running" });
			_ = Task.Run(() => taskFunc.Invoke(progress));
			return id;
		}

		public TaskProgress? GetProgress(Guid taskId)
		{
			return ongoingTasks.TryGetValue(taskId, out var progress) ? progress : null;
		}
	}
}
