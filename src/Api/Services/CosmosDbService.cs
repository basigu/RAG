using Api.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;

namespace Api.Services;

public class CosmosDbService
{
    private readonly Container _container;
    private readonly Container _leaseContainer;
    private readonly ILogger<CosmosDbService> _logger;

    private const string DatabaseName = "classicrag";
    private const string ContainerName = "properties";
    private const string LeaseContainerName = "leases";

    public CosmosDbService(CosmosClient cosmosClient, ILogger<CosmosDbService> logger)
    {
        _container = cosmosClient.GetContainer(DatabaseName, ContainerName);
        _leaseContainer = cosmosClient.GetContainer(DatabaseName, LeaseContainerName);
        _logger = logger;
    }

    public Container PropertiesContainer => _container;
    public Container LeaseContainer => _leaseContainer;

    public async Task<RealEstateProperty?> GetPropertyAsync(string id, string city)
    {
        try
        {
            var response = await _container.ReadItemAsync<RealEstateProperty>(id, new PartitionKey(city));
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<List<RealEstateProperty>> GetPropertiesAsync(string? city = null, int maxItems = 50)
    {
        var queryable = _container.GetItemLinqQueryable<RealEstateProperty>(
            requestOptions: city != null
                ? new QueryRequestOptions { PartitionKey = new PartitionKey(city) }
                : null);

        var iterator = queryable.Take(maxItems).ToFeedIterator();
        var results = new List<RealEstateProperty>();

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            results.AddRange(response);
        }

        return results;
    }

    public async Task<List<RealEstateProperty>> GetAllPropertiesAsync()
    {
        var query = _container.GetItemQueryIterator<RealEstateProperty>("SELECT * FROM c");
        var results = new List<RealEstateProperty>();

        while (query.HasMoreResults)
        {
            var response = await query.ReadNextAsync();
            results.AddRange(response);
        }

        return results;
    }

    public async Task<RealEstateProperty> UpsertPropertyAsync(RealEstateProperty property)
    {
        var response = await _container.UpsertItemAsync(property, new PartitionKey(property.City));
        _logger.LogInformation("Upserted property {Id} in {City}, cost: {RU} RUs", property.Id, property.City, response.RequestCharge);
        return response.Resource;
    }

    public async Task DeletePropertyAsync(string id, string city)
    {
        await _container.DeleteItemAsync<RealEstateProperty>(id, new PartitionKey(city));
        _logger.LogInformation("Deleted property {Id} from {City}", id, city);
    }

    public async Task BulkUpsertAsync(IReadOnlyList<RealEstateProperty> properties)
    {
        var tasks = new List<Task>(properties.Count);
        foreach (var p in properties)
        {
            tasks.Add(_container.UpsertItemAsync(p, new PartitionKey(p.City))
                .ContinueWith(t =>
                {
                    if (t.IsFaulted)
                        _logger.LogError(t.Exception?.InnerException, "Failed to upsert property {Id}", p.Id);
                }));
        }

        await Task.WhenAll(tasks);
        _logger.LogInformation("Bulk upserted {Count} properties", properties.Count);
    }
}
