using System.Text.Json;
using Api.Endpoints;
using Api.Services;
using Azure.AI.OpenAI;
using Azure.Identity;
using Azure.Search.Documents.Indexes;
using Microsoft.Azure.Cosmos;

var builder = WebApplication.CreateBuilder(args);

// --- Azure Identity ---
var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
{
    ManagedIdentityClientId = builder.Configuration["AZURE_CLIENT_ID"]
});

// --- Cosmos DB (singleton) ---
var cosmosClient = new CosmosClient(
    builder.Configuration["AZURE_COSMOSDB_ENDPOINT"],
    credential,
    new CosmosClientOptions
    {
        UseSystemTextJsonSerializerWithOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        },
        AllowBulkExecution = true
    });
builder.Services.AddSingleton(cosmosClient);
builder.Services.AddSingleton<CosmosDbService>();

// --- Azure AI Search ---
var searchIndexClient = new SearchIndexClient(
    new Uri(builder.Configuration["AZURE_SEARCH_ENDPOINT"]!),
    credential);
builder.Services.AddSingleton(searchIndexClient);
builder.Services.AddSingleton<SearchIndexService>();

// --- Azure OpenAI ---
var openAiClient = new AzureOpenAIClient(
    new Uri(builder.Configuration["AZURE_OPENAI_ENDPOINT"]!),
    credential);
builder.Services.AddSingleton(openAiClient);
builder.Services.AddSingleton<EmbeddingService>();
builder.Services.AddSingleton<RagService>();

// --- Change Feed Background Service ---
builder.Services.AddHostedService<ChangeFeedService>();

// --- CORS (allow frontend Container App) ---
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseCors();

// --- Map Endpoints ---
app.MapChatEndpoints();
app.MapPropertyEndpoints();
app.MapAdminEndpoints();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
   .WithTags("Health");

app.Run();

public partial class Program { }
