using Microsoft.AspNetCore.Http.Features;
using PlaylistRepoAPI;
using PlaylistRepoAPI.Controllers;

// collect repo
if (args.Length == 0)
{
	Console.Error.WriteLine("Provide a repo directory path.");
	Environment.Exit(1);
}

DirectoryInfo path = new(args[0]);

// build api
var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
	WebRootPath = Path.Combine(AppContext.BaseDirectory, "wwwroot")
});

builder.Configuration.AddCommandLine(args);
string url = builder.Configuration["ApiBaseUrl"] ?? "https://localhost:7002";
builder.WebHost.UseUrls(url);

// Add services to the container.
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddSingleton<ITaskService, TaskService>();
builder.Services.AddSingleton<IPlayRepoService, PlayRepoService>(serviceProvider =>
{
	return new PlayRepoService(path.FullName);
});

builder.Services.AddScoped<IConversionService, ConversionService>();
builder.Services.AddSingleton<IExportService, ExportService>();
builder.Services.AddSingleton<IRemoteService, RemoteService>();
builder.Services.AddScoped<YtDlpService>();
builder.Services.AddScoped<HttpClient>();
builder.Services.AddScoped<InternetRemoteService>();

builder.Services.AddScoped<IMetadataEnricher, MediaHeuristicMetadataEnricher>();
builder.Services.AddScoped<MetadataEnrichmentService>();

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<PlayRepoDbContext>();

#if DEBUG
// Add Swagger services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
#endif

var app = builder.Build();
app.UseHttpsRedirection();

#if DEBUG
// Enable Swagger in development
if (app.Environment.IsDevelopment() || true) // use `|| true` for always-on in local dev
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.MapSwagger();
#endif

app.UseMiddleware<RepoMiddleware>();
app.MapControllers();

app.UseDefaultFiles();
app.UseStaticFiles();
app.MapFallbackToFile("index.html");

app.Use(async (context, next) =>
{
	context.Features.Get<IHttpMaxRequestBodySizeFeature>()!.MaxRequestBodySize = 4294967296L;
	await next();
});

app.Run();
