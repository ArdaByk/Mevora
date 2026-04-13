using Microsoft.CodeAnalysis;

namespace Mevora.Generators;

internal sealed class ProcessorDescriptor
{
    public INamedTypeSymbol ProcessorClass { get; }
    public bool IsRequestProcessor { get; }
    public ITypeSymbol RequestType { get; }
    public INamedTypeSymbol? ResponseType { get; }

    public ProcessorDescriptor(
        INamedTypeSymbol processorClass,
        bool isRequestProcessor,
        ITypeSymbol requestType,
        INamedTypeSymbol? responseType)
    {
        ProcessorClass = processorClass;
        IsRequestProcessor = isRequestProcessor;
        RequestType = requestType;
        ResponseType = responseType;
    }
}
