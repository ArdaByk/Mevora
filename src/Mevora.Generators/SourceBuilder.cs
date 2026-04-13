using System.Text;

namespace Mevora.Generators;

internal class SourceBuilder
{
    private readonly StringBuilder _sb = new();

    public void AppendHeader()
    {
        _sb.AppendLine(@"
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;
using Mevora;

");
    }

    public void BeginClass(string @namespace, string className, string iface)
    {
        _sb.AppendLine($@"namespace {@namespace};

public partial class {className} : {iface}
{{
    private readonly IServiceProvider _serviceProvider;
    
    private static readonly ConcurrentDictionary<Type, object[]> _cachedPipelineActions = new();
    private static readonly ConcurrentDictionary<Type, Func<object, CancellationToken, Task>> _asyncVoidDispatchers = new();
    private static readonly ConcurrentDictionary<Type, Func<object, CancellationToken, Task<object>>> _asyncGenericDispatchers = new();
    private static readonly ConcurrentDictionary<Type, Func<object, CancellationToken, Task>[]> _cachedMessageDelegates = new();
    
    private static readonly ConcurrentDictionary<Type, bool> _hasValidatorCache = new();
    private static readonly ConcurrentDictionary<Type, object> _cachedValidators = new();
    private static readonly ConcurrentDictionary<Type, ConcurrentBag<object>> _validationContextPool = new();


    public {className}(IServiceProvider serviceProvider)
    {{
        _serviceProvider = serviceProvider;
    }}
");
    }

    public void Append(string code)
    {
        _sb.AppendLine(code);
    }

    public void EndClass()
    {
        _sb.AppendLine("}");
    }

    public override string ToString() => _sb.ToString();
}
