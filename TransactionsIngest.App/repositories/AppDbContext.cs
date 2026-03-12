using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext {
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }

    public AppDbContext(DbContextOptions options) : base(options) {
}
}