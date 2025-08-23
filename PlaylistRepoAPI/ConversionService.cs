using PlaylistRepoLib;
using System.Diagnostics;

namespace PlaylistRepoAPI
{
	public interface IConversionService
	{
		public Task Convert(IEnumerable<FileInfo> convertees, IEnumerable<FileInfo> targets, IProgress<TaskProgress> progress);
	}

	public class ConversionService : IConversionService
	{
		private readonly int MAX_PROCESS_COUNT;
		private readonly Queue<string> argumentQueue = [];

		public ConversionService()
		{
			MAX_PROCESS_COUNT = Math.Max(1, Environment.ProcessorCount / 2);
		}

		public async Task Convert(IEnumerable<FileInfo> convertees, IEnumerable<FileInfo> targets, IProgress<TaskProgress> progress)
		{
			foreach (var (file, target) in convertees.Zip(targets))
			{
				argumentQueue.Enqueue($"-i \"{file.FullName}\" \"{target.FullName}\" -y");
			}

			List<Task> tasks = new(MAX_PROCESS_COUNT);
			int totalTaskCount = argumentQueue.Count;
			progress.Report(TaskProgress.FromIndeterminate("Converting..."));

			while (argumentQueue.Count > 0 || tasks.Count > 0)
			{
				while (tasks.Count < MAX_PROCESS_COUNT && argumentQueue.Count > 0)
				{
					string arg = argumentQueue.Dequeue();
					Task task = Task.Run(async () =>
					{
						var ffmpeg = new Process();
						ffmpeg.StartInfo.FileName = "ffmpeg";
						ffmpeg.StartInfo.Arguments = arg;
						ffmpeg.StartInfo.CreateNoWindow = true;
						ffmpeg.Start();
						await ffmpeg.WaitForExitAsync();
					});

					tasks.Add(task);
				}

				Task completedTask = await Task.WhenAny(tasks);
				tasks.Remove(completedTask);

				int completedTaskCount = totalTaskCount - argumentQueue.Count - tasks.Count;
				progress.Report(TaskProgress.FromNumbers(completedTaskCount, totalTaskCount, "Converting..."));
			}

			progress.Report(TaskProgress.FromCompleted("Conversion completed."));
		}
	}
}
