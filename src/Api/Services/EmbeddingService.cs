using Api.Models;
using Azure.AI.OpenAI;
using OpenAI.Embeddings;

namespace Api.Services;

public class EmbeddingService
{
    private readonly EmbeddingClient _client;

    public EmbeddingService(AzureOpenAIClient openAiClient, IConfiguration config)
    {
        _client = openAiClient.GetEmbeddingClient(config["AZURE_OPENAI_EMBEDDING_DEPLOYMENT"]!);
    }

    public async Task<ReadOnlyMemory<float>> GetEmbeddingAsync(string text)
    {
        OpenAIEmbedding embedding = await _client.GenerateEmbeddingAsync(text);
        return embedding.ToFloats();
    }

    public async Task<List<ReadOnlyMemory<float>>> GetEmbeddingsAsync(IEnumerable<string> texts)
    {
        var textList = texts.ToList();
        if (textList.Count == 0)
            return [];

        // Process in batches of 16 to stay within API limits
        var allEmbeddings = new List<ReadOnlyMemory<float>>();
        foreach (var batch in textList.Chunk(16))
        {
            OpenAIEmbeddingCollection embeddings = await _client.GenerateEmbeddingsAsync(batch.ToList());
            allEmbeddings.AddRange(embeddings.Select(e => e.ToFloats()));
        }

        return allEmbeddings;
    }

    public static string GetEmbeddingText(RealEstateProperty property)
    {
        return $"{property.PropertyType} in {property.City}, {property.State}. " +
               $"{property.Title}. {property.Description} " +
               $"{property.Bedrooms} bedrooms, {property.Bathrooms} bathrooms, " +
               $"{property.SquareFeet} sqft. Built in {property.YearBuilt}. " +
               $"Price: ${property.Price:N0}. Status: {property.Status}. " +
               $"Features: {string.Join(", ", property.Features)}.";
    }
}
