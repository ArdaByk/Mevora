<p align="center">
  <img src="docs/logo.png" alt="Mevora Logo" width="150"/>
</p>

# Mevora

**Mevora** is a high-performance, compile-time powered CQRS and Mediator framework for .NET. 

By leveraging C# Source Generators, Mevora analyzes your project at compile-time and dynamically generates the dispatcher classes. This eliminates the heavy runtime reflection overhead typically seen in traditional mediator frameworks, making Mevora blazingly fast, Native AOT friendly, and strictly type-safe.

---

## Key Features

* **Zero Runtime Reflection**: Dispatchers and method bindings are generated entirely at compile-time.
* **Compile-Time Safety**: If you forget to write a handler for a request, you get a compiler error immediately, not a runtime crash!
* **First-Class Validation**: Built-in support for performant, object-pooled validation pipelines.
* **Matryoshka Pipelines**: Easily inject behaviors (Caching, Logging, Transaction Handling) around your request processors.
* **Dependency Lifecycle Aware**: Full support for `Transient`, `Scoped`, and `Singleton` handlers without captive dependency leaks.
* **Event Publishing**: Publish-Subscribe architecture out of the box for handling domain events.

---

## Installation

You can install Mevora via the NuGet Package Manager or the .NET CLI.

**NuGet Package Manager Console:**
```bash
Install-Package Mevora
```

**.NET CLI:**
```bash
dotnet add package Mevora
```

---

## Getting Started

To start using Mevora, you need to register it with the ASP.NET Core (or generic Host) completely natively using Dependency Injection.

### 1. Configuration

In your `Program.cs` or `Startup.cs`, register the processors and the dispatcher. 

```csharp
using Mevora;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMevora(cfg =>
{
    // Scan assemblies to find Processors, Handlers, and Validators
    cfg.AddProcessorsFromAssembly(typeof(Program).Assembly);
    
    // (Optional) Register pipelines in the order you want them to execute
    cfg.AddPipelineAction(typeof(LoggingPipeline<,>));
    
    // (Optional) Change default handler lifetime (Transient is default)
    // cfg.WithServiceLifetime(ServiceLifetime.Scoped); 
});

// Finally, build and register the IMevoraDispatcher
builder.Services.AddMevoraDispatcher();

var app = builder.Build();
```

---

## Requests & Processors (CQRS)

Mevora splits operations into **Requests** (which convey the data) and **Processors** (which contain the business logic). There are two types of requests: those that return a value, and those that do not.

### Requests with a Response

Define a request by implementing `IRequest<TResponse>`.

```csharp
public record GetUserRequest(Guid UserId) : IRequest<UserDto>;
```

Next, implement the matching processor using `IRequestProcessorAsync<TRequest, TResponse>`.

```csharp
public class GetUserRequestProcessor : IRequestProcessorAsync<GetUserRequest, UserDto>
{
    private readonly IUserRepository _repository;

    public GetUserRequestProcessor(IUserRepository repository)
    {
        _repository = repository;
    }

    public async Task<UserDto> ProcessAsync(GetUserRequest request, CancellationToken cancellationToken)
    {
        var user = await _repository.GetUserAsync(request.UserId, cancellationToken);
        return new UserDto(user.Id, user.Name);
    }
}
```

### Dispatching Requests

Mevora will automatically discover your `GetUserRequestProcessor`! To dispatch the request, simply inject `IMevoraDispatcher` and call `DispatchAsync`.

```csharp
[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IMevoraDispatcher _dispatcher;

    public UsersController(IMevoraDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetUser(Guid id, CancellationToken ct)
    {
        var request = new GetUserRequest(id);
        
        // This is type-safe and incredibly fast!
        UserDto result = await _dispatcher.DispatchAsync(request, ct);
        
        return Ok(result);
    }
}
```

> **Note:** If you create a `GetUserDetailsRequest` but forget to create the matching processor, your project will **refuse to compile**. Mevora strongly protects your architecture! 🛡️

---

## Event Publishing (Pub/Sub)

For logic that involves notifying multiple parts of your system without waiting for a single response, Mevora provides an Event Publishing system via `IMessage`.

### Defining & Processing Messages

```csharp
public record OrderCreatedMessage(Guid OrderId) : IMessage;

// Handler 1: Sends an email
public class SendOrderConfirmationEmail : IMessageProcessor<OrderCreatedMessage>
{
    public async Task Run(OrderCreatedMessage message, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Sending email for Order {message.OrderId}");
    }
}

// Handler 2: Updates Inventory
public class UpdateInventoryOnOrderCreated : IMessageProcessor<OrderCreatedMessage>
{
    public async Task Run(OrderCreatedMessage message, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Reducing inventory for Order {message.OrderId}");
    }
}
```

### Publishing Messages

Use the `PublishAsync` method to trigger all registered processors simultaneously.

```csharp
await _dispatcher.PublishAsync(new OrderCreatedMessage(order.Id), cancellationToken);
```

---

## Pipeline Actions (Onion Architecture)

Mevora supports pipeline behaviors that wrap your core processors. This is perfect for cross-cutting concerns like logging, caching, metrics, or database transactions.

Pipeline actions are registered sequentially and execute in a matryoshka (onion) fashion.

### Example: A Logging Pipeline

```csharp
public class LoggingPipeline<TRequest, TResponse> : IPipelineAction<TRequest, TResponse> 
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingPipeline<TRequest, TResponse>> _logger;

    public LoggingPipeline(ILogger<LoggingPipeline<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Run(
        TRequest request, 
        ProcessorDelegate<TResponse> next, 
        CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Starting Request: {typeof(TRequest).Name}");
        
        var stopwatch = Stopwatch.StartNew();
        
        // Execute the next pipeline in the chain, or the actual Processor itself
        var response = await next();
        
        stopwatch.Stop();
        
        _logger.LogInformation($"Completed Request: {typeof(TRequest).Name} in {stopwatch.ElapsedMilliseconds}ms");
        
        return response;
    }
}
```

Make sure to register your pipeline in the `AddMevora` configuration!

```csharp
cfg.AddPipelineAction(typeof(LoggingPipeline<,>));
```

---

## Built-in Validation System

Mevora comes with an ultra-fast, object-pooled validation system. Before a request even reaches the processor or pipelines, it is validated. If it fails, a `ValidationException` is thrown, short-circuiting the entire process.

### Creating a Validator

To validate a request, implement `IRequestValidator<TRequest>`. The `ValidationContext` provides fluent methods out-of-the-box (`CheckNotEmpty`, `CheckMinLength`, `CheckRange`, etc.).

```csharp
public class CreateUserRequestValidator : IRequestValidator<CreateUserRequest>
{
    public ValidationResult Validate(ValidationContext<CreateUserRequest> context)
    {
        context.CheckNotEmpty(r => r.Username, "Username cannot be empty")
               .CheckMinLength(r => r.Username, 3, "Username must be at least 3 characters")
               .CheckNotEmpty(r => r.Email, "Email must be provided");
               
        return context.Result;
    }
}
```

Validators are seamlessly cached, and `ValidationContext` objects are concurrently pooled, meaning the GC (Garbage Collector) receives near-zero overhead during high-traffic validation.

---

## Frequently Asked Questions

**Does Mevora have Lifecycle issues like Captive Dependencies?**
No. Mevora resolves your processors correctly inside the HTTP Request Scope using pure dependency injection. `Transient` processors are created newly on every request, and `Scoped` processors correctly resolve their nested Entity Framework Contexts without memory leaking.

**Can I use CancellationToken in my requests?**
Yes. Every request and pipeline takes a `CancellationToken`. It bubbles down completely smoothly so you can terminate database queries exactly when the client drops the request!

**Do exceptions bubble up correctly?**
Yes! If your `IRequestProcessor` throws an exception, Mevora ensures that the raw StackTrace surfaces beautifully to your global exception handler. Pipeling completely respects async contexts.
