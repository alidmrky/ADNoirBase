namespace ADNoirBase.Shared.Application.Interfaces;

public static class CurrentUserServiceExtensions
{
    public const string SystemUser = "system";

    /// <summary>
    /// CreatedBy/UpdatedBy/DeletedBy için yazılacak değer. Arka plan işleri ve anonim
    /// istekler (login, register) kullanıcı taşımadığı için "system" olarak kaydedilir.
    /// </summary>
    public static string GetAuditUserId(this ICurrentUserService currentUserService) =>
        currentUserService.IsAuthenticated
            ? currentUserService.UserId ?? SystemUser
            : SystemUser;
}
