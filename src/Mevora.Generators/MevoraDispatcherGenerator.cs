using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;

namespace Mevora.Generators;

[Generator]
internal class MevoraDispatcherGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var requestDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => node is ClassDeclarationSyntax,
                transform: static (ctx, _) => TypeDiscovery.GetTypes(ctx))
            .Where(static m => m is not null)
            .Collect();

        var compilationAndRequests = context.CompilationProvider.Combine(requestDeclarations);

        context.RegisterSourceOutput(compilationAndRequests,
            static (spc, source) => GenerateDispatcher(spc, source.Left, source.Right!));
    }

    private static void GenerateDispatcher(
        SourceProductionContext context,
        Compilation compilation,
        ImmutableArray<INamedTypeSymbol> requestTypes)
    {

        var builder = new SourceBuilder();

        builder.AppendHeader();
        builder.BeginClass("Mevora", "MevoraDispatcher", "IMevoraDispatcher");

        builder.Append(@"
   
     private T[] GetCachedPipelineActions<T>(Type requestType)
    {
        if (!_cachedPipelineActions.TryGetValue(requestType, out var cached))
        {
            var pipelineActions = _serviceProvider.GetServices<T>().ToArray();
            cached = pipelineActions.Cast<object>().ToArray();
            _cachedPipelineActions.TryAdd(requestType, cached);
        }
        return cached.Cast<T>().ToArray();
    }

    private async Task ExecutePipelineAsync<TRequest>(
        IPipelineAction<TRequest>[] pipelineActions, 
        TRequest request, 
        CancellationToken cancellationToken,
        ProcessorDelegate processor)
        where TRequest : IRequest
    {
        if (pipelineActions.Length == 0)
        {
            await processor();
            return;
        }

        ProcessorDelegate pipeline = processor;
        
        for (int i = pipelineActions.Length - 1; i >= 0; i--)
        {
            var pipelineAction = pipelineActions[i];
            var currentPipeline = pipeline;
            pipeline = () => pipelineAction.Run(request, currentPipeline, cancellationToken);
        }
        
        await pipeline();
    }

    private async Task<TResponse> ExecutePipelineAsync<TRequest, TResponse>(
        IPipelineAction<TRequest, TResponse>[] pipelineActions, 
        TRequest request, 
        CancellationToken cancellationToken,
        ProcessorDelegate<TResponse> processor)
        where TRequest : IRequest<TResponse>, IRequest
    {
        if (pipelineActions.Length == 0)
        {
            return await processor();
        }

        ProcessorDelegate<TResponse> pipeline = processor;
        
        for (int i = pipelineActions.Length - 1; i >= 0; i--)
        {
            var pipelineAction = pipelineActions[i];
            var currentPipeline = pipeline;
            pipeline = () => pipelineAction.Run(request, currentPipeline, cancellationToken);
        }
        
        return await pipeline();
    }

    private bool HasValidator<TRequest>() where TRequest : IRequest
    {
        return _serviceProvider.GetService<IRequestValidator<TRequest>>() != null;
    }

    private async Task ValidateRequestAsync<TRequest>(TRequest request) where TRequest : IRequest
    {
        var validator = _serviceProvider.GetService<IRequestValidator<TRequest>>();
        if (validator == null)
            return;

        try
        {
            var context = new ValidationContext<TRequest>(request);
            var result = validator.Validate(context);
            
            if (!result.IsValid)
                throw new ValidationException(result.Errors);
        }
        catch (ValidationException)
        {
            throw; // Re-throw validation exceptions as-is
        }
        catch (Exception ex)
        {
            throw new ValidationException($""Validation failed with error: { ex.Message}"");
        }
    }

  private Func<object, CancellationToken, Task>[] GetCachedMessageDelegates<T>() where T : IMessage
{
    var msgType = typeof(T);

    if (!_cachedMessageDelegates.TryGetValue(msgType, out var cached))
    {
        var delegates = _serviceProvider
            .GetServices<IMessageProcessor<T>>()
            .GroupBy(p => p.GetType()) // duplicate processor'ları engelle
            .Select(g => g.First())
            .Select<IMessageProcessor<T>, Func<object, CancellationToken, Task>>(proc =>
                async (msg, ct) => await proc.Run((T)msg, ct))
            .ToArray();

        _cachedMessageDelegates.TryAdd(msgType, delegates);
        cached = delegates;
    }

    return cached;
}
");

        var dispatcherGen = new DispatcherMethodGenerator(compilation, requestTypes);
        dispatcherGen.Generate(builder);

        var publisherGen = new MessagePublisherGenerator(compilation, requestTypes);
        publisherGen.Generate(builder);

        builder.EndClass();

        context.AddSource("MevoraDispatcher.g.cs", builder.ToString());
    }

}
