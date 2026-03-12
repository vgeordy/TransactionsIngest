using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace TransactionsIngest.Tests;

public class TransactionServiceTests
{
    [Fact]
    public async Task InsertTransaction_NewTransaction_ShouldBeInserted()
    {
        // Arrange
        
        var options = new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
        .Options;

        var context = new AppDbContext(options);

        var ITransactionApiClientMock = new MockTransactionApiClient();

        var auditServiceMock = new AuditService(context);

        var transactionServiceMock = new TransactionService(ITransactionApiClientMock, context, auditServiceMock);

        // Act

        await transactionServiceMock.ProcessTransactionsAsync();

        // Assert

        var count = context.Transactions.Count();

        Assert.Equal(2, count);

        Assert.Equal(2, context.AuditLogs
        .Where(t => t.EventName == AuditLogStatus.Created)
        .Count());

    }

    [Fact]
    public async Task InsertTransaction_Idempotency_ShouldBeInsertedOnce()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
       .UseInMemoryDatabase(Guid.NewGuid().ToString())
       .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
       .Options;

        var context = new AppDbContext(options);

        var ITransactionApiClientMock = new MockTransactionApiClient();

        var auditServiceMock = new AuditService(context);

        var transactionServiceMock = new TransactionService(ITransactionApiClientMock, context, auditServiceMock);

        // Act

        await transactionServiceMock.ProcessTransactionsAsync();

        await transactionServiceMock.ProcessTransactionsAsync();


        // Assert
        var count = context.Transactions.Count();

        Assert.Equal(2, count);

        Assert.Equal(2, context.AuditLogs
        .Where(t => t.EventName == AuditLogStatus.Created)
        .Count());

    }

    [Fact]
    public async Task UpdateTransaction_ChangeAmount_ShouldBeUpdatedAndLogged()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
       .UseInMemoryDatabase(Guid.NewGuid().ToString())
       .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
       .Options;

        var context = new AppDbContext(options);

        var auditServiceMock = new AuditService(context);


        // first run
        var transactions = new List<Transaction> {
            new Transaction {
                TransactionId = "T-1001",
                CardLast4 = "1111",
                LocationCode = "STO-01",
                ProductName = "Wireless Mouse",
                Amount = 19.99m,
                TransactionTime = DateTime.Parse("2026-03-12T06:00:00Z").ToUniversalTime(),
                Status = TransactionStatus.Pending
            }
        };

        var ITransactionApiClientMock = new CustomMockApiClient(transactions);

        var transactionServiceMock = new TransactionService(ITransactionApiClientMock, context, auditServiceMock);


        await transactionServiceMock.ProcessTransactionsAsync();

        context.ChangeTracker.Clear();

        // second run with different amount
        var transactions2 = new List<Transaction> {
            new Transaction {
                TransactionId = "T-1001",
                CardLast4 = "1111",
                LocationCode = "STO-01",
                ProductName = "Wireless Mouse",
                Amount = 17.99m,
                TransactionTime = DateTime.Parse("2026-03-12T06:00:00Z").ToUniversalTime(),
                Status = TransactionStatus.Pending
            }
        };

        var ITransactionApiClientMock2 = new CustomMockApiClient(transactions2);

        var transactionServiceMock2 = new TransactionService(ITransactionApiClientMock2, context, auditServiceMock);

        // Act
        await transactionServiceMock2.ProcessTransactionsAsync();

        // Assert
        var count = context.Transactions.Count();
        var transaction = context.Transactions.First();

        Assert.Equal(1, count);
        Assert.Equal(17.99m, transaction.Amount);
        Assert.Equal(1, context.AuditLogs
        .Where(t => t.EventName == AuditLogStatus.Updated)
        .Count());
    }

    [Fact]
    public async Task RevokeTransactions_EmptySnapshot_ShouldRevokeTransactionAndLog()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
       .UseInMemoryDatabase(Guid.NewGuid().ToString())
       .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
       .Options;
        
        var context = new AppDbContext(options);
        var auditServiceMock = new AuditService(context);

        // first run
        var transactions = new List<Transaction> {
            new Transaction {
                TransactionId = "T-1001",
                CardLast4 = "1111",
                LocationCode = "STO-01",
                ProductName = "Wireless Mouse",
                Amount = 19.99m,
                TransactionTime = DateTime.Parse("2026-03-12T06:00:00Z").ToUniversalTime(),
                Status = TransactionStatus.Pending
            },
             new Transaction {
                TransactionId = "T-1002",
                CardLast4 = "0002",
                LocationCode = "STO-02",
                ProductName = "USB-C Cable",
                Amount = 25.0m,
                TransactionTime = DateTime.Parse("2026-03-12T06:00:00Z").ToUniversalTime(),
                Status = TransactionStatus.Pending
            }
        };

        var ITransactionApiClientMock = new CustomMockApiClient(transactions);

        var transactionServiceMock = new TransactionService(ITransactionApiClientMock, context, auditServiceMock);


        await transactionServiceMock.ProcessTransactionsAsync();

        context.ChangeTracker.Clear();


        // second run
        var transactions2 = new List<Transaction> {
             new Transaction {
                TransactionId = "T-1002",
                CardLast4 = "0002",
                LocationCode = "STO-02",
                ProductName = "USB-C Cable",
                Amount = 25.0m,
                TransactionTime = DateTime.Parse("2026-03-12T06:00:00Z").ToUniversalTime(),
                Status = TransactionStatus.Pending
            }
        };
        var ITransactionApiClientMock2 = new CustomMockApiClient(transactions2);
        var transactionServiceMock2 = new TransactionService(ITransactionApiClientMock2, context, auditServiceMock);
       
       await transactionServiceMock2.ProcessTransactionsAsync();

    
    var count = context.Transactions.Count();

    Assert.Equal(2, count);
    Assert.Equal(1, context.AuditLogs
    .Where(t => t.EventName == AuditLogStatus.Revoked)
    .Count());

    }
}
