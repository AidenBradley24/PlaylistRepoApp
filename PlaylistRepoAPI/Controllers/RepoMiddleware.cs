namespace PlaylistRepoAPI.Controllers
{
	public class RepoMiddleware(RequestDelegate next, IPlayRepoService repoService)
	{
		public async Task InvokeAsync(HttpContext context)
		{
			var path = context.Request.Path.Value ?? string.Empty;

			// Allow requests under "service"
			if (path.StartsWith("/api/service/", StringComparison.OrdinalIgnoreCase))
			{
				await next(context);
				return;
			}

			if (!repoService.IsRepoInitialized)
			{
				context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
				await context.Response.WriteAsync("Service unavailable: repo is not initialized.");
				return;
			}

			// Otherwise continue
			await next(context);
		}
	}
}
