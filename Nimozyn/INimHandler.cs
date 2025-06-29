using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace Nimozyn;

public interface INimSupervisor;
public interface INimRootSupervisor : INimSupervisor;
public interface INimScopeSupervisor : INimSupervisor;

public interface INimHandler;
public interface INimHandler<T> : INimHandler where T : INimInput;

public interface INimBus;

public interface INimInput;
public interface INimRequest<T> : INimInput;

public static partial class NimDiscovery
{
    public static IServiceCollection ScanNimHandlersAsync(this IServiceCollection collection)
    {
        foreach (ServiceDescriptor? service in collection.ToList())
        {
            if (service.ImplementationType is null)
                continue;

            Type? extended = null;//Extend(service.ImplementationType, service.Lifetime);

            if (extended is null) continue;

            collection.Remove(service);

            collection.Add(ServiceDescriptor.Describe(
                service.ServiceType,
                extended,
                service.Lifetime));
        }

        return collection;
    }
}
public static partial class NimDiscovery
{
    private static ConcurrentDictionary<Assembly, ModuleBuilder> ModuleBuilders;

    private const string ASSIMBLY_PREFIX = "Runtime_Nim_Assembly_";
    private const string TYPE_PREFIX = "Runtime_Nim_Type_";
    private const string SUPERVISOR_FIELD_NAME = "Runtime_Nim_Supervisor_Field";
    private static readonly Type SUPERVISOR_TYPE = typeof(INimSupervisor);

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
        var moduleBuilder = GenerateModuleBuilder(pure, type);
        var typeBuilder = GenerateType(type, moduleBuilder);
        GenerateConstructors(type, lifetime, typeBuilder);

        foreach (var item in type.GetRuntimeMethods())
            DescribeMethod(item, typeBuilder, lifetime);

        return typeBuilder.CreateType()!;
    }

    private static void DescribeMethod(MethodInfo item, TypeBuilder typeBuilder, ServiceLifetime lifetime)
    {
        var descriptor = new NimServiceDescriptor { };








        GenerateMethod(descriptor, item, typeBuilder, lifetime);
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

    private static void GenerateMethod(NimServiceDescriptor descriptor, MethodInfo methodinfo, TypeBuilder type, ServiceLifetime lifetime)
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


        il.DeclareLocal(methodinfo.ReturnType); // 0
        il.DeclareLocal(typeof(Exception)); //     1

        if (!IsTask(returnType))
        {
            var exBlock = il.BeginExceptionBlock();
        }

        for (var i = 0; i < parameters.Count + 1; i++)
            il.Emit(OpCodes.Ldarg, i);
        il.Emit(OpCodes.Call, methodinfo);

        if (!IsTask(returnType))
        {

        }
        else if (typeof(Task<>).IsAssignableFrom(returnType))
        {
            Type[] genArg = [returnType.GenericTypeArguments[0]];

            var suppressor = "SuppressTask";

            il.Emit(OpCodes.Call, typeof(NimDiscovery).GetMethods().First(x => x.Name == suppressor).MakeGenericMethod(genArg));
        }
        else if (typeof(ValueTask<>).IsAssignableFrom(returnType))
        {
            Type[] genArg = [returnType.GenericTypeArguments[0]];

            var suppressor = "SuppressValueTask";

            il.Emit(OpCodes.Call, typeof(NimDiscovery).GetMethods().First(x => x.Name == "SuppressValueTask").MakeGenericMethod(genArg));
        }
        else if (typeof(Task).IsAssignableFrom(returnType))
        {
            var suppressor = "SuppressTaskVoid";

            il.Emit(OpCodes.Call, typeof(NimDiscovery).GetMethods().First(x => x.Name == suppressor).MakeGenericMethod([]));
        }
        else if (typeof(ValueTask).IsAssignableFrom(returnType))
        {
            var suppressor = "SuppressValueTaskVoid";

            il.Emit(OpCodes.Call, typeof(NimDiscovery).GetMethods().First(x => x.Name == "SuppressValueTask").MakeGenericMethod([]));
        }








    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    private static bool IsTask(Type returnType) =>
        typeof(Task).IsAssignableFrom(returnType) ||
        typeof(Task<>).IsAssignableFrom(returnType) ||
        typeof(ValueTask).IsAssignableFrom(returnType) ||
        typeof(ValueTask<>).IsAssignableFrom(returnType);


    [DebuggerStepThrough]
    public static async Task SuppressTaskVoid<T>(Task input)
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
    public static async ValueTask SuppressValueTaskVoid<T>(ValueTask input)
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

public class NimServiceDescriptor
{
    public List<INimBlock> InputFilterBlocks { get; set; }
    public List<INimBlock> PreExecuteBlocks { get; set; }
    public List<INimBlock> PostExecuteBlocks { get; set; }
    public List<INimBlock> OnErrorBlocks { get; set; }
    public List<INimBlock> OutputFilterBlocks { get; set; }
}

public interface INimBlock;
public interface INimNeutralBlock : INimBlock
{
    Task Execute();
}
public class NimBlockTest : INimNeutralBlock
{
    public Task Execute()
    {
        throw new NotImplementedException();
    }
}



public interface INimTransparentBlock : INimBlock;
public interface INimTransparentBlock<T> : INimTransparentBlock where T : INimInput
{
    Task Execute(T request);
}
public class NimTransparentBlockTest<T> : INimTransparentBlock<T> where T : INimInput
{
    public Task Execute(T request)
    {
        throw new NotImplementedException();
    }
}



public interface INimTransformBlock : INimBlock;
public interface INimTransformBlock<T, R> : INimTransformBlock where T : INimInput where R : T
{
    Task<R> Execute(T request);
}
public class NimTransformBlockTest<T, R> : INimTransformBlock<T, R> where T : INimInput where R : T
{
    public Task<R> Execute(T request)
    {
        throw new NotImplementedException();
    }
}



public class NimRequestLinkerAttribute<T>(ServiceLifetime targetLifeTime) : Attribute where T : INimInput
{
    public Type BlockType => typeof(T);
}

public class NimAspectLinkerAttribute<T>(ServiceLifetime targetLifeTime) : Attribute where T : INimBlock
{
    public Type BlockType => typeof(T);
}

public class NimGeneratedClassAttribute : Attribute;