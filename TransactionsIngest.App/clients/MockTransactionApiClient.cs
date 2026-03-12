public class MockTransactionApiClient : ITransactionApiClient
{
    public async Task<List<Transaction>> FetchLast24HoursAsync()
    {
        var transactions = new List<Transaction> {
            new Transaction {
                TransactionId = "T-1001",
                CardLast4 = "1111",
                LocationCode = "STO-01",
                ProductName = "Wireless Mouse",
                Amount = 19.99m,
                TransactionTime = DateTime.Parse("2026-02-27T08:50:10Z").ToUniversalTime(),
                Status = TransactionStatus.Pending
            },
            new Transaction {
                TransactionId = "T-1002",
                CardLast4 = "0002",
                LocationCode = "STO-02",
                ProductName = "USB-C Cable",
                Amount = 25.0m,
                TransactionTime = DateTime.Parse("2026-02-27T06:15:30Z").ToUniversalTime(),
                Status = TransactionStatus.Pending
            }
        };

        return await Task.FromResult(transactions);
    }
}