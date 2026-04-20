using Api.Models;
using Api.Services;

namespace Api.Endpoints;

public static class AdminEndpoints
{
    public static void MapAdminEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/admin").WithTags("Admin");

        group.MapPost("/seed", async (CosmosDbService cosmos, ILogger<Program> logger) =>
        {
            var properties = GenerateSeedData();
            await cosmos.BulkUpsertAsync(properties);
            logger.LogInformation("Seeded {Count} properties to Cosmos DB", properties.Count);
            return Results.Ok(new { message = $"Seeded {properties.Count} properties. Change Feed will index them to Search automatically." });
        })
        .WithName("SeedData")
        .WithDescription("Seed Cosmos DB with sample real estate data");

        group.MapPost("/reindex", async (
            CosmosDbService cosmos,
            SearchIndexService search,
            EmbeddingService embedding,
            ILogger<Program> logger) =>
        {
            await search.EnsureIndexAsync();

            var properties = await cosmos.GetAllPropertiesAsync();
            if (properties.Count == 0)
                return Results.Ok(new { message = "No properties found to index" });

            var texts = properties.Select(EmbeddingService.GetEmbeddingText).ToList();
            var embeddings = (await embedding.GetEmbeddingsAsync(texts)).ToList();
            await search.IndexPropertiesBatchAsync(properties, embeddings);

            logger.LogInformation("Bulk reindexed {Count} properties", properties.Count);
            return Results.Ok(new { message = $"Reindexed {properties.Count} properties" });
        })
        .WithName("Reindex")
        .WithDescription("Bulk reindex all properties from Cosmos DB to Azure AI Search");
    }

    private static List<RealEstateProperty> GenerateSeedData()
    {
        return
        [
            new()
            {
                Id = "prop-001", PropertyType = "House", Title = "Modern Craftsman Home in Capitol Hill",
                Description = "Stunning 4-bedroom craftsman with original hardwood floors, updated kitchen with quartz countertops, and a spacious backyard with mature landscaping. Walking distance to shops and restaurants.",
                Address = "1234 E Pine St", City = "Seattle", State = "WA", ZipCode = "98122",
                Price = 895000, Bedrooms = 4, Bathrooms = 2.5, SquareFeet = 2400, LotSizeAcres = 0.15, YearBuilt = 1920,
                Status = "Active", Features = ["Hardwood Floors", "Updated Kitchen", "Backyard", "Garage", "Fireplace"],
                AgentName = "Sarah Chen", AgentPhone = "206-555-0101", ListedDate = DateTime.UtcNow.AddDays(-14)
            },
            new()
            {
                Id = "prop-002", PropertyType = "Condo", Title = "Luxury Waterfront Condo with Panoramic Views",
                Description = "High-rise luxury condo with floor-to-ceiling windows overlooking Elliott Bay. Features include a chef's kitchen, spa-like bathroom, and access to building amenities including pool, gym, and concierge.",
                Address = "2001 Western Ave #1502", City = "Seattle", State = "WA", ZipCode = "98121",
                Price = 1250000, Bedrooms = 2, Bathrooms = 2, SquareFeet = 1450, LotSizeAcres = 0, YearBuilt = 2019,
                Status = "Active", Features = ["Waterfront", "Concierge", "Pool", "Gym", "Parking", "Floor-to-Ceiling Windows"],
                AgentName = "James Rodriguez", AgentPhone = "206-555-0102", ListedDate = DateTime.UtcNow.AddDays(-7)
            },
            new()
            {
                Id = "prop-003", PropertyType = "Townhouse", Title = "New Construction Townhouse in Ballard",
                Description = "Brand new 3-story townhouse with rooftop deck, 2-car garage, and high-end finishes throughout. Open concept living with chef's kitchen, smart home features, and energy-efficient design.",
                Address = "5678 NW Market St", City = "Seattle", State = "WA", ZipCode = "98107",
                Price = 749000, Bedrooms = 3, Bathrooms = 2.5, SquareFeet = 1800, LotSizeAcres = 0.04, YearBuilt = 2025,
                Status = "Active", Features = ["Rooftop Deck", "Smart Home", "EV Charging", "2-Car Garage", "Energy Efficient"],
                AgentName = "Sarah Chen", AgentPhone = "206-555-0101", ListedDate = DateTime.UtcNow.AddDays(-3)
            },
            new()
            {
                Id = "prop-004", PropertyType = "House", Title = "Victorian Beauty in Pacific Heights",
                Description = "Meticulously restored Victorian home with period details, modern amenities, and breathtaking Bay views. Features include a wine cellar, home office, and landscaped garden. Steps from Fillmore Street shopping.",
                Address = "2850 Pacific Ave", City = "San Francisco", State = "CA", ZipCode = "94115",
                Price = 4500000, Bedrooms = 5, Bathrooms = 4, SquareFeet = 4200, LotSizeAcres = 0.12, YearBuilt = 1895,
                Status = "Active", Features = ["Bay Views", "Wine Cellar", "Garden", "Home Office", "Period Details", "Restored"],
                AgentName = "Michael Chang", AgentPhone = "415-555-0201", ListedDate = DateTime.UtcNow.AddDays(-21)
            },
            new()
            {
                Id = "prop-005", PropertyType = "Condo", Title = "SoMa Loft with Industrial Charm",
                Description = "Converted warehouse loft with soaring 14-foot ceilings, exposed brick, and polished concrete floors. Open floor plan perfect for entertaining. Located in the heart of SoMa near tech offices.",
                Address = "888 Brannan St #305", City = "San Francisco", State = "CA", ZipCode = "94103",
                Price = 975000, Bedrooms = 1, Bathrooms = 1, SquareFeet = 1100, LotSizeAcres = 0, YearBuilt = 2015,
                Status = "Active", Features = ["Loft", "Exposed Brick", "High Ceilings", "Concrete Floors", "In-Unit Laundry"],
                AgentName = "Lisa Park", AgentPhone = "415-555-0202", ListedDate = DateTime.UtcNow.AddDays(-10)
            },
            new()
            {
                Id = "prop-006", PropertyType = "House", Title = "Hill Country Estate with Pool",
                Description = "Sprawling ranch-style home on 2 acres in the Texas Hill Country. Features a resort-style pool, outdoor kitchen, and stunning hill country views. Open concept living with a gourmet kitchen and custom finishes.",
                Address = "4500 Bee Cave Rd", City = "Austin", State = "TX", ZipCode = "78746",
                Price = 1650000, Bedrooms = 5, Bathrooms = 4.5, SquareFeet = 4800, LotSizeAcres = 2.0, YearBuilt = 2018,
                Status = "Active", Features = ["Pool", "Outdoor Kitchen", "Hill Country Views", "2 Acres", "3-Car Garage", "Custom Finishes"],
                AgentName = "Maria Torres", AgentPhone = "512-555-0301", ListedDate = DateTime.UtcNow.AddDays(-5)
            },
            new()
            {
                Id = "prop-007", PropertyType = "Condo", Title = "Downtown Austin High-Rise Living",
                Description = "Modern condo in the heart of downtown Austin with skyline views, walking distance to 6th Street and Lady Bird Lake. Building features include rooftop pool, fitness center, and valet parking.",
                Address = "200 Congress Ave #2401", City = "Austin", State = "TX", ZipCode = "78701",
                Price = 585000, Bedrooms = 2, Bathrooms = 2, SquareFeet = 1200, LotSizeAcres = 0, YearBuilt = 2021,
                Status = "Active", Features = ["Skyline Views", "Rooftop Pool", "Fitness Center", "Valet", "Balcony", "Walk to 6th Street"],
                AgentName = "Maria Torres", AgentPhone = "512-555-0301", ListedDate = DateTime.UtcNow.AddDays(-12)
            },
            new()
            {
                Id = "prop-008", PropertyType = "House", Title = "Renovated Bungalow in East Austin",
                Description = "Charming bungalow completely renovated with modern touches while keeping its original character. New roof, HVAC, and plumbing. Large fenced backyard with workshop. Close to East Austin restaurants and bars.",
                Address = "1122 E 6th St", City = "Austin", State = "TX", ZipCode = "78702",
                Price = 525000, Bedrooms = 3, Bathrooms = 2, SquareFeet = 1400, LotSizeAcres = 0.18, YearBuilt = 1945,
                Status = "Pending", Features = ["Renovated", "Fenced Yard", "Workshop", "New Roof", "Original Character"],
                AgentName = "David Kim", AgentPhone = "512-555-0302", ListedDate = DateTime.UtcNow.AddDays(-30)
            },
            new()
            {
                Id = "prop-009", PropertyType = "Townhouse", Title = "Modern Townhouse Near Red Rocks",
                Description = "Contemporary townhome with mountain views, just minutes from Red Rocks Amphitheatre. Open concept with floor-to-ceiling windows, designer finishes, and a private rooftop terrace.",
                Address = "345 Bear Creek Ave", City = "Denver", State = "CO", ZipCode = "80228",
                Price = 620000, Bedrooms = 3, Bathrooms = 2.5, SquareFeet = 2000, LotSizeAcres = 0.05, YearBuilt = 2023,
                Status = "Active", Features = ["Mountain Views", "Rooftop Terrace", "Modern Design", "Near Red Rocks", "2-Car Garage"],
                AgentName = "Rachel Green", AgentPhone = "303-555-0401", ListedDate = DateTime.UtcNow.AddDays(-8)
            },
            new()
            {
                Id = "prop-010", PropertyType = "House", Title = "Historic Victorian in Curtis Park",
                Description = "Beautifully preserved 1890s Victorian in Denver's oldest neighborhood. Features include original woodwork, stained glass windows, wrap-around porch, and a detached carriage house converted to an ADU.",
                Address = "2900 Champa St", City = "Denver", State = "CO", ZipCode = "80205",
                Price = 875000, Bedrooms = 4, Bathrooms = 3, SquareFeet = 3200, LotSizeAcres = 0.2, YearBuilt = 1892,
                Status = "Active", Features = ["Victorian", "ADU/Carriage House", "Wrap-Around Porch", "Stained Glass", "Original Woodwork"],
                AgentName = "Rachel Green", AgentPhone = "303-555-0401", ListedDate = DateTime.UtcNow.AddDays(-15)
            },
            new()
            {
                Id = "prop-011", PropertyType = "Condo", Title = "Tribeca Loft with Designer Finishes",
                Description = "Stunning Tribeca loft with 12-foot ceilings, oversized windows, and premium finishes throughout. Custom Italian kitchen, spa bathroom with soaking tub, and dedicated storage unit. Full-service doorman building.",
                Address = "60 Warren St #8A", City = "New York", State = "NY", ZipCode = "10007",
                Price = 2800000, Bedrooms = 2, Bathrooms = 2, SquareFeet = 1800, LotSizeAcres = 0, YearBuilt = 2017,
                Status = "Active", Features = ["Doorman", "Loft", "Italian Kitchen", "Soaking Tub", "Storage Unit", "High Ceilings"],
                AgentName = "Alexandra Petrov", AgentPhone = "212-555-0501", ListedDate = DateTime.UtcNow.AddDays(-6)
            },
            new()
            {
                Id = "prop-012", PropertyType = "Apartment", Title = "Upper West Side Classic Six",
                Description = "Rare pre-war Classic Six apartment with Central Park views. Features include a formal dining room, original herringbone floors, crown moldings, and a windowed eat-in kitchen. Prestigious co-op building with live-in super.",
                Address = "320 Central Park West #12C", City = "New York", State = "NY", ZipCode = "10025",
                Price = 3200000, Bedrooms = 3, Bathrooms = 2, SquareFeet = 2200, LotSizeAcres = 0, YearBuilt = 1929,
                Status = "Active", Features = ["Central Park Views", "Pre-War", "Formal Dining", "Herringbone Floors", "Doorman"],
                AgentName = "Alexandra Petrov", AgentPhone = "212-555-0501", ListedDate = DateTime.UtcNow.AddDays(-20)
            },
            new()
            {
                Id = "prop-013", PropertyType = "House", Title = "Waterfront Estate on Biscayne Bay",
                Description = "Magnificent waterfront estate with 100 feet of bay frontage, private dock, and infinity pool. Mediterranean-inspired architecture with imported Italian marble, chef's kitchen, and smart home automation throughout.",
                Address = "5800 N Bay Rd", City = "Miami", State = "FL", ZipCode = "33140",
                Price = 8500000, Bedrooms = 6, Bathrooms = 7, SquareFeet = 7500, LotSizeAcres = 0.5, YearBuilt = 2020,
                Status = "Active", Features = ["Waterfront", "Private Dock", "Infinity Pool", "Smart Home", "Italian Marble", "Bay Views"],
                AgentName = "Carlos Mendez", AgentPhone = "305-555-0601", ListedDate = DateTime.UtcNow.AddDays(-4)
            },
            new()
            {
                Id = "prop-014", PropertyType = "Condo", Title = "Brickell City Centre Penthouse",
                Description = "Ultra-luxe penthouse in the heart of Brickell with wraparound terrace and unobstructed city and ocean views. Private elevator, Snaidero kitchen, wine room, and access to world-class amenities.",
                Address = "801 S Miami Ave PH5", City = "Miami", State = "FL", ZipCode = "33130",
                Price = 4200000, Bedrooms = 4, Bathrooms = 4.5, SquareFeet = 3800, LotSizeAcres = 0, YearBuilt = 2022,
                Status = "Active", Features = ["Penthouse", "Ocean Views", "Private Elevator", "Wine Room", "Wraparound Terrace"],
                AgentName = "Carlos Mendez", AgentPhone = "305-555-0601", ListedDate = DateTime.UtcNow.AddDays(-2)
            },
            new()
            {
                Id = "prop-015", PropertyType = "House", Title = "Pearl District Industrial Conversion",
                Description = "Unique single-family home converted from a historic warehouse in Portland's Pearl District. Soaring ceilings, massive windows, and an open industrial aesthetic blended with warm residential touches. Includes a rooftop garden.",
                Address = "1150 NW Everett St", City = "Portland", State = "OR", ZipCode = "97209",
                Price = 1100000, Bedrooms = 3, Bathrooms = 2.5, SquareFeet = 2800, LotSizeAcres = 0.08, YearBuilt = 2016,
                Status = "Active", Features = ["Warehouse Conversion", "Rooftop Garden", "Industrial Design", "High Ceilings", "Pearl District"],
                AgentName = "Emma Wilson", AgentPhone = "503-555-0701", ListedDate = DateTime.UtcNow.AddDays(-11)
            },
            new()
            {
                Id = "prop-016", PropertyType = "House", Title = "Music Row Craftsman with Studio",
                Description = "Charming craftsman bungalow on historic Music Row with a fully equipped recording studio in the detached garage. Original hardwood floors, updated systems, and a large front porch. Walk to Broadway.",
                Address = "1600 Division St", City = "Nashville", State = "TN", ZipCode = "37203",
                Price = 780000, Bedrooms = 3, Bathrooms = 2, SquareFeet = 1900, LotSizeAcres = 0.22, YearBuilt = 1935,
                Status = "Active", Features = ["Recording Studio", "Music Row", "Hardwood Floors", "Front Porch", "Walk to Broadway"],
                AgentName = "Jake Morrison", AgentPhone = "615-555-0801", ListedDate = DateTime.UtcNow.AddDays(-18)
            },
            new()
            {
                Id = "prop-017", PropertyType = "Townhouse", Title = "The Gulch Modern Townhome",
                Description = "Sleek 3-story townhome in Nashville's trendy Gulch neighborhood. Floor-to-ceiling windows, chef's kitchen with waterfall island, private garage, and a rooftop terrace with downtown skyline views.",
                Address = "600 12th Ave S #22", City = "Nashville", State = "TN", ZipCode = "37203",
                Price = 695000, Bedrooms = 3, Bathrooms = 3.5, SquareFeet = 2200, LotSizeAcres = 0.03, YearBuilt = 2024,
                Status = "Active", Features = ["Skyline Views", "Rooftop Terrace", "Chef's Kitchen", "Modern Design", "The Gulch"],
                AgentName = "Jake Morrison", AgentPhone = "615-555-0801", ListedDate = DateTime.UtcNow.AddDays(-9)
            },
            new()
            {
                Id = "prop-018", PropertyType = "House", Title = "Starter Home in Greenwood",
                Description = "Affordable starter home in the up-and-coming Greenwood neighborhood. 2 bedrooms, 1 bath with a large backyard. Great bones with potential for expansion. New water heater and electrical panel.",
                Address = "8800 Greenwood Ave N", City = "Seattle", State = "WA", ZipCode = "98103",
                Price = 450000, Bedrooms = 2, Bathrooms = 1, SquareFeet = 950, LotSizeAcres = 0.12, YearBuilt = 1952,
                Status = "Active", Features = ["Starter Home", "Large Backyard", "Updated Electrical", "Expansion Potential"],
                AgentName = "Sarah Chen", AgentPhone = "206-555-0101", ListedDate = DateTime.UtcNow.AddDays(-25)
            },
            new()
            {
                Id = "prop-019", PropertyType = "Condo", Title = "LoDo Micro-Loft Near Union Station",
                Description = "Efficient and stylish micro-loft in Denver's Lower Downtown. Perfect for urban professionals. Building features co-working space, bike storage, and rooftop lounge. Steps from Union Station transit.",
                Address = "1600 Wewatta St #407", City = "Denver", State = "CO", ZipCode = "80202",
                Price = 325000, Bedrooms = 1, Bathrooms = 1, SquareFeet = 550, LotSizeAcres = 0, YearBuilt = 2022,
                Status = "Active", Features = ["Micro-Loft", "Co-Working Space", "Bike Storage", "Rooftop Lounge", "Near Transit"],
                AgentName = "Rachel Green", AgentPhone = "303-555-0401", ListedDate = DateTime.UtcNow.AddDays(-16)
            },
            new()
            {
                Id = "prop-020", PropertyType = "House", Title = "Coral Gables Mediterranean Villa",
                Description = "Classic Coral Gables Mediterranean villa with barrel tile roof, arched doorways, and lush tropical landscaping. Updated with impact windows, new pool, and a summer kitchen. Top-rated school district.",
                Address = "1200 Alhambra Cir", City = "Miami", State = "FL", ZipCode = "33134",
                Price = 2100000, Bedrooms = 5, Bathrooms = 4, SquareFeet = 3800, LotSizeAcres = 0.3, YearBuilt = 1948,
                Status = "Pending", Features = ["Mediterranean", "Pool", "Impact Windows", "Summer Kitchen", "Top Schools", "Tropical Landscaping"],
                AgentName = "Carlos Mendez", AgentPhone = "305-555-0601", ListedDate = DateTime.UtcNow.AddDays(-35)
            }
        ];
    }
}
