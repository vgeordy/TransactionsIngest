using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        var connectionString = context.Configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<AppDbContext>(options =>
        options.UseSqlite(connectionString));
        services.AddScoped<AuditService>();
        services.AddScoped<TransactionService>();
        services.AddScoped<ITransactionApiClient, MockTransactionApiClient>();

    })
    .Build();

var transactionService = host.Services.GetRequiredService<TransactionService>();
await transactionService.ProcessTransactionsAsync();