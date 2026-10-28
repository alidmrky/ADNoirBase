using Microsoft.Extensions.Options;
using Notification.Application.Configuration;
using Notification.Application.Messages;
using Notification.Application.Services.Attachments;
using Notification.Domain.ValueObjects;

namespace Notification.Application.Tests;

public sealed class AttachmentServiceTests
{
    private static NotificationAttachmentService Create(AttachmentStoreFake store) => new(store,
        Options.Create(new NotificationSettings { Attachments = new AttachmentSettings { MaxBytes = 4, MaxCount = 2 } }));

    [Fact]
    public async Task RejectsActualStreamSizeBeforePersisting()
    {
        var store = new AttachmentStoreFake();
        using var input = new MemoryStream(new byte[5]);
        var result = await Create(store).UploadAsync("test.txt", "text/plain", input);
        Assert.Equal(NotificationMessageCodes.Attachment.TooLarge, result.MessageCode);
        Assert.Equal(0, store.Uploads);
    }

    [Fact]
    public async Task AcceptsExactLimitAndStripsPathAndHeaderNewlines()
    {
        var store = new AttachmentStoreFake();
        using var input = new MemoryStream(new byte[4]);
        var result = await Create(store).UploadAsync("C:\\private\\test\r\n.txt", "text/plain", input);
        Assert.True(result.IsSuccess);
        Assert.Equal("test.txt", result.Data!.FileName);
        Assert.Equal(4, result.Data.Length);
    }

    [Fact]
    public async Task RejectsDisallowedTypeBeforePersisting()
    {
        var store = new AttachmentStoreFake();
        using var input = new MemoryStream(new byte[1]);
        var result = await Create(store).UploadAsync("test.exe", "application/x-msdownload", input);
        Assert.Equal(NotificationMessageCodes.Attachment.TypeNotAllowed, result.MessageCode);
        Assert.Equal(0, store.Uploads);
    }

    [Fact]
    public async Task EnforcesCountEvenForDuplicateIds()
    {
        var result = await Create(new AttachmentStoreFake()).ResolveAsync(["id", "id", "id"]);
        Assert.Equal(NotificationMessageCodes.Attachment.TooMany, result.MessageCode);
    }

    [Fact]
    public async Task RejectsMissingStoredReference()
    {
        var result = await Create(new AttachmentStoreFake()).ResolveAsync(["missing"]);
        Assert.Equal(NotificationMessageCodes.Attachment.NotFound, result.MessageCode);
    }

    [Fact]
    public async Task RevalidatesPreviouslyUploadedFilesAgainstCurrentLimits()
    {
        var store = new AttachmentStoreFake { Stored = new NotificationAttachmentRef("id", "old.txt", "text/plain", 5) };
        var result = await Create(store).ResolveAsync(["id"]);
        Assert.Equal(NotificationMessageCodes.Attachment.TooLarge, result.MessageCode);
    }
}
