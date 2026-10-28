using System.Reflection;

namespace Identity.Application.Tests;

public class InterfaceStub : DispatchProxy
{
    public Func<string, object?[]?, object?> Handle { get; set; } = (_, _) => throw new InvalidOperationException("Unexpected call");
    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => Handle(targetMethod!.Name, args);
    public static T For<T>(Func<string, object?[]?, object?> handle) where T : class
    {
        var instance = Create<T, InterfaceStub>();
        ((InterfaceStub)(object)instance).Handle = handle;
        return instance;
    }
}
