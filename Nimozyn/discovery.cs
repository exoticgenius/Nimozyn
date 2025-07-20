using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

using static Nimozyn.NimDiscovery;

namespace Nimozyn;



public static partial class NimDiscovery
{
    public static IServiceCollection ScanNimHandlersAsync(this IServiceCollection collection)
    {
        collection.TryAddSingleton<INimRootSupervisor, NimRootSupervisor>();
        collection.TryAddScoped<INimScopeSupervisor, NimScopeSupervisor>();

        //foreach (ServiceDescriptor? service in collection.ToList())
        //{
        //    if (service.ImplementationType is null)
        //        continue;

        //    //Type? extended = Extend(service.ImplementationType, service.Lifetime);

        //    //if (extended is null) continue;

        //    collection.Remove(service);

        //    collection.Add(ServiceDescriptor.Describe(
        //        service.ServiceType,
        //        service.ImplementationType,
        //        service.Lifetime));
        //}

        if (collection.Any(x => x.ImplementationType?.IsAssignableTo(typeof(INimHandler)) == true))
            throw new InvalidProgramException("handlers must not be registered as services before scan");

        var hType = typeof(INimHandler);
        var allHandlers = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(x => x.GetTypes())
            .Where(x => x.IsAssignableTo(hType) && !x.IsInterface)
            .Select(ExpandByMethods)
            .ToImmutableList();

        foreach (var handler in allHandlers)
        {
            collection.Add(ServiceDescriptor.Describe(
                handler.ServiceType,
                handler.ServiceType,
                ConvertLifetime(handler.Lifetime)));
        }

        var manager = new NimManager(allHandlers);
        collection.AddSingleton(manager);
        collection.AddTransient<INimBus, NimBus>();


        return collection;
    }

    private static ServiceLifetime ConvertLifetime(NimLifetime lifetime) => lifetime switch
    {
        NimLifetime.Singleton => ServiceLifetime.Singleton,
        NimLifetime.Scoped => ServiceLifetime.Scoped,
        NimLifetime.Transient => ServiceLifetime.Transient,
        _ => throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, "Invalid ServiceLifetime")
    };


    private static ExpandedHandler ExpandByMethods(Type type)
    {
        var iType = typeof(INimInput);
        var defaultlifetimeAttrs = GetLifetimeAttrs(type);

        var defaultLifetime = NimLifetime.Scoped;
        var defaultCompMode = NimCompatibilityMode.Enforce;

        if (defaultlifetimeAttrs.Length > 0)
            defaultLifetime = defaultlifetimeAttrs[0].Lifetime;
        if (defaultlifetimeAttrs.Length > 0)
            defaultCompMode = defaultlifetimeAttrs[0].CompatibilityMode;

        var handlers = type
            .GetMethods()
            .Where(x => x.GetParameters().Count() > 0 && x.GetParameters().First().ParameterType.IsAssignableTo(iType))
            .Select(method =>
            {
                var methodLifeTimes = GetLifetimeAttrs(method);
                return new ExpandedHandlerMethod
                {
                    handlerMethod = method,
                    InputType = method.GetParameters()[0].ParameterType,
                    Lifetime = defaultLifetime = GetLifetime(methodLifeTimes, defaultLifetime),
                    CompatibilityMode = GetCompatibilityMode(methodLifeTimes, defaultCompMode)
                };
            })
            .ToImmutableList();

        if (handlers.DistinctBy(x => x.Lifetime).Count() > 1)
            throw new InvalidProgramException($"Handler {type.Name} has methods with different lifetimes. All methods must have the same lifetime.");

        var wrapper = new ExpandedHandler
        {
            ServiceType = type,
            Methods = handlers,
            Lifetime = defaultLifetime,
            BaseMatrix = GenerateBaseMatrix(type),
        };

        handlers.ForEach(x => x.HandlerWrapper = wrapper);

        return wrapper;

        static NimLifetime GetLifetime(NimLifetimeAttribute[] attrs, NimLifetime defaultLifetime) =>
            attrs.Length > 0 ?
            attrs[0].Lifetime :
            defaultLifetime;

        static NimCompatibilityMode GetCompatibilityMode(NimLifetimeAttribute[] attrs, NimCompatibilityMode defaultCompMode) =>
            attrs.Length > 0 ?
            attrs[0].CompatibilityMode :
            defaultCompMode;

        static NimLifetimeAttribute[] GetLifetimeAttrs(MemberInfo mi) => mi
            .GetCustomAttributes(true)
            .Where(x => x is NimLifetimeAttribute)
            .Select(x => (NimLifetimeAttribute)x)
            .ToArray();
    }















    private static ConcurrentDictionary<Assembly, ModuleBuilder> ModuleBuilders;

    private const string ASSIMBLY_PREFIX = "Runtime_Nim_Assembly_";
    private const string TYPE_PREFIX = "Runtime_Nim_Type_";
    private const string SUPERVISOR_FIELD_NAME = "Runtime_Nim_Supervisor_Field";
    private static readonly Type SUPERVISOR_TYPE = typeof(INimSupervisor);

    static NimDiscovery()
    {
        ModuleBuilders = new();
    }

    private static Type SelectSupervisor(ServiceLifetime lifetime) => lifetime switch
    {
        ServiceLifetime.Scoped => typeof(INimScopeSupervisor),
        _ => typeof(INimRootSupervisor),
    };

    public static Type Extend(Type type, ServiceLifetime lifetime)
    {
        if (!typeof(INimHandler).IsAssignableFrom(type))
            return null;

        var pure = ExtractPureType(type);
        var baseMatrix = GenerateBaseMatrix(type);

        var moduleBuilder = GenerateModuleBuilder(pure, type);
        var typeBuilder = GenerateType(type, moduleBuilder);
        GenerateConstructors(type, lifetime, typeBuilder);

        foreach (var item in type.GetRuntimeMethods())
            DescribeMethod(item, typeBuilder, lifetime, baseMatrix);

        return typeBuilder.CreateType()!;
    }

    private static BaseMatrix GenerateBaseMatrix(Type type)
    {
        var asmAttrs = type.Assembly.GetCustomAttributes(true);
        var typeAttrs = type.GetCustomAttributes(true);
        var bm = new BaseMatrix();


        bm.AssemblyMatrix = new AspectMatrix
        {
            InputFilter = asmAttrs
                .Where(x => x is ANimAspect na && na.AspectPosition is AspectPosition.Pre or AspectPosition.Wrap && na.BlockType.IsAssignableTo(typeof(INimTransparentBlock))).Select(x => (ANimAspect)x).ToList(),

            Pre = asmAttrs
                .Where(x => x is ANimAspect na && na.AspectPosition is AspectPosition.Pre or AspectPosition.Wrap && na.BlockType.IsAssignableTo(typeof(INimNeutralBlock))).Select(x => (ANimAspect)x).ToList(),

            OnError = asmAttrs
                .Where(x => x is ANimAspect na && na.BlockType is INimErrorBlock).Select(x => (ANimAspect)x).ToList(),

            Post = asmAttrs
                .Where(x => x is ANimAspect na && na.AspectPosition is AspectPosition.Post or AspectPosition.Wrap && na.BlockType.IsAssignableTo(typeof(INimNeutralBlock))).Select(x => (ANimAspect)x).ToList(),

            OutputFilter = asmAttrs
                .Where(x => x is ANimAspect na && na.AspectPosition is AspectPosition.Post or AspectPosition.Wrap && na.BlockType.IsAssignableTo(typeof(INimTransparentBlock))).Select(x => (ANimAspect)x).ToList()
        };

        bm.TypeMatrix = new AspectMatrix
        {
            InputFilter = typeAttrs
                 .Where(x => x is ANimAspect na && na.AspectPosition is AspectPosition.Pre or AspectPosition.Wrap && na.BlockType.IsAssignableTo(typeof(INimTransparentBlock))).Select(x => (ANimAspect)x).ToList(),

            Pre = typeAttrs
                 .Where(x => x is ANimAspect na && na.AspectPosition is AspectPosition.Pre or AspectPosition.Wrap && na.BlockType.IsAssignableTo(typeof(INimNeutralBlock))).Select(x => (ANimAspect)x).ToList(),

            OnError = typeAttrs
                 .Where(x => x is ANimAspect na && na.BlockType is INimErrorBlock).Select(x => (ANimAspect)x).ToList(),

            Post = typeAttrs
                 .Where(x => x is ANimAspect na && na.AspectPosition is AspectPosition.Post or AspectPosition.Wrap && na.BlockType.IsAssignableTo(typeof(INimNeutralBlock))).Select(x => (ANimAspect)x).ToList(),

            OutputFilter = typeAttrs
                 .Where(x => x is ANimAspect na && na.AspectPosition is AspectPosition.Post or AspectPosition.Wrap && na.BlockType.IsAssignableTo(typeof(INimTransparentBlock))).Select(x => (ANimAspect)x).ToList()
        };


        return bm;
    }
    internal class BaseMatrix
    {
        public AspectMatrix AssemblyMatrix { get; set; }
        public AspectMatrix TypeMatrix { get; set; }
    }
    internal class AspectMatrix
    {
        public List<ANimAspect> InputFilter { get; set; }
        public List<ANimAspect> Pre { get; set; }
        public List<ANimAspect> OnError { get; set; }
        public List<ANimAspect> Post { get; set; }
        public List<ANimAspect> OutputFilter { get; set; }
    }

    private static void DescribeMethod(MethodInfo targetMethod, TypeBuilder typeBuilder, ServiceLifetime lifetime, BaseMatrix baseMatrix)
    {
        var descriptor = new NimServiceDescriptor { };
        var methodAttrs = targetMethod.GetCustomAttributes(true);

        var methodMatrix = new AspectMatrix
        {
            InputFilter = methodAttrs
                    .Where(x => x is ANimAspect na && na.AspectPosition is AspectPosition.Pre or AspectPosition.Wrap && na.BlockType.IsAssignableTo(typeof(INimTransparentBlock))).Select(x => (ANimAspect)x).ToList(),

            Pre = methodAttrs
                    .Where(x => x is ANimAspect na && na.AspectPosition is AspectPosition.Pre or AspectPosition.Wrap && na.BlockType.IsAssignableTo(typeof(INimNeutralBlock))).Select(x => (ANimAspect)x).ToList(),

            OnError = methodAttrs
                    .Where(x => x is ANimAspect na && na.BlockType is INimErrorBlock).Select(x => (ANimAspect)x).ToList(),

            Post = methodAttrs
                    .Where(x => x is ANimAspect na && na.AspectPosition is AspectPosition.Post or AspectPosition.Wrap && na.BlockType.IsAssignableTo(typeof(INimNeutralBlock))).Select(x => (ANimAspect)x).ToList(),

            OutputFilter = methodAttrs
                    .Where(x => x is ANimAspect na && na.AspectPosition is AspectPosition.Post or AspectPosition.Wrap && na.BlockType.IsAssignableTo(typeof(INimTransparentBlock))).Select(x => (ANimAspect)x).ToList()
        };

        methodMatrix.Pre.AddRange(baseMatrix.AssemblyMatrix.Pre);
        methodMatrix.Pre.AddRange(baseMatrix.TypeMatrix.Pre);

        methodMatrix.OnError.AddRange(baseMatrix.TypeMatrix.OnError);

        methodMatrix.Post.AddRange(baseMatrix.AssemblyMatrix.Post);
        methodMatrix.Post.AddRange(baseMatrix.TypeMatrix.Post);

        methodMatrix.OutputFilter.AddRange(baseMatrix.AssemblyMatrix.OutputFilter
            .Where(x => x.BlockType.GetInterfaces().First(x => x.IsGenericType).GenericTypeArguments[0] == targetMethod.ReturnType));
        methodMatrix.OutputFilter.AddRange(baseMatrix.TypeMatrix.OutputFilter
            .Where(x => x.BlockType.GetInterfaces().First(x => x.IsGenericType).GenericTypeArguments[0] == targetMethod.ReturnType));

        methodMatrix.InputFilter.AddRange(baseMatrix.AssemblyMatrix.InputFilter
            .Where(x => targetMethod.GetParameters().Any(z => z.ParameterType == x.BlockType.GenericTypeArguments[0])));



        GenerateMethod(descriptor, targetMethod, typeBuilder, lifetime, methodMatrix);
    }

    private static Type ExtractPureType(Type type)
    {
        while (type.GetCustomAttribute<NimGeneratedClassAttribute>() != null) type = type.BaseType;
        return type;
    }

    private static ModuleBuilder GenerateModuleBuilder(Type pure, Type type)
    {
        return ModuleBuilders.GetOrAdd(
            type.Assembly,
            (a) => AssemblyBuilder
                .DefineDynamicAssembly(
                    new AssemblyName($"{ASSIMBLY_PREFIX}{a.FullName}"),
                    AssemblyBuilderAccess.Run)
                .DefineDynamicModule("Module"));
    }

    private static TypeBuilder GenerateType(Type type, ModuleBuilder moduleBuilder)
    {
        var typeName = $"{TYPE_PREFIX}{Guid.NewGuid()}_{type.Name}";

        var typeBuilder = moduleBuilder.DefineType(
            typeName,
            TypeAttributes.Public,
            type,
            type.GetInterfaces());
        return typeBuilder;
    }

    private static void GenerateConstructors(Type type, ServiceLifetime lifetime, TypeBuilder typeBuilder)
    {
        bool alreadyIsNim = false;
        if (type.GetField(SUPERVISOR_FIELD_NAME, BindingFlags.Instance) is null)
            typeBuilder.DefineField(SUPERVISOR_FIELD_NAME, SelectSupervisor(lifetime), FieldAttributes.Family);
        else
            alreadyIsNim = true;

        foreach (var item in type.GetConstructors())
            GenerateConstructor(typeBuilder, item, alreadyIsNim, lifetime);
    }
    private static void GenerateConstructor(TypeBuilder type, ConstructorInfo constructor, bool alreadyIsNim, ServiceLifetime lifetime)
    {
        var prms = new List<Type>();
        prms.AddRange(constructor.GetParameters().Select(x => x.ParameterType).ToArray());
        prms.Add(SelectSupervisor(lifetime));
        var newCtor = type.DefineConstructor(constructor.Attributes, constructor.CallingConvention, prms.ToArray());
        var il = newCtor.GetILGenerator();

        for (int i = 0, j = alreadyIsNim ? 0 : 1; i < prms.Count + j; i++)
            il.Emit(OpCodes.Ldarg, i);

        il.Emit(OpCodes.Call, constructor);

        if (alreadyIsNim)
        {
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg, prms.Count + 1);
            il.Emit(OpCodes.Stfld, type.GetField(SUPERVISOR_FIELD_NAME, BindingFlags.Instance)!);
        }

        il.Emit(OpCodes.Ret);
    }

    private static void GenerateMethod(NimServiceDescriptor descriptor, MethodInfo methodinfo, TypeBuilder type, ServiceLifetime lifetime, AspectMatrix methodMatrix)
    {
        var parameters = new List<Type>();
        parameters.AddRange(methodinfo.GetParameters().Select(x => x.ParameterType).ToArray());

        var returnType = methodinfo.ReturnType;

        var method = type.DefineMethod(
            methodinfo.Name,
            MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.Virtual | MethodAttributes.NewSlot,
            CallingConventions.Standard | CallingConventions.HasThis,
            methodinfo.ReturnType,
            methodinfo.GetParameters().Select(x => x.ParameterType).ToArray());
        var il = method.GetILGenerator();

        il.Emit(OpCodes.Ldarg_S, 1);
        foreach (var pre in methodMatrix.Pre)
        {
            il.Emit(OpCodes.Ldfld, type.GetFields().First(x => x.Name == SUPERVISOR_FIELD_NAME));
            il.Emit(OpCodes.Ldtoken, pre.BlockType);
            il.Emit(OpCodes.Callvirt, typeof(INimSupervisor).GetMethods().First(x => x.Name == "GetService"));
            il.Emit(OpCodes.Callvirt, typeof(INimNeutralBlock).GetMethods().First(x => x.Name == "Execute"));
            il.Emit(OpCodes.Starg_S, 1);
            il.Emit(OpCodes.Ldarg_S, 1);
        }

        if (IsTask(returnType))
            return;

        il.DeclareLocal(methodinfo.ReturnType); // 0
        il.DeclareLocal(typeof(Exception)); //     1



        var exBlock = il.BeginExceptionBlock();

        for (var i = 0; i < parameters.Count + 1; i++)
            il.Emit(OpCodes.Ldarg, i);
        il.Emit(OpCodes.Call, methodinfo);









        {
            //else if (typeof(Task<>).IsAssignableFrom(returnType))
            //{
            //    Type[] genArg = [returnType.GenericTypeArguments[0]];

            //    var suppressor = "SuppressTask";

            //    il.Emit(OpCodes.Call, typeof(NimDiscovery).GetMethods().First(x => x.Name == suppressor).MakeGenericMethod(genArg));
            //}
            //else if (typeof(ValueTask<>).IsAssignableFrom(returnType))
            //{
            //    Type[] genArg = [returnType.GenericTypeArguments[0]];

            //    var suppressor = "SuppressValueTask";

            //    il.Emit(OpCodes.Call, typeof(NimDiscovery).GetMethods().First(x => x.Name == "SuppressValueTask").MakeGenericMethod(genArg));
            //}
            //else if (typeof(Task).IsAssignableFrom(returnType))
            //{
            //    var suppressor = "SuppressTaskVoid";

            //    il.Emit(OpCodes.Call, typeof(NimDiscovery).GetMethods().First(x => x.Name == suppressor).MakeGenericMethod([]));
            //}
            //else if (typeof(ValueTask).IsAssignableFrom(returnType))
            //{
            //    var suppressor = "SuppressValueTaskVoid";

            //    il.Emit(OpCodes.Call, typeof(NimDiscovery).GetMethods().First(x => x.Name == "SuppressValueTask").MakeGenericMethod([]));
            //}
        }
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    private static bool IsTask(Type returnType) =>
        typeof(Task).IsAssignableFrom(returnType) ||
        typeof(Task<>).IsAssignableFrom(returnType) ||
        typeof(ValueTask).IsAssignableFrom(returnType) ||
        typeof(ValueTask<>).IsAssignableFrom(returnType);


    [DebuggerStepThrough]
    public static async Task SuppressTaskVoid(Task input)
    {
        try
        {
            await input;
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    [DebuggerStepThrough]
    public static async Task<T> SuppressTask<T>(Task<T> input)
    {
        try
        {
            return await input;
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    [DebuggerStepThrough]
    public static async ValueTask SuppressValueTaskVoid(ValueTask input)
    {
        try
        {
            await input;
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    [DebuggerStepThrough]
    public static async ValueTask<T> SuppressValueTask<T>(ValueTask<T> input)
    {
        try
        {
            return await input;
        }
        catch (Exception ex)
        {
            throw;
        }
    }

}
