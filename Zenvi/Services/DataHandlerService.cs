namespace Zenvi.Services;

public class DataHandlerService(IHostEnvironment environment, IConfiguration configuration)
{
    private readonly string _basePath = environment.IsDevelopment()
        ? Path.Combine(Directory.GetCurrentDirectory(), "TempData") // Specify a 'Temp' subdirectory under the current directory
        : configuration.GetValue<string>("DataSettings:BasePath") ?? throw new InvalidOperationException("Data path not set in appsettings.json");

    public string GetBasePath()
    {
        return _basePath;
    }
}