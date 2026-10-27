namespace ADNoirBase.Shared.Contracts.Responses;

public static class ResponseArgs
{
    public static IReadOnlyDictionary<string, object?>? From(
        (string Key, object? Value)[] args)
    {
        if (args is null || args.Length == 0)
            return null;

        return args.ToDictionary(a => a.Key, a => a.Value);
    }
}
