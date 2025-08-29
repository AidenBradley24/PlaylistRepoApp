using PlaylistRepoLib;
using System.Collections.Concurrent;

namespace PlaylistRepoAPI
{
	public interface ITaskService
	{
		/// <summary>
		/// Gets the progress of a currently running task
		/// </summary>
		TaskProgress? GetProgress(Guid taskId);

		/// <summary>
		/// Starts a new task with tracked progress. Includes the GUID for tracking.
		/// </summary>
		Guid StartTask(Func<IProgress<TaskProgress>, Guid, Task> taskFunc);

		/// <summary>
		/// Starts a new task with tracked progress. Includes the GUID for tracking and an array of services to inject.
		/// </summary>
		Guid StartTask(Func<IProgress<TaskProgress>, Guid, object[], Task> taskFunc, Type[] serviceTypes);

		/// <summary>
		/// Starts a new task with tracked progress.  Includes the GUID for tracking <typeparamref name="S1"/> service to inject.
		/// </summary>
		Guid StartTask<S1>(Func<IProgress<TaskProgress>, Guid, S1, Task> taskFunc);

		/// <summary>
		/// Starts a new task with tracked progress.  Includes the GUID for tracking <typeparamref name="S1"/> and <typeparamref name="S2"/> services to inject.
		/// </summary>
		Guid StartTask<S1, S2>(Func<IProgress<TaskProgress>, Guid, S1, S2, Task> taskFunc);

		/// <summary>
		/// Starts a new task with tracked progress.  Includes the GUID for tracking <typeparamref name="S1"/>, <typeparamref name="S2"/>, and <typeparamref name="S3"/> services to inject.
		/// </summary>
		Guid StartTask<S1, S2, S3>(Func<IProgress<TaskProgress>, Guid, S1, S2, S3, Task> taskFunc);
	}

	public class TaskService(ILogger<TaskService> logger, IServiceProvider serviceProvider) : ITaskService
	{
		private readonly ConcurrentDictionary<Guid, TaskProgress> ongoingTasks = [];

		public TaskProgress? GetProgress(Guid taskId)
		{
			return ongoingTasks.TryGetValue(taskId, out var progress) ? progress : null;
		}

		public Guid StartTask(Func<IProgress<TaskProgress>, Guid, Task> taskFunc)
		{
			return StartTask((progress, guid, _) => taskFunc.Invoke(progress, guid), []);
		}

		public Guid StartTask(Func<IProgress<TaskProgress>, Guid, object[], Task> taskFunc, Type[] serviceTypes)
		{
			var guid = Guid.NewGuid();
			Progress<TaskProgress> progress = new();
			progress.ProgressChanged += (o, p) => ongoingTasks[guid] = p;
			((IProgress<TaskProgress>)progress).Report(new TaskProgress { Progress = -1, Status = "Running" });
			var task = Task.Run(async () =>
			{
				try
				{
					using var scope = serviceProvider.CreateScope();
					object[] services = [.. serviceTypes.Select(t => scope.ServiceProvider.GetRequiredService(t)
						?? throw new NotImplementedException("Service not implemented: " + t.Name))];
					await taskFunc.Invoke(progress, guid, services);
				}
				catch (Exception ex)
				{
					((IProgress<TaskProgress>)progress).Report(TaskProgress.FromException(ex));
					logger.LogError("An error has occurred when processing the request {request} {ex}", guid, ex);
				}
			});
			return guid;
		}

		public Guid StartTask<S1>(Func<IProgress<TaskProgress>, Guid, S1, Task> taskFunc)
		{
			return StartTask((progress, guid, services) => taskFunc.Invoke(progress, guid, (S1)services[0]), [typeof(S1)]);
		}

		public Guid StartTask<S1, S2>(Func<IProgress<TaskProgress>, Guid, S1, S2, Task> taskFunc)
		{
			return StartTask((progress, guid, services) => taskFunc.Invoke(progress, guid, (S1)services[0], (S2)services[1]), [typeof(S1), typeof(S2)]);
		}

		public Guid StartTask<S1, S2, S3>(Func<IProgress<TaskProgress>, Guid, S1, S2, S3, Task> taskFunc)
		{
			return StartTask((progress, guid, services) => taskFunc.Invoke(progress, guid, (S1)services[0], (S2)services[1], (S3)services[2]), [typeof(S1), typeof(S2), typeof(S3)]);
		}
	}
}
