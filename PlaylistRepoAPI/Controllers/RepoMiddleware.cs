using PlaylistRepoLib;

namespace PlaylistRepoAPI.Controllers
{
	public class RepoMiddleware(RequestDelegate next, IPlayRepoService repoService)
	{
		public async Task InvokeAsync(HttpContext context)
		{
			var path = context.Request.Path.Value ?? string.Empty;

			// Allow requests under "service" and non api requests
			if (!path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase) || path.StartsWith("/api/service/", StringComparison.OrdinalIgnoreCase))
			{
				await next(context);
				return;
			}

			if (!repoService.IsRepoInitialized)
			{
				if (FileSpec.IsInsideProject(repoService.RootPath))
				{
					context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
					await context.Response.WriteAsync("Service unavailable: repo must be outside of app directory");
					return;
				}

				context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
				await context.Response.WriteAsync("Service unavailable: repo is not initialized.");
				return;
			}

			// Otherwise continue
			await next(context);
		}
	}
}
