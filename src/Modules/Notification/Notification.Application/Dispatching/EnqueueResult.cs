namespace Notification.Application.Dispatching;

/// <summary>
/// Kuyruğa alma sonucu. Tercihleri nedeniyle atlanan alıcılar hata değildir;
/// çağıran taraf bunu başarı sayıp kullanıcıya bilgi olarak gösterebilir.
/// </summary>
public sealed record EnqueueResult(
    IReadOnlyList<Guid> QueuedMessageIds,
    IReadOnlyList<string> SkippedRecipients,
    IReadOnlyList<string> Errors)
{
    public bool IsSuccess => Errors.Count == 0;

    public static EnqueueResult Failure(params string[] errors) => new([], [], errors);
}
