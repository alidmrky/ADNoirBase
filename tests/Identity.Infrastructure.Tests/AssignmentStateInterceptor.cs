using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Identity.Infrastructure.Tests;

internal sealed class AssignmentStateInterceptor : SaveChangesInterceptor
{
    public EntityState? AssignmentState { get; private set; }
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        AssignmentState = eventData.Context!.ChangeTracker.Entries<UserRole>().Single().State;
        return ValueTask.FromResult(InterceptionResult<int>.SuppressWithResult(1));
    }
}
