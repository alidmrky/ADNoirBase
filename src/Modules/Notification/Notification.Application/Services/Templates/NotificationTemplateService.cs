using ADNoirBase.Shared.Application.Interfaces;
using ADNoirBase.Shared.Contracts.Pagination;
using ADNoirBase.Shared.Contracts.Responses;
using Microsoft.Extensions.Options;
using Notification.Application.Configuration;
using Notification.Application.Dispatching;
using Notification.Application.Dtos.Templates;
using Notification.Application.Messages;
using Notification.Application.Templating;
using Notification.Domain.Entities;
using Notification.Domain.Enums;
using Notification.Domain.Interfaces.Repositories;
using Notification.Domain.ValueObjects;

namespace Notification.Application.Services.Templates;

public sealed class NotificationTemplateService : INotificationTemplateService
{
    private readonly INotificationTemplateRepository _templateRepository;
    private readonly INotificationComposer _composer;
    private readonly INotificationDispatcher _dispatcher;
    private readonly ICurrentUserService _currentUser;
    private readonly IUserDirectory _userDirectory;
    private readonly NotificationSettings _settings;

    public NotificationTemplateService(
        INotificationTemplateRepository templateRepository,
        INotificationComposer composer,
        INotificationDispatcher dispatcher,
        ICurrentUserService currentUser,
        IUserDirectory userDirectory,
        IOptions<NotificationSettings> settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        _templateRepository = templateRepository;
        _composer = composer;
        _dispatcher = dispatcher;
        _currentUser = currentUser;
        _userDirectory = userDirectory;
        _settings = settings.Value;
    }

    public async Task<BaseResponse<PagedList<TemplateListDto>>> ListAsync(
        TemplateQuery query, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var paged = await _templateRepository.SearchAsync(
            query.Page, query.PageSize, query.Channel, query.IsActive, query.Search, ct);

        var items = paged.Items.Select(ToListDto).ToList();
        var result = new PagedList<TemplateListDto>(items, paged.TotalCount, paged.Page, paged.PageSize);

        return BaseResponse<PagedList<TemplateListDto>>.Success(result);
    }

    public async Task<BaseResponse<TemplateDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var template = await _templateRepository.FindByIdAsync(id, ct);

        return template is null
            ? BaseResponse<TemplateDto>.NotFound(NotificationMessageCodes.Template.NotFound)
            : BaseResponse<TemplateDto>.Success(ToDto(template));
    }

    public async Task<BaseResponse<TemplateDto>> CreateAsync(
        CreateTemplateRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var code = NotificationTemplate.NormalizeCode(request.Code);

        if (await _templateRepository.CodeExistsAsync(code, null, ct))
        {
            return BaseResponse<TemplateDto>.Conflict(NotificationMessageCodes.Template.CodeConflict);
        }

        if (!request.IsLayout && !HasRequiredContent(
                request.Channel, request.Subject, request.HtmlBody, request.TextBody, request.SmsBody))
        {
            return BaseResponse<TemplateDto>.BusinessRuleViolation(
                NotificationMessageCodes.Template.ContentMissing);
        }

        var template = NotificationTemplate.Create(
            code, request.Name, request.Channel, request.Category);

        if (request.IsLayout)
        {
            template.MarkAsLayout();
        }

        ApplyEditableFields(
            template,
            request.Name,
            request.Description,
            request.Category,
            isActive: true,
            request.LayoutCode,
            request.Subject,
            request.HtmlBody,
            request.TextBody,
            request.SmsBody,
            request.Variables,
            request.LogPolicy,
            request.RetentionDays);

        await _templateRepository.InsertAsync(template, ct);

        return BaseResponse<TemplateDto>.Created(
            ToDto(template), NotificationMessageCodes.Template.Created);
    }

    public async Task<BaseResponse> UpdateAsync(
        Guid id, UpdateTemplateRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var template = await _templateRepository.FindByIdAsync(id, ct);

        if (template is null)
        {
            return BaseResponse.NotFound(NotificationMessageCodes.Template.NotFound);
        }

        // Eksik içerik taslak olarak kaydedilebilir, ama aktif bir şablon gönderim anında
        // patlamamalı; bu yüzden kontrol yalnızca aktifleştirmede uygulanır.
        if (request.IsActive && !template.IsLayout && !HasRequiredContent(
                template.Channel, request.Subject, request.HtmlBody, request.TextBody, request.SmsBody))
        {
            return BaseResponse.BusinessRuleViolation(NotificationMessageCodes.Template.ContentMissing);
        }

        ApplyEditableFields(
            template,
            request.Name,
            request.Description,
            request.Category,
            request.IsActive,
            request.LayoutCode,
            request.Subject,
            request.HtmlBody,
            request.TextBody,
            request.SmsBody,
            request.Variables,
            request.LogPolicy,
            request.RetentionDays);

        await _templateRepository.UpdateAsync(template, ct);

        return BaseResponse.Success(NotificationMessageCodes.Template.Updated);
    }

    public async Task<BaseResponse> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var template = await _templateRepository.FindByIdAsync(id, ct);

        if (template is null)
        {
            return BaseResponse.NotFound(NotificationMessageCodes.Template.NotFound);
        }

        // Sistem şablonlarına koddan referans verilir; silinirse ilgili gönderim sessizce durur.
        if (template.IsSystem)
        {
            return BaseResponse.BusinessRuleViolation(NotificationMessageCodes.Template.SystemImmutable);
        }

        await _templateRepository.DeleteAsync(template, ct);

        return BaseResponse.Success(NotificationMessageCodes.Template.Deleted);
    }

    public async Task<BaseResponse<TemplatePreviewResponse>> PreviewAsync(
        TemplatePreviewRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Önizleme kaydedilmemiş içerik üzerinde çalışır; bu yüzden kalıcı olmayan
        // bir şablon nesnesi kurulur ve veritabanına hiç dokunulmaz.
        var draft = NotificationTemplate.Create(
            "preview", "preview", NotificationChannel.Email, NotificationCategory.Transactional);

        draft.UpdateDefinition("preview", null, NotificationCategory.Transactional, request.LayoutCode, true);
        draft.UpdateContent(request.Subject, request.HtmlBody, request.TextBody, request.SmsBody);

        var model = BuildSampleModel(request.SampleValues);
        var composed = await _composer.ComposeAsync(draft, model, ct);

        var response = new TemplatePreviewResponse(
            composed.Subject, composed.HtmlBody, composed.TextBody, composed.SmsBody, composed.Errors);

        // Render hataları önizlemenin çıktısıdır, isteğin başarısızlığı değildir;
        // editör bunları satır satır gösterebilsin diye 200 ile döner.
        return BaseResponse<TemplatePreviewResponse>.Success(response);
    }

    public async Task<BaseResponse> TestSendAsync(
        Guid id, TemplateTestSendRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var template = await _templateRepository.FindByIdAsync(id, ct);

        if (template is null)
        {
            return BaseResponse.NotFound(NotificationMessageCodes.Template.NotFound);
        }

        if (template.IsLayout)
        {
            return BaseResponse.BusinessRuleViolation(NotificationMessageCodes.Template.ChannelMismatch);
        }

        if (!Guid.TryParse(_currentUser.UserId, out var userId))
        {
            return BaseResponse.Unauthorized();
        }

        var user = await _userDirectory.FindByIdAsync(userId, ct);

        if (user is null)
        {
            return BaseResponse.Unauthorized();
        }

        var address = template.Channel == NotificationChannel.Sms ? user.PhoneNumber : user.Email;

        if (string.IsNullOrWhiteSpace(address))
        {
            return BaseResponse.BusinessRuleViolation(
                NotificationMessageCodes.Message.RecipientRequired);
        }

        var model = BuildSampleModel(request.SampleValues);
        TemplateModelBuilder.ApplyUser(model, user);

        var result = await _dispatcher.EnqueueAsync(
            new NotificationRequest
            {
                Channel = template.Channel,
                Category = template.Category,
                To = [new NotificationRecipient(address, user.FullName, user.UserId)],
                TemplateCode = template.Code,
                Model = model,
                Priority = NotificationPriority.High,
                // Test gönderimi kullanıcının kendi isteğidir; kendi tercihleri onu engellememeli.
                BypassPreferences = true
            },
            ct);

        return result.IsSuccess
            ? BaseResponse.Success(NotificationMessageCodes.Message.Queued)
            : BaseResponse.BusinessRuleViolation(NotificationMessageCodes.Template.RenderFailed);
    }

    private Dictionary<string, object?> BuildSampleModel(
        IReadOnlyDictionary<string, string>? sampleValues)
    {
        var model = TemplateModelBuilder.BuildBase(_settings);

        if (sampleValues is not null)
        {
            foreach (var (key, value) in sampleValues)
            {
                model[key] = value;
            }
        }

        return model;
    }

    private static void ApplyEditableFields(
        NotificationTemplate template,
        string name,
        string? description,
        NotificationCategory category,
        bool isActive,
        string? layoutCode,
        string? subject,
        string? htmlBody,
        string? textBody,
        string? smsBody,
        IReadOnlyList<TemplateVariableDto>? variables,
        NotificationLogPolicy logPolicy,
        int retentionDays)
    {
        template.UpdateDefinition(name, description, category, layoutCode, isActive);
        template.UpdateContent(subject, htmlBody, textBody, smsBody);
        template.UpdateRetention(logPolicy, retentionDays);
        template.UpdateVariables(
            (variables ?? []).Select(v => new TemplateVariable(v.Name, v.Description, v.SampleValue)));
    }

    /// <summary>Kanalın gönderim için ihtiyaç duyduğu alanlar dolu mu.</summary>
    private static bool HasRequiredContent(
        NotificationChannel channel,
        string? subject,
        string? htmlBody,
        string? textBody,
        string? smsBody) =>
        channel switch
        {
            NotificationChannel.Email =>
                !string.IsNullOrWhiteSpace(subject) &&
                (!string.IsNullOrWhiteSpace(htmlBody) || !string.IsNullOrWhiteSpace(textBody)),
            NotificationChannel.Sms => !string.IsNullOrWhiteSpace(smsBody),
            NotificationChannel.InApp =>
                !string.IsNullOrWhiteSpace(subject) && !string.IsNullOrWhiteSpace(textBody),
            _ => false
        };

    private static TemplateListDto ToListDto(NotificationTemplate t) => new(
        t.Id, t.Code, t.Name, t.Channel, t.Category,
        t.IsSystem, t.IsLayout, t.IsActive, t.CreatedAt, t.UpdatedAt);

    private static TemplateDto ToDto(NotificationTemplate t) => new(
        t.Id, t.Code, t.Name, t.Description, t.Channel, t.Category,
        t.IsSystem, t.IsLayout, t.IsActive, t.LayoutCode,
        t.Subject, t.HtmlBody, t.TextBody, t.SmsBody,
        [.. t.Variables.Select(v => new TemplateVariableDto(v.Name, v.Description, v.SampleValue))],
        t.LogPolicy, t.RetentionDays, t.CreatedAt, t.UpdatedAt);
}
