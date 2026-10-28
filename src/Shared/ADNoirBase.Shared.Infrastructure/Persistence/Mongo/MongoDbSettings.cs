namespace ADNoirBase.Shared.Infrastructure.Persistence.Mongo;

/// <summary>Bağlantı dizesi ConnectionStrings:MongoDB'den okunur; burada yalnızca gizli olmayan ayarlar durur.</summary>
public sealed class MongoDbSettings
{
    public const string SectionName = "MongoDb";

    public string DatabaseName { get; init; } = "adnoirbase";
}
