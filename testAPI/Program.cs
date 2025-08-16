using BenchmarkDotNet.Running;
using System.Diagnostics;
using System.Reflection;
using testAPI.queries;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//BenchmarkRunner.Run<DispatcherBenchmarks>();

builder.Services.AddMevora(config =>
{
    config
    .AddProcessorsFromAssembly(Assembly.GetExecutingAssembly())
    .WithServiceLifetime(ServiceLifetime.Transient);
});

builder.Services.AddMediatR(cfg => 
    cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly())
    );

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
