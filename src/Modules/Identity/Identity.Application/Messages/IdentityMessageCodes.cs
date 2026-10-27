namespace Identity.Application.Messages;

/// <summary>Identity modülüne ait tüm mesaj kodları. Literal string kullanımı yasaktır.</summary>
public static class IdentityMessageCodes
{
    public static class User
    {
        public const string NotFound              = "identity.user.not_found";
        public const string EmailConflict         = "identity.user.email_conflict";
        public const string Created               = "identity.user.created";
        public const string Updated               = "identity.user.updated";
        public const string Deactivated           = "identity.user.deactivated";
        public const string Activated             = "identity.user.activated";
        public const string PasswordChanged       = "identity.user.password_changed";
    }

    public static class Auth
    {
        public const string CredentialsInvalid    = "identity.credentials_invalid";
        public const string AccountLocked         = "identity.account_locked";
        public const string AccountInactive       = "identity.account_inactive";
        public const string LoginSuccess          = "identity.login_success";
        public const string LogoutSuccess         = "identity.logout_success";
        public const string RegisterSuccess       = "identity.register_success";
        public const string RefreshTokenInvalid   = "identity.refresh_token.invalid";
        public const string RefreshTokenExpired   = "identity.refresh_token.expired";
        public const string RefreshSuccess        = "identity.refresh_success";
    }

    public static class Role
    {
        public const string NotFound              = "identity.role.not_found";
        public const string CodeConflict          = "identity.role.code_conflict";
        public const string SystemRoleImmutable   = "identity.role.system_immutable";
        public const string Assigned              = "identity.role.assigned";
        public const string Removed               = "identity.role.removed";
        public const string AlreadyAssigned       = "identity.role.already_assigned";
        public const string NotAssigned           = "identity.role.not_assigned";
    }
}
