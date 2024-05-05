namespace NationsConverterBuilder.Services;

internal sealed class GeneralBuildService
{
    private readonly SetupService setupService;

    public GeneralBuildService(SetupService setupService)
    {
        this.setupService = setupService;
    }

    public async Task BuildAsync(CancellationToken cancellationToken = default)
    {
        
    }
}
