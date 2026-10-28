namespace Notification.Application.Dtos.Templates;

public sealed record TemplateVariableDto(
    string Name,
    string? Description,
    string? SampleValue);
