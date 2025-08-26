using Microsoft.CodeAnalysis;

namespace Mevora.Generators;

internal sealed class ProcessorDescriptor
{
    public INamedTypeSymbol ProcessorClass { get; }
    public bool IsAsync { get; }
    public bool IsRequestProcessor { get; }
    public ITypeSymbol RequestType { get; }
    public INamedTypeSymbol? ResponseType { get; }

    public ProcessorDescriptor(
        INamedTypeSymbol processorClass,
        bool isAsync,
        bool isRequestProcessor,
        ITypeSymbol requestType,
        INamedTypeSymbol? responseType)
    {
        ProcessorClass = processorClass;
        IsAsync = isAsync;
        IsRequestProcessor = isRequestProcessor;
        RequestType = requestType;
        ResponseType = responseType;
    }
}
