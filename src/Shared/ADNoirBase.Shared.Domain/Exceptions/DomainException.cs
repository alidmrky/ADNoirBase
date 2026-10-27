namespace ADNoirBase.Shared.Domain.Exceptions;

/// <summary>
/// Domain katmanında fırlatılabilen tek exception türü.
/// Servis katmanı bu exception'ı yakalar ve BaseResponse.BusinessRuleViolation'a dönüştürür.
/// </summary>
public sealed class DomainException : Exception
{
    public string MessageCode { get; }

    public DomainException(string messageCode, string? detail = null)
        : base(detail ?? messageCode)
    {
        MessageCode = messageCode;
    }
}
