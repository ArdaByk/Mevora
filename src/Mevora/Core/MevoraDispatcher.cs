using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using System.Threading.Tasks;

namespace Mevora
{
    public class MevoraDispatcher : IMevoraDispatcher
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ConcurrentDictionary<Type, Delegate> _asyncRequestProcessors;
        private readonly ConcurrentDictionary<Type, Delegate> _requestProcessors;

        public MevoraDispatcher(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _asyncRequestProcessors = new ConcurrentDictionary<Type, Delegate>();
            _requestProcessors = new ConcurrentDictionary<Type, Delegate>();
        }

        public TResponse Dispatch<TResponse>(IRequest<TResponse> request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            var requestType = request.GetType();
            var invoker = _requestProcessors.GetOrAdd(requestType, type => BuildSyncProcessor<TResponse>(type));

            return ((SyncProcessorInvoker<TResponse>)invoker)(request);
        }

        public void Dispatch(IRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            var requestType = request.GetType();
            var invoker = _requestProcessors.GetOrAdd(requestType, type => BuildSyncVoidProcessor(type));

            ((SyncVoidProcessorInvoker)invoker)(request);
        }

        public Task<TResponse> DispatchAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            var requestType = request.GetType();
            var invoker = _asyncRequestProcessors.GetOrAdd(requestType, type => BuildAsyncProcessor<TResponse>(type));

            return ((AsyncProcessorInvoker<TResponse>)invoker)(request, _serviceProvider, cancellationToken);
        }

        public Task DispatchAsync(IRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            var requestType = request.GetType();
            var invoker = _asyncRequestProcessors.GetOrAdd(requestType, type => BuildAsyncVoidProcessor(type));

            return ((AsyncVoidProcessorInvoker)invoker)(request, _serviceProvider, cancellationToken);
        }

        private Delegate BuildAsyncProcessor<TResponse>(Type requestType)
        {
            var processorType = typeof(IRequestProcessorAsync<,>).MakeGenericType(requestType, typeof(TResponse));
            var processMethod = processorType.GetMethod("ProcessAsync");

            var dynamicMethod = new DynamicMethod(
                name: $"AsyncInvoker_{requestType.Name}",
                returnType: typeof(Task<TResponse>),
                parameterTypes: new[] { typeof(IRequest<TResponse>), typeof(IServiceProvider), typeof(CancellationToken) },
                typeof(MevoraDispatcher).Module,
                skipVisibility: false);

            var generator = dynamicMethod.GetILGenerator();

            generator.Emit(OpCodes.Ldarg_1);
            generator.Emit(OpCodes.Ldtoken, processorType);
            generator.Emit(OpCodes.Call, typeof(Type).GetMethod("GetTypeFromHandle", BindingFlags.Static | BindingFlags.Public));
            generator.Emit(OpCodes.Callvirt, typeof(IServiceProvider).GetMethod("GetService", new[] { typeof(Type) }));
            generator.Emit(OpCodes.Castclass, processorType);

            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Castclass, requestType);

            generator.Emit(OpCodes.Ldarg_2);

            generator.Emit(OpCodes.Callvirt, processMethod);
            generator.Emit(OpCodes.Ret);

            return dynamicMethod.CreateDelegate(typeof(AsyncProcessorInvoker<TResponse>));
        }

        private Delegate BuildAsyncVoidProcessor(Type requestType)
        {
            var processorType = typeof(IRequestProcessorAsync<>).MakeGenericType(requestType);
            var processMethod = processorType.GetMethod("ProcessAsync");

            var dynamicMethod = new DynamicMethod(
                name: $"AsyncVoidInvoker_{requestType.Name}",
                returnType: typeof(Task),
                parameterTypes: new[] { typeof(IRequest), typeof(IServiceProvider), typeof(CancellationToken) },
                typeof(MevoraDispatcher).Module,
                skipVisibility: false);

            var generator = dynamicMethod.GetILGenerator();

            generator.Emit(OpCodes.Ldarg_1);
            generator.Emit(OpCodes.Ldtoken, processorType);
            generator.Emit(OpCodes.Call, typeof(Type).GetMethod("GetTypeFromHandle", BindingFlags.Static | BindingFlags.Public));
            generator.Emit(OpCodes.Callvirt, typeof(IServiceProvider).GetMethod("GetService", new[] { typeof(Type) }));
            generator.Emit(OpCodes.Castclass, processorType);

            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Castclass, requestType);

            generator.Emit(OpCodes.Ldarg_2);

            generator.Emit(OpCodes.Callvirt, processMethod);
            generator.Emit(OpCodes.Ret);

            return dynamicMethod.CreateDelegate(typeof(AsyncVoidProcessorInvoker));
        }

        private Delegate BuildSyncProcessor<TResponse>(Type requestType)
        {
            var processorType = typeof(IRequestProcessor<,>).MakeGenericType(requestType, typeof(TResponse));
            var processMethod = processorType.GetMethod("Process");

            var dynamicMethod = new DynamicMethod(
                name: $"SyncInvoker_{requestType.Name}",
                returnType: typeof(TResponse),
                parameterTypes: new[] { typeof(IRequest<TResponse>) },
                typeof(MevoraDispatcher).Module,
                skipVisibility: false);

            var generator = dynamicMethod.GetILGenerator();

            generator.Emit(OpCodes.Ldtoken, processorType);
            generator.Emit(OpCodes.Call, typeof(Type).GetMethod("GetTypeFromHandle", BindingFlags.Static | BindingFlags.Public));
            generator.Emit(OpCodes.Call, typeof(ServiceProviderServiceExtensions).GetMethod("GetRequiredService", BindingFlags.Static | BindingFlags.Public, null, new[] { typeof(IServiceProvider), typeof(Type) }, null)
                .MakeGenericMethod(processorType));
            generator.Emit(OpCodes.Castclass, processorType);

            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Castclass, requestType);

            generator.Emit(OpCodes.Callvirt, processMethod);
            generator.Emit(OpCodes.Ret);

            return dynamicMethod.CreateDelegate(typeof(SyncProcessorInvoker<TResponse>));
        }

        private Delegate BuildSyncVoidProcessor(Type requestType)
        {
            var processorType = typeof(IRequestProcessor<>).MakeGenericType(requestType);
            var processMethod = processorType.GetMethod("Process");

            var dynamicMethod = new DynamicMethod(
                name: $"SyncVoidInvoker_{requestType.Name}",
                returnType: typeof(void),
                parameterTypes: new[] { typeof(IRequest) },
                typeof(MevoraDispatcher).Module,
                skipVisibility: false);

            var generator = dynamicMethod.GetILGenerator();

            generator.Emit(OpCodes.Ldtoken, processorType);
            generator.Emit(OpCodes.Call, typeof(Type).GetMethod("GetTypeFromHandle", BindingFlags.Static | BindingFlags.Public));
            generator.Emit(OpCodes.Call, typeof(ServiceProviderServiceExtensions).GetMethod("GetRequiredService", BindingFlags.Static | BindingFlags.Public, null, new[] { typeof(IServiceProvider), typeof(Type) }, null)
                .MakeGenericMethod(processorType));
            generator.Emit(OpCodes.Castclass, processorType);

            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Castclass, requestType);

            generator.Emit(OpCodes.Callvirt, processMethod);
            generator.Emit(OpCodes.Ret);

            return dynamicMethod.CreateDelegate(typeof(SyncVoidProcessorInvoker));
        }

        private delegate Task<TResponse> AsyncProcessorInvoker<TResponse>(IRequest<TResponse> request, IServiceProvider serviceProvider, CancellationToken cancellationToken);
        private delegate Task AsyncVoidProcessorInvoker(IRequest request, IServiceProvider serviceProvider, CancellationToken cancellationToken);
        private delegate TResponse SyncProcessorInvoker<TResponse>(IRequest<TResponse> request);
        private delegate void SyncVoidProcessorInvoker(IRequest request);
    }
}
