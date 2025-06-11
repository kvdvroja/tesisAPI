using tesisAPI.Services;
public class HostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;

    public HostedService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _serviceProvider.CreateScope();
            var archivoService = scope.ServiceProvider.GetRequiredService<ArchivoService>();

            await archivoService.SubirArchivosPendientes();

            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}