using PlaylistRepoAPI;

// collect repo
if (args.Length == 0)
{
	Console.Error.WriteLine("Provide a repo directory path.");
	Environment.Exit(1);
}

DirectoryInfo path = new(args[0]);

// build api
var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddCommandLine(args);
string url = builder.Configuration["ApiBaseUrl"] ?? "https://localhost:4271";
builder.WebHost.UseUrls(url);

// Add services to the container.
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddSingleton<ITaskService, TaskService>();
builder.Services.AddSingleton<IPlayRepoService, PlayRepoService>(serviceProvider =>
{
	return new PlayRepoService(path.FullName);
});

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<PlayRepoDbContext>();

// Add Swagger services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Enable Swagger in development
if (app.Environment.IsDevelopment() || true) // use `|| true` for always-on in local dev
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.MapSwagger();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
