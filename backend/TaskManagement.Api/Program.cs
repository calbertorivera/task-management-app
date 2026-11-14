using TaskManagement.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddTaskManagementApi(
    builder.Configuration);

var app = builder.Build();

await app.InitializeTaskManagementDatabaseAsync();

app.UseTaskManagementPipeline();

app.Run();