using Api.Models;
using Azure.AI.OpenAI;
using Azure.Search.Documents.Models;
using OpenAI.Chat;

namespace Api.Services;

public class RagService
{
    private readonly SearchIndexService _searchService;
    private readonly EmbeddingService _embeddingService;
    private readonly ChatClient _chatClient;
    private readonly ILogger<RagService> _logger;

    public RagService(
        SearchIndexService searchService,
        EmbeddingService embeddingService,
        AzureOpenAIClient openAiClient,
        IConfiguration config,
        ILogger<RagService> logger)
    {
        _searchService = searchService;
        _embeddingService = embeddingService;
        _chatClient = openAiClient.GetChatClient(config["AZURE_OPENAI_CHAT_DEPLOYMENT"]!);
        _logger = logger;
    }

    public async Task<ChatResponse> AskAsync(string question)
    {
        // 1. Generate embedding for the question
        var questionEmbedding = await _embeddingService.GetEmbeddingAsync(question);

        // 2. Hybrid search: vector + keyword
        var searchResults = await _searchService.SearchAsync(questionEmbedding, textQuery: question, top: 5);

        if (searchResults.Count == 0)
        {
            return new ChatResponse(
                "I couldn't find any properties matching your query. Try a different search.",
                []);
        }

        // 3. Build context from search results (limit to ~6000 chars to stay within token budget)
        var contextParts = new List<string>();
        var totalLength = 0;
        const int maxContextLength = 6000;
        foreach (var (doc, i) in searchResults.Select((r, i) => (r.Document, i)))
        {
            var part = FormatPropertyContext(doc, i + 1);
            if (totalLength + part.Length > maxContextLength)
                break;
            contextParts.Add(part);
            totalLength += part.Length;
        }
        var context = string.Join("\n\n", contextParts);

        // 4. Generate response with Azure OpenAI
        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(
                """
                You are a knowledgeable real estate assistant. Answer questions about properties based on
                the listing data provided below. Be specific with details like price, location, size, and features.
                If the data doesn't contain enough information to answer, say so.
                Always reference specific properties by their listing number when discussing them.
                Format prices with dollar signs and commas. Be concise but helpful.

                PROPERTY LISTINGS:
                """ + context),
            new UserChatMessage(question)
        };

        ChatCompletion completion = await _chatClient.CompleteChatAsync(messages);
        var answer = completion.Content[0].Text;

        _logger.LogInformation("RAG query: '{Question}', found {Count} results", question, searchResults.Count);

        // 5. Build source references
        var sources = searchResults.Select(r => new PropertyResult(
            Id: r.Document["id"]?.ToString() ?? "",
            Title: r.Document["title"]?.ToString() ?? "",
            City: r.Document["city"]?.ToString() ?? "",
            State: r.Document["state"]?.ToString() ?? "",
            Price: Convert.ToDecimal(r.Document["price"] ?? 0),
            Bedrooms: Convert.ToInt32(r.Document["bedrooms"] ?? 0),
            Bathrooms: Convert.ToDouble(r.Document["bathrooms"] ?? 0),
            SquareFeet: Convert.ToInt32(r.Document["squareFeet"] ?? 0),
            Score: r.Score
        )).ToList();

        return new ChatResponse(answer, sources);
    }

    private static string FormatPropertyContext(SearchDocument doc, int index)
    {
        return $"""
            Listing #{index}:
            Title: {doc["title"]}
            Type: {doc["propertyType"]}
            Address: {doc["address"]}, {doc["city"]}, {doc["state"]} {doc["zipCode"]}
            Price: ${Convert.ToDecimal(doc["price"]):N0}
            Bedrooms: {doc["bedrooms"]} | Bathrooms: {doc["bathrooms"]} | Sqft: {doc["squareFeet"]}
            Year Built: {doc["yearBuilt"]}
            Status: {doc["status"]}
            Features: {string.Join(", ", (doc["features"] as object[] ?? []).Select(f => f?.ToString() ?? ""))}
            Agent: {doc["agentName"]}
            """;
    }
}
