using ADNoirBase.Shared.Application.Interfaces;
using ADNoirBase.Shared.Contracts.Responses;
using Identity.Application.Dtos.Role;
using Identity.Application.Messages;
using Identity.Application.Services.Role;
using Identity.Application.Services.User;
using Identity.Domain.Entities;
using Identity.Domain.Interfaces.Repositories;

namespace Identity.Application.Tests;

public sealed class RoleManagementTests
{
    [Theory]
    [InlineData(true, ResponseStatus.Success)]
    [InlineData(false, ResponseStatus.Conflict)]
    public async Task AssignRoleUsesTheRelationshipRepositoryWithoutUpdatingTheUserGraph(bool inserted, ResponseStatus expected)
    {
        var user = User.Create("test@example.invalid", "unused", "Test", "User");
        var role = Role.Create("Editor", "EDITOR");
        var users = InterfaceStub.For<IUserRepository>((method, _) =>
        {
            Assert.Equal("FindWithRolesAsync", method);
            return Task.FromResult<User?>(user);
        });
        var roles = InterfaceStub.For<IRoleRepository>((_, _) => Task.FromResult<Role?>(role));
        var assignments = InterfaceStub.For<IUserRoleRepository>((method, args) =>
        {
            Assert.Equal("TryAssignAsync", method);
            var assignment = Assert.IsType<UserRole>(args![0]);
            Assert.Equal(user.Id, assignment.UserId);
            Assert.Equal(role.Id, assignment.RoleId);
            return Task.FromResult(inserted);
        });
        var service = new UserManagementService(users, roles,
            InterfaceStub.For<IPasswordHasher>((_, _) => throw new InvalidOperationException()), assignments,
            InterfaceStub.For<ICurrentUserService>((_, _) => throw new InvalidOperationException()));
        Assert.Equal(expected, (await service.AssignRoleAsync(user.Id, role.Id)).Status);
    }

    [Theory]
    [InlineData("", "VALID")]
    [InlineData("Role", "1INVALID")]
    [InlineData("Role", "BAD CODE")]
    public async Task InvalidRolesNeverReachPersistence(string name, string code)
    {
        var service = new RoleManagementService(InterfaceStub.For<IRoleRepository>((_, _) => throw new InvalidOperationException()),
            InterfaceStub.For<IUserRoleRepository>((_, _) => throw new InvalidOperationException()));
        var result = await service.CreateAsync(new CreateRoleRequest(name, code));
        Assert.Equal(IdentityMessageCodes.Role.Invalid, result.MessageCode);
    }

    [Fact]
    public async Task RoleCodeIsNormalizedAndConcurrentDuplicateReturnsConflict()
    {
        Role? received = null;
        var roles = InterfaceStub.For<IRoleRepository>((method, args) =>
        {
            Assert.Equal("TryInsertAsync", method);
            received = (Role)args![0]!;
            return Task.FromResult(false);
        });
        var service = new RoleManagementService(roles, InterfaceStub.For<IUserRoleRepository>((_, _) => throw new InvalidOperationException()));
        var result = await service.CreateAsync(new CreateRoleRequest("Editor", " editor "));
        Assert.Equal("EDITOR", received!.Code);
        Assert.Equal(ResponseStatus.Conflict, result.Status);
    }

    [Fact]
    public async Task SystemRoleCannotBeDeletedOrEdited()
    {
        var role = Role.Create("Admin", "ADMIN", isSystem: true);
        var roles = InterfaceStub.For<IRoleRepository>((method, _) =>
        {
            Assert.Equal("FindByIdAsync", method);
            return Task.FromResult<Role?>(role);
        });
        var service = new RoleManagementService(roles, InterfaceStub.For<IUserRoleRepository>((_, _) => throw new InvalidOperationException()));
        Assert.Equal(IdentityMessageCodes.Role.SystemRoleImmutable, (await service.DeleteAsync(role.Id)).MessageCode);
        Assert.Equal(IdentityMessageCodes.Role.SystemRoleImmutable, (await service.UpdateAsync(role.Id, new UpdateRoleRequest("Changed"))).MessageCode);
    }

    [Fact]
    public async Task AdministratorCannotRemoveOwnAdminRole()
    {
        var user = User.Create("test@example.invalid", "unused", "Test", "User");
        var role = Role.Create("Admin", "ADMIN", isSystem: true);
        var users = InterfaceStub.For<IUserRepository>((method, _) =>
        {
            Assert.Equal("FindWithRolesAsync", method);
            return Task.FromResult<User?>(user);
        });
        var roles = InterfaceStub.For<IRoleRepository>((_, _) => Task.FromResult<Role?>(role));
        var current = InterfaceStub.For<ICurrentUserService>((_, _) => user.Id.ToString());
        var service = new UserManagementService(users, roles,
            InterfaceStub.For<IPasswordHasher>((_, _) => throw new InvalidOperationException()),
            InterfaceStub.For<IUserRoleRepository>((_, _) => throw new InvalidOperationException()), current);
        var result = await service.RemoveRoleAsync(user.Id, role.Id);
        Assert.Equal(IdentityMessageCodes.Role.CannotRemoveOwnAdmin, result.MessageCode);
    }
}
