<!-- LOGO -->
<p align="center">
  <img src="docs/logo.png" alt="Mevora Logo" width="150"/>
</p>

# Mevora
Mevora enables you to quickly and easily perform certain request/response and event operations using the CQRS and Mediator design patterns. You can review the [documentation](#docs) to learn how to use Mevora.

## How does Mevora work?

Mevora identifies all your Request Processor, Pipeline Action, and Message Processor classes at the beginning of the project and creates a main MevoraDispatcher class. It creates the necessary methods for each Processor class within the class and makes them available for your use. Since these operations are performed at compile-time, it offers high-performance usage.

---

## Installation
You can download the Mevora library via Nuget.

```bash
Install-Package Mevora
```
or you can download it using the .NET CLI
```bash
dotnet add package Mevora
```

---

## The Components of Mevora
Below are the definitions of the components you can use.

`IRequest`: The Interface defining request classes (for operations that do not return a response).
`IRequest<TResponse>`: The Interface defining request classes (for operations that return a response).

`IRequestProcessor<TRequest>`: The Interface that defines the class that will process a request of type TRequest when it arrives (for operations that do not return a response).
`IRequestProcessor<TRequest, TResponse>`: The Interface that defines the class that will process a request of type TRequest when it arrives (for operations that return a response).

`IMessage`: The interface that defines the messages to be published.
`IMessageProcessor<TMessage>`: The interface that defines the classes that process published messages.

`IPipelineAction<TRequest, TResponse>`: The interface that defines the operations to be performed before or after the request processing stage.

The usage and details of these interfaces are described in the [documentation](#docs).

---

## Configuration
Configuration operations are performed through the `AddMevora` method written as an Extension. The Assemblies and PipelineActions to be used for scanning the relevant classes are defined here.

```csharp
builder.Services.AddMevora(config =>
{
  config
    .AddProcessorsFromAssembly(Assembly.GetExecutingAssembly())
    .AddPipelineAction(typeof(LoggingAction<,>))
    .AddPipelineAction(typeof(CachingAction<,>));
);
```
