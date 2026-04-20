namespace Api.Models;

public record ChatRequest(string Message);

public record ChatResponse(string Answer, List<PropertyResult> Sources);

public record PropertyResult(
    string Id,
    string Title,
    string City,
    string State,
    decimal Price,
    int Bedrooms,
    double Bathrooms,
    int SquareFeet,
    double Score);
