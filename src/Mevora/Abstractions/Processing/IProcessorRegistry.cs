namespace Mevora.Abstractions.Processing;

public interface IProcessorRegistry
{
    IEnumerable<Type> GetRequestProcessorTypes();
    IEnumerable<Type> GetMessageProcessorTypes();
    IEnumerable<Type> GetValidatorTypes();
}
