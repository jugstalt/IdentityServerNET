using System.Reflection;

namespace IdentityServer.Nova.HttpProxy;

internal class Helper
{
    public static MethodInfo GetMethod<T>(string methodName)
            => typeof(T).GetMethod(methodName)
               ?? throw new InvalidOperationException($"Method {methodName} not found.");

    public static MethodInfo GetMethod<T>(string methodName, Type[] argumentTypes)
            => typeof(T).GetMethod(methodName, argumentTypes)
               ?? throw new InvalidOperationException($"Method {methodName} not found.");
}
