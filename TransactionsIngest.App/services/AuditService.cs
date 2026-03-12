public class AuditService
{

    private readonly AppDbContext _context;

    public AuditService(
        AppDbContext context
    )
    {
        _context = context;
    }
    public async Task InsertAuditLog(string transactionId, AuditLogStatus eventName,
        string? fieldName, string? oldValue, string? newValue)
    {
        var auditLog = new AuditLog
        {
            TransactionId = transactionId,
            EventName = eventName,
            FieldName = fieldName,
            OldValue = oldValue,
            NewValue = newValue,
            Timestamp = DateTime.UtcNow
        };

        _context.AuditLogs.Add(auditLog);
    }
}