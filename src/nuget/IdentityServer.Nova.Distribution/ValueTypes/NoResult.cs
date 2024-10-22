namespace IdentityServer.Nova.Distribution.ValueTypes;

public class NoResult
{
    public static T? Value<T>() => default;
}
