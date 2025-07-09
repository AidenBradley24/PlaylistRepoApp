using LocalPlaylistMasterAPI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

// collect repo
if (args.Length == 0) 
{
	Console.Error.WriteLine("Provide a repo directory path.");
	Environment.Exit(1);
}

DirectoryInfo path = new(args[0]);

// build api
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddScoped(serviceProvider =>
{
	var optionsBuilder = new DbContextOptionsBuilder<PlayRepoDbContext>();
	var db = new PlayRepoDbContext(optionsBuilder.Options, path.FullName);
	db.Database.EnsureCreated();
	return db;
});

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
