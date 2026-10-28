using Identity.Domain.Entities;
using Identity.Infrastructure.Persistence;
using Identity.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Tests;

public sealed class UserRolePersistenceTests
{
    [Fact]
    public void UpdatingTheUserGraphMarksANewGuidAssignmentAsModified()
    {
        using var context = new IdentityDbContext(new DbContextOptionsBuilder<IdentityDbContext>()
            .UseNpgsql("Host=localhost;Database=unused").Options);
        var user = User.Create("test@example.invalid", "unused", "Test", "User");
        context.Attach(user);
        var assignment = UserRole.Create(user.Id, Guid.NewGuid());
        user.UserRoles.Add(assignment);
        context.Users.Update(user);
        context.ChangeTracker.DetectChanges();
        Assert.Equal(EntityState.Modified, context.Entry(assignment).State);
    }

    [Fact]
    public async Task AssignmentRepositoryExplicitlyInsertsTheNewRelationship()
    {
        var interceptor = new AssignmentStateInterceptor();
        await using var context = new IdentityDbContext(new DbContextOptionsBuilder<IdentityDbContext>()
            .UseNpgsql("Host=localhost;Database=unused").AddInterceptors(interceptor).Options);
        var assignment = UserRole.Create(Guid.NewGuid(), Guid.NewGuid());
        var result = await new UserRoleRepository(context).TryAssignAsync(assignment);
        Assert.True(result);
        Assert.Equal(EntityState.Added, interceptor.AssignmentState);
    }
}
