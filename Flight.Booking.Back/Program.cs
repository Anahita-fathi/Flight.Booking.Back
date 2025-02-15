var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// Add CORS services
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});
var app = builder.Build();

// Use CORS middleware
app.UseCors("AllowAll");
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Trip data
var trips = new[]
{
    new { id = 1, origin = "تهران", destination = "مشهد", date = "2023-10-15", time = "10:00", price = 500000 },
    new { id = 2, origin = "تهران", destination = "شیراز", date = "2023-10-15", time = "12:00", price = 600000 },
    new { id = 3, origin = "تهران", destination = "اصفهان", date = "2023-10-15", time = "14:00", price = 450000 },
    new { id = 4, origin = "مشهد", destination = "تهران", date = "2023-10-16", time = "08:00", price = 550000 },
    new { id = 5, origin = "شیراز", destination = "تهران", date = "2023-10-16", time = "16:00", price = 650000 }
};

// Define the endpoint to return the trips data with filtering using LINQ
app.MapGet("/flights", (string? origin, string? destination, string? date) =>
    {
        var filteredTrips = trips.AsQueryable(); // Convert to IQueryable for LINQ

        if (!string.IsNullOrEmpty(origin))
        {
            filteredTrips = filteredTrips.Where(t => t.origin.Contains(origin, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrEmpty(destination))
        {
            filteredTrips =
                filteredTrips.Where(t => t.destination.Contains(destination, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrEmpty(date))
        {
            filteredTrips = filteredTrips.Where(t => t.date == date);
        }

        return filteredTrips.ToList();
    })
    .WithName("GetFlights")
    .WithOpenApi();

app.Run();