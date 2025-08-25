using Mevora;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddMevora(config =>
{
    config.AddProcessorsFromAssemblies(new List<Assembly> { typeof(GetUserByIdQueryProcessor).Assembly, typeof(UserRegisteredMessageProcessor).Assembly, typeof(UserRegisteredMessageProcessor2).Assembly, typeof(UserRegisterValidator).Assembly })
    .AddProcessorsFromAssembly(typeof(UserRegisteredMessageProcessor2).Assembly)
    .AddPipelineAction(typeof(LoggingBehavior<,>))
    .WithServiceLifetime(ServiceLifetime.Singleton);
});

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
