using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using queue_backend.Data;
using queue_backend.Models;

namespace queue_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QueueController : ControllerBase
{
    private readonly AppDbContext _context;

    public QueueController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllQueues()
    {
        var queues = await _context.Queues
            .AsQueryable()
            .AsNoTracking()
            .Where(q => q.Valid)
            .ToListAsync();
        return Ok(queues);
    }
    [HttpGet("counter")]
    public async Task<IActionResult> GetAllCounter()
    {
        var counters = await _context.QueueCounters
            .AsQueryable()
            .AsNoTracking()
            .Where(q => q.Valid)
            .ToListAsync();

        if (counters.Count == 0)
        {
            counters.Add(new QueueCounter
            {
                ID = 1,
                CurrentPrefix = "A",
                CurrentNumber = 0,
                Valid = true
            });
        }

        return Ok(counters.Select(counter => new
        {
            id = counter.ID,
            currentPrefix = counter.CurrentPrefix,
            currentNumber = counter.CurrentNumber,
            currentQueueNumber = counter.CurrentQueueNumber,
            valid = counter.Valid,
            createdAt = counter.CreatedAt,
            updatedAt = counter.UpdatedAt
        }));
    }

    [HttpGet("status/{status}")]
    public async Task<IActionResult> GetQueuesByStatus(string status)
    {
        if (!QueueStatus.IsValid(status))
        {
            return BadRequest(new
            {
                message = "Invalid queue status.",
                allowedStatuses = QueueStatus.All
            });
        }

        var queues = await _context.Queues
            .AsNoTracking()
            .Where(q => q.Valid && q.Status == status.ToLower())
            .ToListAsync();

        return Ok(queues);
    }

    [HttpPost("create")]
    public async Task<IActionResult> CreateQueueAsync()
    {
        const int maxRetries = 3;

        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

            try
            {
                var counter = await _context.QueueCounters
                    .SingleOrDefaultAsync(q => q.ID == 1);

                if (counter is null)
                {
                    counter = new QueueCounter
                    {
                        ID = 1,
                        CurrentPrefix = "A",
                        CurrentNumber = 0
                    };

                    _context.QueueCounters.Add(counter);
                }

                if (counter.CurrentNumber < 9)
                {
                    counter.CurrentNumber++;
                }
                else
                {
                    counter.CurrentNumber = 0;
                    counter.CurrentPrefix = counter.CurrentPrefix == "Z"
                        ? "A"
                        : ((char)(counter.CurrentPrefix[0] + 1)).ToString();
                }

                var queue = new QueueItem
                {
                    QueueNumber = $"{counter.CurrentPrefix}{counter.CurrentNumber}",
                    Prefix = counter.CurrentPrefix,
                    RunningNumber = counter.CurrentNumber,
                    Status = QueueStatus.Waiting,
                    CreatedAt = DateTime.UtcNow.AddHours(7)
                };

                _context.Queues.Add(queue);
                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                return Ok(queue);
            }
            catch (DbUpdateException) when (attempt < maxRetries)
            {
                await tx.RollbackAsync();
                _context.ChangeTracker.Clear();
            }
            catch (InvalidOperationException) when (attempt < maxRetries)
            {
                await tx.RollbackAsync();
                _context.ChangeTracker.Clear();
            }
        }

        return StatusCode(StatusCodes.Status409Conflict, new
        {
            message = "Could not generate the next queue number due to concurrent updates."
        });
    }

    [HttpPost("reset")]
    public async Task<IActionResult> ResetQueuesAsync()
    {
        await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        var counter = await _context.QueueCounters
            .SingleOrDefaultAsync(q => q.ID == 1);

        if (counter is null)
        {
            counter = new QueueCounter
            {
                ID = 1,
                CurrentPrefix = "A",
                CurrentNumber = 0
            };

            _context.QueueCounters.Add(counter);
        }
        else
        {
            counter.CurrentPrefix = "A";
            counter.CurrentNumber = 0;
        }

        var activeQueues = await _context.Queues
            .Where(q => q.Valid)
            .ToListAsync();

        foreach (var queue in activeQueues)
        {
            queue.Valid = false;
        }

        await _context.SaveChangesAsync();
        await tx.CommitAsync();

        return Ok(new
        {
            message = "Queue system reset successfully.",
            counter = new
            {
                prefix = counter.CurrentPrefix,
                number = counter.CurrentNumber,
                currentQueueNumber = counter.CurrentQueueNumber
            },
            archivedQueues = activeQueues.Count
        });
    }

    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> UpdateQueueStatus(int id, [FromBody] UpdateQueueStatusRequest request)
    {
        if (!QueueStatus.IsValid(request.Status))
        {
            return BadRequest(new
            {
                message = "Invalid queue status.",
                allowedStatuses = QueueStatus.All
            });
        }

        var queue = await _context.Queues
            .SingleOrDefaultAsync(q => q.ID == id && q.Valid);

        if (queue is null)
        {
            return NotFound(new { message = "Queue not found." });
        }

        queue.Status = request.Status.ToLower();
        await _context.SaveChangesAsync();

        return Ok(queue);
    }

    [HttpPatch("{id:int}/call")]
    public Task<IActionResult> CallQueue(int id)
    {
        return UpdateQueueStatusInternal(id, QueueStatus.Called);
    }

    [HttpPatch("{id:int}/done")]
    public Task<IActionResult> CompleteQueue(int id)
    {
        return UpdateQueueStatusInternal(id, QueueStatus.Done);
    }

    [HttpPatch("{id:int}/cancel")]
    public Task<IActionResult> CancelQueue(int id)
    {
        return UpdateQueueStatusInternal(id, QueueStatus.Cancel);
    }

    private async Task<IActionResult> UpdateQueueStatusInternal(int id, string status)
    {
        var queue = await _context.Queues
            .SingleOrDefaultAsync(q => q.ID == id && q.Valid);

        if (queue is null)
        {
            return NotFound(new { message = "Queue not found." });
        }

        queue.Status = status;
        queue.UpdatedAt = DateTime.UtcNow.AddHours(7);
        await _context.SaveChangesAsync();

        return Ok(queue);
    }
}
