using Microsoft.EntityFrameworkCore;

public class TransactionService
{
    private readonly ITransactionApiClient _apiClient;
    private readonly AppDbContext _context;
    private readonly AuditService _auditService;

    public TransactionService(

        ITransactionApiClient apiClient,
        AppDbContext context,
        AuditService auditService)
    {
        _apiClient = apiClient;
        _context = context;
        _auditService = auditService;
    }

    public async Task ProcessTransactionsAsync()
    {
        // 
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {

            // Fetch the snapshot from the api client
            List<Transaction> snapshots = await _apiClient.FetchLast24HoursAsync();
            // Process the snapshot
            await ProcessSnapshot(snapshots);
            // revoke missing transactions
            await RevokeTransactions(snapshots);

            await FinalizeTransactions();

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private async Task ProcessSnapshot(List<Transaction> snapshots)
    {
        foreach (Transaction snapshot in snapshots)
        {
            // look it up in db
            var existing = await _context.Transactions.
            FirstOrDefaultAsync(t => t.TransactionId == snapshot.TransactionId);

            // if found
            if (existing != null)
            {
                // update the transaction
                await UpdateTransaction(existing, snapshot);

            }
            else
            {
                // insert the transaction
                await InsertTransaction(snapshot);
            }



        }
    }

    private async Task InsertTransaction(Transaction snapshot)
    {
        snapshot.Status = TransactionStatus.Pending;
        _context.Transactions.Add(snapshot);
        await _auditService.InsertAuditLog(snapshot.TransactionId, AuditLogStatus.Created, null, null, null);
        await _context.SaveChangesAsync();
    }

    private async Task UpdateTransaction(Transaction existing, Transaction incoming)
    {
        // any record older than 24 hours can be marked as finalized and should not change afterward
        if (existing.Status == TransactionStatus.Finalized)
            return;

        // transactionId and card number don't change, so we only account for the following:

        bool hasChanged = false;



        // LocationCode
        if (existing.LocationCode != incoming.LocationCode)
        {
            await _auditService.InsertAuditLog(existing.TransactionId, AuditLogStatus.Updated, "LocationCode", existing.LocationCode, incoming.LocationCode);
            existing.LocationCode = incoming.LocationCode;
            hasChanged = true;
        }

        // ProductName
        if (existing.ProductName != incoming.ProductName)
        {
            await _auditService.InsertAuditLog(existing.TransactionId, AuditLogStatus.Updated, "ProductName", existing.ProductName, incoming.ProductName);
            existing.ProductName = incoming.ProductName;
            hasChanged = true;
        }

        // Amount
        if (existing.Amount != incoming.Amount)
        {
            await _auditService.InsertAuditLog(existing.TransactionId, AuditLogStatus.Updated, "Amount", existing.Amount.ToString(), incoming.Amount.ToString());
            existing.Amount = incoming.Amount;
            hasChanged = true;
        }

        // TransactionTime

        if (existing.TransactionTime != incoming.TransactionTime)
        {
            await _auditService.InsertAuditLog(existing.TransactionId, AuditLogStatus.Updated, "TransactionTime", existing.TransactionTime.ToString("O"), incoming.TransactionTime.ToString("O"));
            existing.TransactionTime = incoming.TransactionTime;
            hasChanged = true;
        }

        if (hasChanged)
        {
            await _context.SaveChangesAsync();
        }


    }

    private async Task RevokeTransactions(List<Transaction> snapshots)
    {

        var cutoff = DateTime.UtcNow.AddHours(-24);
        var snapShotIds = snapshots.Select(t => t.TransactionId).ToHashSet();

        var toRevoke = await _context.Transactions
        .Where(t => !snapShotIds.Contains(t.TransactionId))
        .Where(t => t.Status != TransactionStatus.Revoked)
        .Where(t => t.TransactionTime >= cutoff)
        .ToListAsync();

        foreach (Transaction t in toRevoke)
        {
            t.Status = TransactionStatus.Revoked;
            await _auditService.InsertAuditLog(t.TransactionId, AuditLogStatus.Revoked, null, null, null);
        }

        await _context.SaveChangesAsync();
    }

    private async Task FinalizeTransactions()
    {
        var cutoff = DateTime.UtcNow.AddHours(-24);

        var toFinalize = await _context.Transactions
        .Where(t => t.Status != TransactionStatus.Revoked)
        .Where(t => t.Status != TransactionStatus.Finalized)
        .Where(t => t.TransactionTime <= cutoff)
        .ToListAsync();

        foreach (Transaction t in toFinalize)
        {
            t.Status = TransactionStatus.Finalized;
            await _auditService.InsertAuditLog(t.TransactionId, AuditLogStatus.Finalized, null, null, null);
        }

        await _context.SaveChangesAsync();
    }

}