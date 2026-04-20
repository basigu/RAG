using System.Text.Json.Serialization;

namespace Api.Models;

public class RealEstateProperty
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("propertyType")]
    public string PropertyType { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("address")]
    public string Address { get; set; } = string.Empty;

    [JsonPropertyName("city")]
    public string City { get; set; } = string.Empty;

    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;

    [JsonPropertyName("zipCode")]
    public string ZipCode { get; set; } = string.Empty;

    [JsonPropertyName("price")]
    public decimal Price { get; set; }

    [JsonPropertyName("bedrooms")]
    public int Bedrooms { get; set; }

    [JsonPropertyName("bathrooms")]
    public double Bathrooms { get; set; }

    [JsonPropertyName("squareFeet")]
    public int SquareFeet { get; set; }

    [JsonPropertyName("lotSizeAcres")]
    public double LotSizeAcres { get; set; }

    [JsonPropertyName("yearBuilt")]
    public int YearBuilt { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = "Active";

    [JsonPropertyName("features")]
    public List<string> Features { get; set; } = [];

    [JsonPropertyName("agentName")]
    public string AgentName { get; set; } = string.Empty;

    [JsonPropertyName("agentPhone")]
    public string AgentPhone { get; set; } = string.Empty;

    [JsonPropertyName("listedDate")]
    public DateTime ListedDate { get; set; } = DateTime.UtcNow;
}
