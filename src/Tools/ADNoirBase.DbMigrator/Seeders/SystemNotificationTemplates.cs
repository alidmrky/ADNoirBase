using Notification.Domain.Entities;
using Notification.Domain.Enums;
using Notification.Domain.ValueObjects;

namespace ADNoirBase.DbMigrator.Seeders;

/// <summary>
/// Kodun referans verdiği şablonların ilk hali. Yalnızca eksikse eklenir;
/// yöneticinin UI'dan yaptığı düzenlemeler migrator tarafından ezilmez.
/// </summary>
public static class SystemNotificationTemplates
{
    public const string DefaultLayoutCode = "layout.default";
    public const string WelcomeCode = "identity.welcome";
    public const string PasswordChangedCode = "identity.password_changed";
    public const string SmsVerificationCode = "sms.verification_code";
    public const string AnnouncementCode = "marketing.announcement";

    public static IReadOnlyList<NotificationTemplate> Build() =>
    [
        BuildDefaultLayout(),
        BuildWelcome(),
        BuildPasswordChanged(),
        BuildSmsVerification(),
        BuildAnnouncement()
    ];

    private static NotificationTemplate BuildDefaultLayout()
    {
        var template = NotificationTemplate.Create(
            DefaultLayoutCode,
            "Default e-mail layout",
            NotificationChannel.Email,
            NotificationCategory.System,
            isSystem: true);

        template.MarkAsLayout();
        template.UpdateDefinition(
            "Default e-mail layout",
            "Tüm e-postaları saran ortak başlık ve altlık.",
            NotificationCategory.System,
            layoutCode: null,
            isActive: true);

        template.UpdateContent(
            subject: null,
            htmlBody: """
                <!DOCTYPE html>
                <html>
                  <head><meta charset="utf-8"><title>{{ subject }}</title></head>
                  <body style="margin:0;padding:24px;background:#f4f4f5;font-family:Arial,Helvetica,sans-serif;color:#18181b;">
                    <table role="presentation" width="100%" cellpadding="0" cellspacing="0">
                      <tr><td align="center">
                        <table role="presentation" width="600" cellpadding="0" cellspacing="0" style="background:#ffffff;border-radius:8px;padding:32px;">
                          <tr><td style="font-size:20px;font-weight:bold;padding-bottom:16px;">{{ app_name }}</td></tr>
                          <tr><td style="font-size:15px;line-height:1.6;">{{ content }}</td></tr>
                          <tr><td style="padding-top:24px;font-size:12px;color:#71717a;">
                            &copy; {{ year }} {{ app_name }}
                            {{ if unsubscribe_url }}
                              &middot; <a href="{{ unsubscribe_url }}" style="color:#71717a;">Abonelikten çık</a>
                            {{ end }}
                          </td></tr>
                        </table>
                      </td></tr>
                    </table>
                  </body>
                </html>
                """,
            textBody: null,
            smsBody: null);

        template.UpdateRetention(NotificationLogPolicy.MetadataOnly, retentionDays: 0);
        return template;
    }

    private static NotificationTemplate BuildWelcome()
    {
        var template = NotificationTemplate.Create(
            WelcomeCode,
            "Hoş geldiniz",
            NotificationChannel.Email,
            NotificationCategory.Transactional,
            isSystem: true);

        template.UpdateDefinition(
            "Hoş geldiniz",
            "Kullanıcı kayıt olduğunda otomatik gönderilir.",
            NotificationCategory.Transactional,
            DefaultLayoutCode,
            isActive: true);

        template.UpdateContent(
            subject: "{{ app_name }} hesabınız hazır",
            htmlBody: """
                <p>Merhaba {{ first_name }},</p>
                <p>{{ app_name }} hesabınız oluşturuldu. Aşağıdaki bağlantıdan giriş yapabilirsiniz.</p>
                <p><a href="{{ app_url }}">{{ app_url }}</a></p>
                """,
            textBody: """
                Merhaba {{ first_name }},

                {{ app_name }} hesabınız oluşturuldu. Giriş yapmak için: {{ app_url }}
                """,
            smsBody: null);

        template.UpdateVariables(
        [
            new TemplateVariable("first_name", "Kullanıcının adı", "Ayşe"),
            new TemplateVariable("app_url", "Uygulamanın adresi", "https://localhost:3000")
        ]);

        template.UpdateRetention(NotificationLogPolicy.MetadataOnly, retentionDays: 90);
        return template;
    }

    private static NotificationTemplate BuildPasswordChanged()
    {
        var template = NotificationTemplate.Create(
            PasswordChangedCode,
            "Parolanız değişti",
            NotificationChannel.Email,
            NotificationCategory.Transactional,
            isSystem: true);

        template.UpdateDefinition(
            "Parolanız değişti",
            "Parola değiştiğinde güvenlik uyarısı olarak gönderilir.",
            NotificationCategory.Transactional,
            DefaultLayoutCode,
            isActive: true);

        template.UpdateContent(
            subject: "Parolanız değiştirildi",
            htmlBody: """
                <p>Merhaba {{ first_name }},</p>
                <p>Hesabınızın parolası {{ changed_at }} tarihinde değiştirildi.</p>
                <p>Bu işlemi siz yapmadıysanız hemen parolanızı sıfırlayın ve bizimle iletişime geçin.</p>
                """,
            textBody: """
                Merhaba {{ first_name }},

                Hesabınızın parolası {{ changed_at }} tarihinde değiştirildi.
                Bu işlemi siz yapmadıysanız hemen parolanızı sıfırlayın.
                """,
            smsBody: null);

        template.UpdateVariables(
        [
            new TemplateVariable("first_name", "Kullanıcının adı", "Ayşe"),
            new TemplateVariable("changed_at", "Değişiklik zamanı", "27.10.2026 18:30")
        ]);

        // Güvenlik uyarısının içeriği hassas değil ama kaydı uzun tutmanın da faydası yok.
        template.UpdateRetention(NotificationLogPolicy.MetadataOnly, retentionDays: 180);
        return template;
    }

    private static NotificationTemplate BuildSmsVerification()
    {
        var template = NotificationTemplate.Create(
            SmsVerificationCode,
            "SMS doğrulama kodu",
            NotificationChannel.Sms,
            NotificationCategory.Transactional,
            isSystem: true);

        template.UpdateDefinition(
            "SMS doğrulama kodu",
            "SMS şablonu örneği.",
            NotificationCategory.Transactional,
            layoutCode: null,
            isActive: true);

        template.UpdateContent(
            subject: null,
            htmlBody: null,
            textBody: null,
            smsBody: "{{ app_name }} doğrulama kodunuz: {{ code }}. Kod {{ expires_in_minutes }} dakika geçerlidir.");

        template.UpdateVariables(
        [
            new TemplateVariable("code", "Tek kullanımlık kod", "483921"),
            new TemplateVariable("expires_in_minutes", "Geçerlilik süresi", "5")
        ]);

        // Doğrulama kodu kişisel veridir ve gönderildikten sonra saklanmasının bir değeri yoktur.
        template.UpdateRetention(NotificationLogPolicy.None, retentionDays: 0);
        return template;
    }

    private static NotificationTemplate BuildAnnouncement()
    {
        var template = NotificationTemplate.Create(
            AnnouncementCode,
            "Duyuru",
            NotificationChannel.Email,
            NotificationCategory.Marketing,
            isSystem: true);

        template.UpdateDefinition(
            "Duyuru",
            "Toplu gönderim için başlangıç şablonu. Abonelikten çıkma linki layout'tan gelir.",
            NotificationCategory.Marketing,
            DefaultLayoutCode,
            isActive: true);

        template.UpdateContent(
            subject: "{{ title }}",
            htmlBody: """
                <p>Merhaba {{ first_name }},</p>
                <div>{{ body }}</div>
                """,
            textBody: """
                Merhaba {{ first_name }},

                {{ body }}
                """,
            smsBody: null);

        template.UpdateVariables(
        [
            new TemplateVariable("title", "Duyuru başlığı", "Yeni sürüm yayında"),
            new TemplateVariable("body", "Duyuru metni", "Bu sürümde bildirim merkezi eklendi.")
        ]);

        template.UpdateRetention(NotificationLogPolicy.MetadataOnly, retentionDays: 30);
        return template;
    }
}
