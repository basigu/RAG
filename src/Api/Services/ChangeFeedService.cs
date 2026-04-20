using Api.Models;
using Microsoft.Azure.Cosmos;

namespace Api.Services;

public class ChangeFeedService : BackgroundService
{
    private readonly CosmosDbService _cosmosDbService;
    private readonly SearchIndexService _searchService;
    private readonly EmbeddingService _embeddingService;
    private readonly ILogger<ChangeFeedService> _logger;
    private ChangeFeedProcessor? _processor;

    public ChangeFeedService(
        CosmosDbService cosmosDbService,
        SearchIndexService searchService,
        EmbeddingService embeddingService,
        ILogger<ChangeFeedService> logger)
    {
        _cosmosDbService = cosmosDbService;
        _searchService = searchService;
        _embeddingService = embeddingService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Ensure the search index exists before processing changes
        await _searchService.EnsureIndexAsync();

        _processor = _cosmosDbService.PropertiesContainer
            .GetChangeFeedProcessorBuilder<RealEstateProperty>(
                processorName: "searchIndexer",
                onChangesDelegate: HandleChangesAsync)
            .WithInstanceName(Environment.MachineName)
            .WithLeaseContainer(_cosmosDbService.LeaseContainer)
            .WithStartTime(DateTime.MinValue.ToUniversalTime())
            .Build();

        _logger.LogInformation("Starting Cosmos DB Change Feed processor");
        await _processor.StartAsync();

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Change Feed processor stopping");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_processor is not null)
        {
            await _processor.StopAsync();
            _logger.LogInformation("Change Feed processor stopped");
        }

        await base.StopAsync(cancellationToken);
    }

    private async Task HandleChangesAsync(
        ChangeFeedProcessorContext context,
        IReadOnlyCollection<RealEstateProperty> changes,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Change Feed: processing {Count} changes from lease {LeaseToken}",
            changes.Count, context.LeaseToken);

        try
        {
            var properties = changes.ToList();
            var texts = properties.Select(EmbeddingService.GetEmbeddingText).ToList();
            var embeddings = (await _embeddingService.GetEmbeddingsAsync(texts)).ToList();

            await _searchService.IndexPropertiesBatchAsync(properties, embeddings);

            _logger.LogInformation("Change Feed: indexed {Count} properties to search", properties.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Change Feed: error processing changes from lease {LeaseToken}", context.LeaseToken);
            throw;
        }
    }
}
