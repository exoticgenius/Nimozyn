using EGO.Gladius.Contracts;
using EGO.Gladius.DataTypes;
using EGO.Nimozyn.Attributes;
using EGO.Nimozyn.Buses;
using EGO.Nimozyn.Descriptors;
using EGO.Nimozyn.Enums;
using EGO.Nimozyn.Interfaces;
using EGO.Nimozyn.Managers;
using EGO.Nimozyn.Supervisors;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;


namespace EGO.Nimozyn.Discoveries;



public static partial class NimDiscovery
{
    private static ConcurrentDictionary<Assembly, ModuleBuilder> ModuleBuilders;

    private const string ASSIMBLY_PREFIX = "Runtime_Nim_Assembly_";
    private const string TYPE_PREFIX = "Runtime_Nim_Type_";
    private const string SUPERVISOR_FIELD_NAME = "Runtime_Nim_Supervisor_Field";
    private static readonly Type SUPERVISOR_TYPE = typeof(INimSupervisor);

    static NimDiscovery()
    {
        ModuleBuilders = new();
    }

    public static ImmutableList<ExpandedHandler> ScanNimHandlersAsync(this IServiceCollection collection)
    {
        collection.TryAddSingleton<INimRootSupervisor, NimRootSupervisor>();
        collection.TryAddScoped<INimScopeSupervisor, NimScopeSupervisor>();

        if (collection.Any(x => x.ImplementationType?.IsAssignableTo(typeof(INimHandler)) == true))
            throw new InvalidProgramException("handlers must not be registered as services before scan");

        var hType = typeof(INimHandler);
        var allHandlers = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(x => x.GetTypes())
            .Where(x => x.IsAssignableTo(hType) && !x.IsInterface && x is not null)
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

        return allHandlers;
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
                var launcherType = GenerateHandlerLauncher(method);
                var launcherInstance = Activator.CreateInstance(launcherType);
                return new ExpandedHandlerMethod
                {
                    handlerMethod = method,
                    InputType = method.GetParameters()[0].ParameterType,
                    Lifetime = defaultLifetime = GetLifetime(methodLifeTimes, defaultLifetime),
                    CompatibilityMode = GetCompatibilityMode(methodLifeTimes, defaultCompMode),
                    LauncherType = launcherType,
                    LauncherInstance = (ILLauncher)launcherInstance,
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


    public static Type GenerateHandlerLauncher(MethodInfo targetMethod)
    {
        var type = targetMethod.DeclaringType;
        if (!typeof(INimHandler).IsAssignableFrom(type))
            return default!;

        var pure = ExtractPureType(type);

        var moduleBuilder = GenerateModuleBuilder(pure, type);
        var typeBuilder = GenerateLauncherType(targetMethod, moduleBuilder);

        GenerateLauncherMethod(targetMethod, typeBuilder);

        return typeBuilder.CreateType()!;
    }
    
    private static TypeBuilder GenerateLauncherType(MethodInfo targetMethod, ModuleBuilder moduleBuilder)
    {
        var typeName = $"{TYPE_PREFIX}{Guid.NewGuid()}_{targetMethod.DeclaringType!.Name}";
        var returnType = targetMethod.ReturnType;

        if (!returnType.IsAssignableTo(typeof(Task)))
        {
            returnType = typeof(Task<>).MakeGenericType(returnType);
        }

        var typeBuilder = moduleBuilder.DefineType(
            typeName,
            TypeAttributes.Public,
            null,
            [
                typeof(ILLauncher<,>)
                    .MakeGenericType(
                        typeof(INimInput),
                        returnType)
            ]);
        return typeBuilder;
    }

    private static void GenerateLauncherMethod(MethodInfo targetMethod, TypeBuilder typeBuilder)
    {

        Type[] inputType = [typeof(INimHandler), typeof(INimInput)];
        var returnType = targetMethod.ReturnType;

        if (!returnType.IsAssignableTo(typeof(Task)))
        {
            returnType = typeof(Task<>).MakeGenericType(returnType);
        }

        var method = typeBuilder.DefineMethod(
            "Execute",
            MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.Virtual | MethodAttributes.NewSlot,
            CallingConventions.Standard,
            returnType,
            inputType);
        var il = method.GetILGenerator();


        il.Emit(OpCodes.Ldarg_1);
        //il.Emit(OpCodes.Ldarg_1);
        //il.Emit(OpCodes.Callvirt, typeof(object).GetMethod("ToString"));
        //il.Emit(OpCodes.Call, typeof(Console).GetMethod("WriteLine", [typeof(string)]));

        il.Emit(OpCodes.Ldarg_2);
        //il.Emit(OpCodes.Ldarg_2);
        //il.Emit(OpCodes.Callvirt, typeof(object).GetMethod("ToString"));
        //il.Emit(OpCodes.Call, typeof(Console).GetMethod("WriteLine", [typeof(string)]));
        il.Emit(OpCodes.Callvirt, targetMethod);

        if (!targetMethod.ReturnType.IsAssignableTo(typeof(Task)))
        {
            il.Emit(OpCodes.Call, typeof(Task).GetMethod(nameof(Task.FromResult))!
                .MakeGenericMethod(targetMethod.ReturnType));
            //return;
        }

        il.Emit(OpCodes.Ret);

    }

    private static Type SelectSupervisor(ServiceLifetime lifetime) => lifetime switch
    {
        ServiceLifetime.Scoped => typeof(INimScopeSupervisor),
        _ => typeof(INimRootSupervisor),
    };



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

    public class BaseMatrix
    {
        public AspectMatrix AssemblyMatrix { get; set; }
        public AspectMatrix TypeMatrix { get; set; }
    }

    public class AspectMatrix
    {
        public List<ANimAspect> InputFilter { get; set; }
        public List<ANimAspect> Pre { get; set; }
        public List<ANimAspect> OnError { get; set; }
        public List<ANimAspect> Post { get; set; }
        public List<ANimAspect> OutputFilter { get; set; }
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

    private static void GenerateSPRMethod(MethodInfo methodinfo, TypeBuilder type)
    {
        var prms = new List<Type>();
        prms.AddRange(methodinfo.GetParameters().Select(x => x.ParameterType).ToArray());
        var returnType = methodinfo.ReturnType;
        var method = type.DefineMethod(
            methodinfo.Name,
            MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.Virtual | MethodAttributes.NewSlot,
            CallingConventions.Standard | CallingConventions.HasThis,
            methodinfo.ReturnType,
            methodinfo.GetParameters().Select(x => x.ParameterType).ToArray());
        var il = method.GetILGenerator();

        var exBlock = il.BeginExceptionBlock();
        var end = il.DefineLabel();
        il.DeclareLocal(methodinfo.ReturnType); //      0
        il.DeclareLocal(typeof(Type)); //               1
        il.DeclareLocal(typeof(Exception)); //          2

        for (var i = 0; i < prms.Count + 1; i++)
            il.Emit(OpCodes.Ldarg, i);
        il.Emit(OpCodes.Call, methodinfo);

        if (typeof(Task).IsAssignableFrom(returnType))
        {
            Type[] genArg = [returnType.GenericTypeArguments[0]];

            if (genArg[0].GenericTypeArguments.Length > 0)
                genArg = genArg[0].GenericTypeArguments;

            var suppressor = "SuppressTask";
            if (typeof(VSP).IsAssignableFrom(genArg[0]))
                suppressor = "SuppressTaskVoid";

            il.Emit(OpCodes.Call, typeof(NimDiscovery).GetMethods().First(x => x.Name == suppressor).MakeGenericMethod(genArg));
        }
        else if (typeof(ValueTask).IsAssignableFrom(returnType))
        {
            Type[] genArg = [returnType.GenericTypeArguments[0]];

            if (genArg[0].GenericTypeArguments.Length > 0)
                genArg = genArg[0].GenericTypeArguments;

            var suppressor = "SuppressValueTask";
            if (typeof(VSP).IsAssignableFrom(genArg[0]))
                suppressor = "SuppressValueTaskVoid";

            il.Emit(OpCodes.Call, typeof(NimDiscovery).GetMethods().First(x => x.Name == suppressor).MakeGenericMethod(genArg));
        }

        il.Emit(OpCodes.Stloc_0);
        il.Emit(OpCodes.Leave_S, end);
        il.BeginCatchBlock(typeof(Exception));

        il.Emit(OpCodes.Stloc_2);
        il.Emit(OpCodes.Ldtoken, methodinfo);
        il.Emit(OpCodes.Ldloc_1); // 1 => 0

        il.Emit(OpCodes.Ldc_I4, prms.Count);
        il.Emit(OpCodes.Newarr, typeof(object)); // => 1

        for (var i = 0; i < prms.Count; i++)
        {
            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Ldc_I4, i);
            il.Emit(OpCodes.Ldarg, i + 1);
            if (prms[i].IsValueType)
                il.Emit(OpCodes.Box, prms[i]);
            il.Emit(OpCodes.Stelem_Ref);
        }

        il.Emit(OpCodes.Ldloc_2); //2 => 2

        il.Emit(OpCodes.Newobj, typeof(SPF).GetConstructor(new Type[] { typeof(MethodInfo), typeof(object[]), typeof(Exception) })!);

        if (typeof(Task).IsAssignableFrom(returnType))
        {
            il.Emit(OpCodes.Newobj, returnType.GenericTypeArguments[0].GetConstructor(new Type[] { typeof(SPF) })!);
            il.Emit(OpCodes.Call, typeof(Task).GetRuntimeMethods().First(x => x.Name == "FromResult").MakeGenericMethod(returnType.GenericTypeArguments[0]));

        }
        else if (typeof(ValueTask).IsAssignableFrom(returnType))
        {
            il.Emit(OpCodes.Newobj, returnType.GenericTypeArguments[0].GetConstructor(new Type[] { typeof(SPF) })!);
            il.Emit(OpCodes.Call, typeof(ValueTask<>).MakeGenericType(returnType.GenericTypeArguments[0]).GetConstructor(new Type[] { returnType.GenericTypeArguments[0] })!);
        }
        else
        {
            il.Emit(OpCodes.Newobj, methodinfo.ReturnType.GetConstructor(new Type[] { typeof(SPF) })!);
        }
        il.Emit(OpCodes.Stloc_0);
        il.Emit(OpCodes.Leave_S, end);

        il.EndExceptionBlock();

        il.MarkLabel(end);
        il.Emit(OpCodes.Ldloc_0);
        il.Emit(OpCodes.Ret);
    }

    [DebuggerStepThrough]
    public static async Task<VSP> SuppressTaskVoid<T>(Task<VSP> input)
    {
        try
        {
            return await input;
        }
        catch (Exception ex)
        {
            return new VSP(new SPF(ex));
        }
    }

    [DebuggerStepThrough]
    public static async Task<SPR<T>> SuppressTask<T>(Task<SPR<T>> input)
    {
        try
        {
            return await input;
        }
        catch (Exception ex)
        {
            return new SPR<T>(new SPF(ex));
        }
    }

    [DebuggerStepThrough]
    public static async ValueTask<VSP> SuppressValueTaskVoid<T>(ValueTask<VSP> input)
    {
        try
        {
            return await input;
        }
        catch (Exception ex)
        {
            return new VSP(new SPF(ex));
        }
    }

    [DebuggerStepThrough]
    public static async ValueTask<SPR<T>> SuppressValueTask<T>(ValueTask<SPR<T>> input)
    {
        try
        {
            return await input;
        }
        catch (Exception ex)
        {
            return new SPR<T>(new SPF(ex));
        }
    }

}