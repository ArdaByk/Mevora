using Mevora;
using System.Reflection;
using Test.Features.Pipelines;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Register Mevora
builder.Services.AddMevora(config =>
{
    config.AddProcessorsFromAssembly(Assembly.GetExecutingAssembly());
    
    // Register Pipeline Actions
    config.AddPipelineAction(typeof(PerformanceLoggingAction<,>));
    config.AddPipelineAction(typeof(SimpleLoggingAction<>));
});

// Explicitly register the generated dispatcher for this assembly using the generated extension method
builder.Services.AddMevoraDispatcher();



var appBuilder = builder.Build();

// Configure the HTTP request pipeline.
if (appBuilder.Environment.IsDevelopment())
{
    appBuilder.MapOpenApi();
    
    // Fallback UI for swagger (redoc/scalar/swagger-ui), using standard map if available or just direct api hits
}

appBuilder.UseHttpsRedirection();

appBuilder.UseAuthorization();

appBuilder.MapControllers();

appBuilder.Run();
