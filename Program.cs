using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using UserManagementAPI.Middlewares;
using UserManagementAPI.Models;

var builder = WebApplication.CreateBuilder(args);

// Read JWT key from configuration
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrEmpty(jwtKey))
    throw new InvalidOperationException("JWT key is not configured. Please set 'Jwt:Key' in configuration.");
var fixedToken = builder.Configuration["Jwt:FixedToken"];

var tokenBlacklist = new ConcurrentDictionary<string, DateTime>();

// Add services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "UserManagementAPI", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer' followed by your JWT token."
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
            new string[] {}
        }
    });
});

// Add JWT authentication services
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                var token = context.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                if (tokenBlacklist.ContainsKey(token))
                {
                    context.Fail("Token has been revoked.");
                }
                return Task.CompletedTask;
            }
        };
    });
// Add authorization services
builder.Services.AddAuthorization();

var app = builder.Build();

// Global exception handler
app.UseExceptionHandler(appError =>
{
    appError.Run(async context =>
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";
        logger.LogError("Unhandled exception occurred while processing request: {Path}", context.Request.Path);
        var errorResponse = new { error = "Internal server error." };
        await context.Response.WriteAsJsonAsync(errorResponse);
    });
});

// Enable Swagger middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Use custom middleware for logging requests and responses
app.UseMiddleware<RequestResponseLoggingMiddleware>();

// Use Authentication and Authorization middlewares
app.UseAuthentication();
app.UseAuthorization();


// In-memory user store
var users = new ConcurrentDictionary<int, User>();
users.TryAdd(1, new User(1, "Alice", 30, "alice@example.com"));
users.TryAdd(2, new User(2, "Bob", 25, "bob@example.com"));

// JWT token generator
string GenerateJwtToken(string username)
{
    var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
    var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

    var claims = new[]
    {
        new Claim(ClaimTypes.Name, username),
        new Claim(ClaimTypes.Role, "User")
    };

    var token = new JwtSecurityToken(
        issuer: "UserManagementAPI",
        audience: "admin",
        claims: claims,
        expires: DateTime.Now.AddHours(1),
        signingCredentials: credentials);

    return new JwtSecurityTokenHandler().WriteToken(token);
}

// Login endpoint
app.MapPost("/login", (string username) =>
{
    return Results.Ok(new { token = GenerateJwtToken(username) });
});

// Logout endpoint

app.MapPost("/logout", (HttpContext context) =>
{
    var token = context.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
    if (!string.IsNullOrEmpty(token))
    {
        tokenBlacklist.TryAdd(token, DateTime.UtcNow);
    }

    var username = context.User.Identity?.Name ?? "Unknown";
    return Results.Ok(new { message = $"User {username} logged out successfully." });
}).RequireAuthorization();


// Fixed token endpoint
app.MapGet("/fixed-token", () =>
{
    return Results.Ok(new { token = fixedToken });
});


// User management API endpoints all requiring authorization

app.MapPost("/users", (User user) =>
{
    if (!User.IsValid(user, out var errors))
        return Results.BadRequest(errors);

    if (!users.TryAdd(user.UserId, user))
        return Results.Conflict("User already exists.");

    return Results.Created($"/users/{user.UserId}", user);
}).RequireAuthorization();

app.MapGet("/users/{id:int}", (int id) =>
{
    return users.TryGetValue(id, out var user)
        ? Results.Ok(user)
        : Results.NotFound($"User with ID {id} not found.");
}).RequireAuthorization();

app.MapGet("/users", () =>
{
    return Results.Ok(users.Values.ToList());
}).RequireAuthorization();

app.MapPut("/users/{id:int}", (int id, User updatedUser) =>
{
    if (!User.IsValid(updatedUser, out var errors))
        return Results.BadRequest(errors);

    if (!users.TryGetValue(id, out var existingUser))
        return Results.NotFound($"User with ID {id} not found.");

    updatedUser.UserId = id;

    if (!users.TryUpdate(id, updatedUser, existingUser))
        return Results.Conflict("Failed to update user due to a concurrency conflict.");

    return Results.NoContent();
}).RequireAuthorization();

app.MapDelete("/users/{id:int}", (int id) =>
{
    return users.TryRemove(id, out _)
        ? Results.NoContent()
        : Results.NotFound($"User with ID {id} not found.");
}).RequireAuthorization();

app.Run();

