using Api.Models;
using Api.Services;

namespace Api.Endpoints;

public static class ChatEndpoints
{
    public static void MapChatEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/chat").WithTags("Chat");

        group.MapPost("/", async (ChatRequest request, RagService ragService) =>
        {
            if (string.IsNullOrWhiteSpace(request.Message))
                return Results.BadRequest("Message is required");

            var response = await ragService.AskAsync(request.Message);
            return Results.Ok(response);
        })
        .WithName("Chat")
        .WithDescription("Ask a question about real estate properties using RAG");
    }
}
