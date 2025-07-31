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
		/// Starts a new task with tracked progress.
		/// </summary>
		Guid StartTask(Func<IProgress<TaskProgress>, Task> taskFunc);

		/// <summary>
		/// Starts a new task with tracked progress. Includes an array of services to inject.
		/// </summary>
		Guid StartTask(Func<IProgress<TaskProgress>, object[], Task> taskFunc, Type[] serviceTypes);

		/// <summary>
		/// Starts a new task with tracked progress. Includes <typeparamref name="S0"/> service to inject.
		/// </summary>
		Guid StartTask<S0>(Func<IProgress<TaskProgress>, S0, Task> taskFunc);

		/// <summary>
		/// Starts a new task with tracked progress. Includes <typeparamref name="S0"/> and <typeparamref name="S1"/> services to inject.
		/// </summary>
		Guid StartTask<S0, S1>(Func<IProgress<TaskProgress>, S0, S1, Task> taskFunc);

		/// <summary>
		/// Starts a new task with tracked progress. Includes <typeparamref name="S0"/>, <typeparamref name="S1"/>, and <typeparamref name="S2"/> services to inject.
		/// </summary>
		Guid StartTask<S0, S1, S2>(Func<IProgress<TaskProgress>, S0, S1, S2, Task> taskFunc);
	}

	public class TaskService(ILogger<TaskService> logger, IServiceProvider serviceProvider) : ITaskService
	{
		private readonly ConcurrentDictionary<Guid, TaskProgress> ongoingTasks = [];

		public TaskProgress? GetProgress(Guid taskId)
		{
			return ongoingTasks.TryGetValue(taskId, out var progress) ? progress : null;
		}

		public Guid StartTask(Func<IProgress<TaskProgress>, Task> taskFunc)
		{
			return StartTask((progress, _) => taskFunc.Invoke(progress), []);
		}

		public Guid StartTask(Func<IProgress<TaskProgress>, object[], Task> taskFunc, Type[] serviceTypes)
		{
			var id = Guid.NewGuid();
			Progress<TaskProgress> progress = new();
			progress.ProgressChanged += (o, p) => ongoingTasks[id] = p;
			((IProgress<TaskProgress>)progress).Report(new TaskProgress { Progress = -1, Status = "Running" });
			var task = Task.Run(async () =>
			{
				try
				{
					using var scope = serviceProvider.CreateScope();
					object[] services = [.. serviceTypes.Select(t => scope.ServiceProvider.GetRequiredService(t)
						?? throw new NotImplementedException("Service not implemented: " + t.Name))];
					await taskFunc.Invoke(progress, services);
				}
				catch (Exception ex)
				{
					((IProgress<TaskProgress>)progress).Report(TaskProgress.FromException(ex));
					logger.LogError("An error has occurred when processing the request {request} {ex}", id, ex);
				}
			});
			return id;
		}

		public Guid StartTask<S0>(Func<IProgress<TaskProgress>, S0, Task> taskFunc)
		{
			return StartTask((progress, services) => taskFunc.Invoke(progress, (S0)services[0]), [typeof(S0)]);
		}

		public Guid StartTask<S0, S1>(Func<IProgress<TaskProgress>, S0, S1, Task> taskFunc)
		{
			return StartTask((progress, services) => taskFunc.Invoke(progress, (S0)services[0], (S1)services[1]), [typeof(S0), typeof(S1)]);
		}

		public Guid StartTask<S0, S1, S2>(Func<IProgress<TaskProgress>, S0, S1, S2, Task> taskFunc)
		{
			return StartTask((progress, services) => taskFunc.Invoke(progress, (S0)services[0], (S1)services[1], (S2)services[2]), [typeof(S0), typeof(S1), typeof(S2)]);
		}
	}
}
