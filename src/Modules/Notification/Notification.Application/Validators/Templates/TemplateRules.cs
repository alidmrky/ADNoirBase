namespace Notification.Application.Validators.Templates;

/// <summary>Şablon alanlarının sınırları. Create ve Update aynı sınırları paylaşır.</summary>
public static class TemplateRules
{
    /// <summary>Koddan referans verilebilsin diye kod yalnızca harf, rakam, nokta ve alt çizgi içerir.</summary>
    public const string CodePattern = "^[a-z0-9]+([._][a-z0-9]+)*$";

    public const int CodeMaxLength = 100;
    public const int NameMaxLength = 150;
    public const int DescriptionMaxLength = 500;
    public const int SubjectMaxLength = 300;

    /// <summary>SMS gövdesi uzun olabilir ama her 153 karakter ayrı bir mesaj olarak ücretlendirilir.</summary>
    public const int SmsBodyMaxLength = 1600;

    public const int BodyMaxLength = 200_000;
    public const int MaxVariables = 100;
    public const int VariableNameMaxLength = 100;

    /// <summary>Saklama süresi üst sınırı; bunun ötesi "süresiz" (0) demektir.</summary>
    public const int MaxRetentionDays = 3650;
}
