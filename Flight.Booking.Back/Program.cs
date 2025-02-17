using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Flight.Booking.Back;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

// Add JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.ASCII.GetBytes(jwtSettings["Key"]);

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ClockSkew = TimeSpan.FromMinutes(5)
        };
    });

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer' followed by your token (e.g., 'Bearer YOUR_TOKEN_HERE')."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] { }
        }
    });
});
builder.Services.AddAuthorization();
builder.Logging.AddConsole();

var app = builder.Build();

// Use CORS middleware
app.UseCors("AllowAll");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Required for JWT
app.UseAuthentication();
app.UseAuthorization();

// Trip data
var trips = new[]
{
    new { id = 1, origin = "تهران", destination = "مشهد", date = "2023-10-15", time = "10:00", price = 500000 },
    new { id = 2, origin = "تهران", destination = "شیراز", date = "2023-10-15", time = "12:00", price = 600000 },
    new { id = 3, origin = "تهران", destination = "اصفهان", date = "2023-10-15", time = "14:00", price = 450000 },
    new { id = 4, origin = "مشهد", destination = "تهران", date = "2023-10-16", time = "08:00", price = 550000 },
    new { id = 5, origin = "شیراز", destination = "تهران", date = "2023-10-16", time = "16:00", price = 650000 }
};
app.MapPost("/login", async (LoginRequest loginRequest) =>
{
    if (loginRequest.Username == "admin" && loginRequest.Password == "password")
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, loginRequest.Username)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.Now.AddMinutes(Convert.ToDouble(jwtSettings["ExpiryInMinutes"])),
            signingCredentials: creds
        );

        return Results.Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token) });
    }

    return Results.Unauthorized();
});


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
    .WithOpenApi()
    .RequireAuthorization();


app.MapPost("/reserve", (ReserveRequest reserveRequest) =>
{
    var reservations = ReservationFileHelper.GetReservations();
    var flight = trips.FirstOrDefault(t => t.id == reserveRequest.FlightId);
    if (flight == null)
    {
        return Results.BadRequest(new { message = "Flight not found!" });
    }

    var reservation = new Reservation
    {
        Id = reservations.Count + 1,
        Username = reserveRequest.Username,
        FlightId = reserveRequest.FlightId,
        ReservationDate = DateTime.Now,
        Status = "Reserved"
    };

    reservations.Add(reservation);
    ReservationFileHelper.SaveReservations(reservations);
    return Results.Ok(new { message = "Flight reserved successfully!" });
}).RequireAuthorization();

app.MapGet("/reservations", (string username) =>
{
    var reservations = ReservationFileHelper.GetReservations();
    var userReservations = reservations.Where(r => r.Username == username).ToList();
    if (!userReservations.Any())
    {
        return Results.Ok(new { message = "No reservations found." });
    }

    return Results.Ok(userReservations);
}).RequireAuthorization();


app.Run();

public class LoginRequest
{
    public string Username { get; set; }
    public string Password { get; set; }
}

public class ReserveRequest
{
    public string Username { get; set; }
    public int FlightId { get; set; }
}