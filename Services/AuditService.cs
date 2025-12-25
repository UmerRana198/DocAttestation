using DocAttestation.Data;
using DocAttestation.Models;
using Microsoft.AspNetCore.Http;

namespace DocAttestation.Services;

public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task LogWorkflowActionAsync(
        int applicationId,
        WorkflowLevel level,
        string userId,
        string action,
        string? remarks,
        ApplicationStatus previousStatus,
        ApplicationStatus newStatus)
    {
        var ipAddress = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

        var history = new WorkflowHistory
        {
            ApplicationId = applicationId,
            Level = level,
            ActionByUserId = userId,
            Action = action,
            Remarks = remarks,
            PreviousStatus = previousStatus,
            NewStatus = newStatus,
            ActionDate = DateTime.UtcNow,
            IpAddress = ipAddress
        };

        _context.WorkflowHistory.Add(history);
        await _context.SaveChangesAsync();
    }
}

