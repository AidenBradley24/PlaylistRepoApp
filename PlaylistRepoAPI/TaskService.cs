using PlaylistRepoLib;
using System.Collections.Concurrent;

namespace PlaylistRepoAPI
{
	public interface ITaskService
	{
		/// <summary>
		/// Starts a new task with tracked progress.
		/// </summary>
		Guid StartTask(Func<IProgress<TaskProgress>, Task> taskFunc);

		/// <summary>
		/// Starts a new task with tracked progress. Includes a <see cref="PlayRepoDbContext"/> for dependency injection.
		/// </summary>
		Guid StartTaskWithDb(Func<IProgress<TaskProgress>, PlayRepoDbContext, Task> taskFunc);

		/// <summary>
		/// Gets the progress of a currently running task
		/// </summary>
		TaskProgress? GetProgress(Guid taskId);
	}

	public class TaskService(ILogger<TaskService> logger, IPlayRepoService playRepo) : ITaskService
	{
		private readonly ConcurrentDictionary<Guid, TaskProgress> ongoingTasks = [];

		public Guid StartTask(Func<IProgress<TaskProgress>, Task> taskFunc)
		{
			var id = Guid.NewGuid();
			Progress<TaskProgress> progress = new();
			progress.ProgressChanged += (o, p) => ongoingTasks[id] = p;
			((IProgress<TaskProgress>)progress).Report(new TaskProgress { Progress = -1, Status = "Running" });
			var task = Task.Run(async () =>
			{
				try
				{
					await taskFunc.Invoke(progress);
				}
				catch (Exception ex)
				{
					((IProgress<TaskProgress>)progress).Report(TaskProgress.FromException(ex));
					logger.LogError("An error has occurred when processing the request {request} {ex}", id, ex);
				}
			});
			return id;
		}

		public Guid StartTaskWithDb(Func<IProgress<TaskProgress>, PlayRepoDbContext, Task> taskFunc)
		{
			var id = Guid.NewGuid();
			Progress<TaskProgress> progress = new();
			progress.ProgressChanged += (o, p) => ongoingTasks[id] = p;
			((IProgress<TaskProgress>)progress).Report(new TaskProgress { Progress = -1, Status = "Running" });
			var task = Task.Run(async () =>
			{
				try
				{
					await taskFunc.Invoke(progress, new PlayRepoDbContext(playRepo));
				}
				catch (Exception ex)
				{
					((IProgress<TaskProgress>)progress).Report(TaskProgress.FromException(ex));
					logger.LogError("An error has occurred when processing the request {request} {ex}", id, ex);
				}
			});
			return id;
		}

		public TaskProgress? GetProgress(Guid taskId)
		{
			return ongoingTasks.TryGetValue(taskId, out var progress) ? progress : null;
		}
	}
}
