using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenHabitTracker.App;
using OpenHabitTracker.Blazor.Web.Data;
using OpenHabitTracker.Data.Entities;
using System.Security.Cryptography;
using System.Text.Json;

namespace OpenHabitTracker.Blazor.Web.Controllers;

[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[ApiController]
[IgnoreAntiforgeryToken]
[Route("api/habit-completions")]
public class HabitCompletionsController(IDbContextFactory<ApplicationDbContext> factory, ICurrentUser currentUser) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<long>> Record(HabitCompletion completion, CancellationToken cancellationToken)
    {
        long userId = await currentUser.GetUserId();
        if (userId == 0 || completion.UserId != userId)
            return Forbid();

        if (completion.OperationId == Guid.Empty || completion.HabitId <= 0 || completion.Quantity <= 0 ||
            completion.StartedAt == default || completion.CompletedAt < completion.StartedAt)
            return BadRequest();

        await using ApplicationDbContext context = await factory.CreateDbContextAsync(cancellationToken);
        context.CurrentUserId = userId;

        // SQLite's write transaction serializes a concurrent retry with the original request.
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        string hash = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new
        {
            completion.UserId, completion.HabitId, completion.Quantity,
            StartedAt = completion.StartedAt.ToUniversalTime(),
            CompletedAt = completion.CompletedAt.ToUniversalTime()
        })));
        HabitCompletionReceipt? receipt = await context.HabitCompletionReceipts
            .SingleOrDefaultAsync(x => x.OperationId == completion.OperationId, cancellationToken);
        if (receipt is not null)
            return receipt.ContentHash == hash ? Ok(receipt.TimeId) : Conflict();

        HabitEntity? habit = await context.Habits.SingleOrDefaultAsync(x => x.Id == completion.HabitId, cancellationToken);
        if (habit is null || habit.IsDeleted)
            return NotFound();

        TimeEntity time = new()
        {
            UserId = userId,
            HabitId = habit.Id,
            StartedAt = completion.StartedAt,
            CompletedAt = completion.CompletedAt,
            Quantity = completion.Quantity
        };
        context.Times.Add(time);
        if (habit.LastTimeDoneAt is null || habit.LastTimeDoneAt < time.CompletedAt)
            habit.LastTimeDoneAt = time.CompletedAt;
        await context.SaveChangesAsync(cancellationToken);

        context.HabitCompletionReceipts.Add(new()
        {
            UserId = userId,
            OperationId = completion.OperationId,
            TimeId = time.Id,
            ContentHash = hash
        });
        await context.SaveChangesAsync(cancellationToken);
        await context.Users.Where(x => x.Id == userId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.LastChangeAt, DateTime.UtcNow), cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Ok(time.Id);
    }
}
