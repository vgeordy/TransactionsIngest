public class AuditLog
{
    public int AuditLogId { get; set; }
    public string TransactionId { get; set; }
    public AuditLogStatus EventName { get; set; }
    public string? FieldName { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public DateTime Timestamp { get; set; }
    public Transaction Transaction { get; set; }
}