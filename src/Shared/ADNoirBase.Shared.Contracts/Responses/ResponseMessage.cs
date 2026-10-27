namespace ADNoirBase.Shared.Contracts.Responses;

public sealed record ResponseMessage(
    string Code,
    MessageSeverity Severity,
    string? Text = null,
    IReadOnlyDictionary<string, object?>? Args = null)
{
    public ResponseMessage WithResolvedText(string? text) =>
        this with { Text = text };
}
