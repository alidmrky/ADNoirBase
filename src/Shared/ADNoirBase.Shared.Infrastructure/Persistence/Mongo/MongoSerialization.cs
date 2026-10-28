using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;

namespace ADNoirBase.Shared.Infrastructure.Persistence.Mongo;

/// <summary>
/// Driver ayarları process genelinde ve ilk class map oluşmadan önce yapılmalıdır.
/// Bu yüzden DI kaydı sırasında, modüller kendi class map'lerini kaydetmeden önce çağrılır.
/// </summary>
public static class MongoSerialization
{
    private static int _configured;

    public static void Configure()
    {
        if (Interlocked.Exchange(ref _configured, 1) == 1)
        {
            return;
        }

        // Driver 3.x Guid için varsayılan gösterim tanımlamaz; belirtilmezse Guid Id'li entity'ler yazılamaz.
        BsonSerializer.TryRegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));

        var conventions = new ConventionPack
        {
            // Koddan kaldırılan alanlar eski dökümanları okunamaz hale getirmesin.
            new IgnoreExtraElementsConvention(true),

            // Enum'lar sayı olarak saklanırsa veritabanındaki kayıt elle okunamaz hale gelir ve
            // enum üyelerinin sırası değişirse eski dökümanlar sessizce yanlış yorumlanır.
            // Sıralamanın anlamlı olduğu alanlar (örneğin öncelik) modülünde int'e geri çevrilir.
            new EnumRepresentationConvention(BsonType.String)
        };

        ConventionRegistry.Register("ADNoirBase", conventions, _ => true);
    }
}
