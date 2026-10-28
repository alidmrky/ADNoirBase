namespace Notification.Application.Dtos.Preferences;

/// <summary>Mail içindeki linkten gelir; kimlik doğrulaması gerektirmez.</summary>
public sealed record UnsubscribeRequest(string Token);
