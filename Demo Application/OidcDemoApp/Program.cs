using OidcDemoApp;
using Serilog;
using Serilog.Events;

Console.WriteLine("OidcDemoApp server started");

Console.WriteLine("");
Console.WriteLine("Connect to it using https://localhost:5001");
Console.WriteLine();

Log.Logger = new LoggerConfiguration()
.WriteTo.Console()
.CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, logger) =>
    {
        logger
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .MinimumLevel.Override("System", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.AspNetCore.Authentication", LogEventLevel.Debug)
            .MinimumLevel.Override("Microsoft.AspNetCore.Authorization", LogEventLevel.Debug)
            .WriteTo.Console(
                outputTemplate:
                "[{Timestamp:HH:mm:ss} {Level}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}")
            .Enrich.FromLogContext();
    });

    var app = builder
        .ConfigureServices()
        .ConfigurePipeline();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Unhandled exception");
}
finally
{
    Log.Information("Shutdown complete");
    Log.CloseAndFlush();
}
