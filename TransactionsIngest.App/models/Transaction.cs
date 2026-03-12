public class Transaction {
    
    public string TransactionId { get; set; }
    public string CardLast4 { get; set; }
    public string LocationCode {get; set; }
    public string ProductName {get; set;}
    public decimal Amount {get; set;}
    public DateTime TransactionTime {get; set;}
    public TransactionStatus Status {get; set;}

}