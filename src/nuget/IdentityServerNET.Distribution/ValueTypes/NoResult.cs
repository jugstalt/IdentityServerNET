namespace IdentityServerNET.Distribution.ValueTypes;

public class NoResult
{
    public static T? Value<T>() => default;
}
