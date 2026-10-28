namespace Identity.Application.Validators.User;

public static class PhoneNumberRules
{
    /// <summary>
    /// E.164: isteğe bağlı '+', ardından 7-15 rakam. Ülke bazlı biçimleri doğrulamaya çalışmak
    /// geçerli numaraları eleyeceği için yalnızca bu geniş kural uygulanır.
    /// </summary>
    public const string Pattern = @"^\+?[1-9]\d{6,14}$";

    public const int MaxLength = 32;
}
