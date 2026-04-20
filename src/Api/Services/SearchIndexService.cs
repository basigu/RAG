using Api.Models;
using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;

namespace Api.Services;

public class SearchIndexService
{
    private readonly SearchIndexClient _indexClient;
    private readonly SearchClient _searchClient;
    private readonly ILogger<SearchIndexService> _logger;

    private const string IndexName = "properties-index";
    private const int VectorDimensions = 1536;

    public SearchIndexService(SearchIndexClient indexClient, ILogger<SearchIndexService> logger)
    {
        _indexClient = indexClient;
        _searchClient = indexClient.GetSearchClient(IndexName);
        _logger = logger;
    }

    public async Task EnsureIndexAsync()
    {
        var index = new SearchIndex(IndexName)
        {
            VectorSearch = new()
            {
                Profiles = { new VectorSearchProfile("vector-profile", "hnsw-config") },
                Algorithms = { new HnswAlgorithmConfiguration("hnsw-config") }
            },
            Fields =
            {
                new SimpleField("id", SearchFieldDataType.String) { IsKey = true, IsFilterable = true },
                new SearchableField("title") { IsFilterable = true },
                new SearchableField("description"),
                new SearchableField("propertyType") { IsFilterable = true, IsFacetable = true },
                new SearchableField("address"),
                new SearchableField("city") { IsFilterable = true, IsFacetable = true },
                new SimpleField("state", SearchFieldDataType.String) { IsFilterable = true, IsFacetable = true },
                new SimpleField("zipCode", SearchFieldDataType.String) { IsFilterable = true },
                new SimpleField("price", SearchFieldDataType.Double) { IsFilterable = true, IsSortable = true },
                new SimpleField("bedrooms", SearchFieldDataType.Int32) { IsFilterable = true, IsSortable = true },
                new SimpleField("bathrooms", SearchFieldDataType.Double) { IsFilterable = true, IsSortable = true },
                new SimpleField("squareFeet", SearchFieldDataType.Int32) { IsFilterable = true, IsSortable = true },
                new SimpleField("yearBuilt", SearchFieldDataType.Int32) { IsFilterable = true, IsSortable = true },
                new SimpleField("status", SearchFieldDataType.String) { IsFilterable = true, IsFacetable = true },
                new SearchableField("features", collection: true) { IsFilterable = true },
                new SearchableField("agentName"),
                new VectorSearchField("contentVector", VectorDimensions, "vector-profile")
            }
        };

        await _indexClient.CreateOrUpdateIndexAsync(index);
        _logger.LogInformation("Search index '{IndexName}' ensured", IndexName);
    }

    public async Task IndexPropertyAsync(RealEstateProperty property, ReadOnlyMemory<float> embedding)
    {
        var doc = CreateSearchDocument(property, embedding);
        await _searchClient.MergeOrUploadDocumentsAsync(new[] { doc });
        _logger.LogInformation("Indexed property {Id} to search", property.Id);
    }

    public async Task IndexPropertiesBatchAsync(
        IReadOnlyList<RealEstateProperty> properties,
        IReadOnlyList<ReadOnlyMemory<float>> embeddings)
    {
        var batch = new IndexDocumentsBatch<SearchDocument>();

        for (int i = 0; i < properties.Count; i++)
        {
            var doc = CreateSearchDocument(properties[i], embeddings[i]);
            batch.Actions.Add(IndexDocumentsAction.MergeOrUpload(doc));
        }

        // Upload in chunks of 100
        var actions = batch.Actions.ToList();
        foreach (var chunk in actions.Chunk(100))
        {
            var chunkBatch = new IndexDocumentsBatch<SearchDocument>();
            foreach (var action in chunk)
                chunkBatch.Actions.Add(action);

            await _searchClient.IndexDocumentsAsync(chunkBatch);
        }

        _logger.LogInformation("Batch indexed {Count} properties", properties.Count);
    }

    public async Task<List<(SearchDocument Document, double Score)>> SearchAsync(
        ReadOnlyMemory<float> queryEmbedding,
        string? textQuery = null,
        string? filter = null,
        int top = 5)
    {
        var searchOptions = new SearchOptions
        {
            VectorSearch = new()
            {
                Queries =
                {
                    new VectorizedQuery(queryEmbedding)
                    {
                        KNearestNeighborsCount = top,
                        Fields = { "contentVector" }
                    }
                }
            },
            Size = top,
            Select =
            {
                "id", "title", "description", "propertyType", "address",
                "city", "state", "zipCode", "price", "bedrooms", "bathrooms",
                "squareFeet", "yearBuilt", "status", "features", "agentName"
            }
        };

        if (!string.IsNullOrEmpty(filter))
            searchOptions.Filter = filter;

        var response = await _searchClient.SearchAsync<SearchDocument>(textQuery, searchOptions);
        var results = new List<(SearchDocument, double)>();

        await foreach (var result in response.Value.GetResultsAsync())
        {
            results.Add((result.Document, result.Score ?? 0));
        }

        return results;
    }

    public async Task DeleteDocumentAsync(string id)
    {
        await _searchClient.DeleteDocumentsAsync("id", new[] { id });
        _logger.LogInformation("Deleted property {Id} from search index", id);
    }

    private static SearchDocument CreateSearchDocument(RealEstateProperty property, ReadOnlyMemory<float> embedding)
    {
        return new SearchDocument
        {
            ["id"] = property.Id,
            ["title"] = property.Title,
            ["description"] = property.Description,
            ["propertyType"] = property.PropertyType,
            ["address"] = property.Address,
            ["city"] = property.City,
            ["state"] = property.State,
            ["zipCode"] = property.ZipCode,
            ["price"] = (double)property.Price,
            ["bedrooms"] = property.Bedrooms,
            ["bathrooms"] = property.Bathrooms,
            ["squareFeet"] = property.SquareFeet,
            ["yearBuilt"] = property.YearBuilt,
            ["status"] = property.Status,
            ["features"] = property.Features.ToArray(),
            ["agentName"] = property.AgentName,
            ["contentVector"] = embedding.ToArray()
        };
    }
}
