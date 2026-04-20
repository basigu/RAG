using Api.Models;
using Api.Services;

namespace Api.Endpoints;

public static class PropertyEndpoints
{
    public static void MapPropertyEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/properties").WithTags("Properties");

        group.MapGet("/", async (string? city, int? maxItems, CosmosDbService cosmos) =>
        {
            var properties = await cosmos.GetPropertiesAsync(city, maxItems ?? 50);
            return Results.Ok(properties);
        })
        .WithName("ListProperties");

        group.MapGet("/{id}", async (string id, string city, CosmosDbService cosmos) =>
        {
            var property = await cosmos.GetPropertyAsync(id, city);
            return property is not null ? Results.Ok(property) : Results.NotFound();
        })
        .WithName("GetProperty");

        group.MapPost("/", async (RealEstateProperty property, CosmosDbService cosmos) =>
        {
            if (string.IsNullOrWhiteSpace(property.City))
                return Results.BadRequest(new { error = "City is required (used as partition key)" });

            if (string.IsNullOrEmpty(property.Id))
                property.Id = Guid.NewGuid().ToString();

            var result = await cosmos.UpsertPropertyAsync(property);
            return Results.Created($"/properties/{result.Id}?city={result.City}", result);
        })
        .WithName("CreateProperty");

        group.MapPut("/{id}", async (string id, RealEstateProperty property, CosmosDbService cosmos) =>
        {
            if (string.IsNullOrWhiteSpace(property.City))
                return Results.BadRequest(new { error = "City is required (used as partition key)" });

            property.Id = id;
            var result = await cosmos.UpsertPropertyAsync(property);
            return Results.Ok(result);
        })
        .WithName("UpdateProperty");

        group.MapDelete("/{id}", async (string id, string city, CosmosDbService cosmos, SearchIndexService search) =>
        {
            await cosmos.DeletePropertyAsync(id, city);
            await search.DeleteDocumentAsync(id);
            return Results.NoContent();
        })
        .WithName("DeleteProperty");
    }
}
