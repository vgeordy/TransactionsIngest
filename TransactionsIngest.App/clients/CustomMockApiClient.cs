public class CustomMockApiClient : ITransactionApiClient
{
    private readonly List<Transaction> _transactions;
    
    public CustomMockApiClient(List<Transaction> transactions)
    {
        _transactions = transactions;
    }
    
    public Task<List<Transaction>> FetchLast24HoursAsync()
    {
        return Task.FromResult(_transactions);
    }
}