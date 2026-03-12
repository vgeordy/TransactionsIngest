public interface ITransactionApiClient {
    Task<List<Transaction>> FetchLast24HoursAsync();
}