using Microsoft.EntityFrameworkCore;
using EduLearn.AuthService.Data;
using EduLearn.AuthService.Services;
using EduLearn.AuthService.Repositories;
using EduLearn.AuthService.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.ConfigureHttpJsonOptions(options => {
    options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token.",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Add JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                builder.Configuration["Jwt:SecretKey"] ?? throw new InvalidOperationException("JWT Secret Key not configured"))),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("ADMIN"));
    options.AddPolicy("InstructorOrAdmin", policy => policy.RequireRole("INSTRUCTOR", "ADMIN"));
    options.AddPolicy("Authenticated", policy => policy.RequireAuthenticatedUser());
});

// Add DbContext
builder.Services.AddDbContext<EduLearn.AuthService.Data.AuthDbContext>(options =>
{
    var dbHost = builder.Configuration["DB_HOST"];
    var dbPort = builder.Configuration["DB_PORT"] ?? "5432";
    var dbName = builder.Configuration["DB_NAME"];
    var dbUser = builder.Configuration["DB_USER"];
    var dbPass = builder.Configuration["DB_PASSWORD"];

    string? connectionString = null;

    if (!string.IsNullOrWhiteSpace(dbHost) && !string.IsNullOrWhiteSpace(dbName))
    {
        connectionString = $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPass};SSL Mode=Require;Trust Server Certificate=true;Include Error Detail=true";
    }
    else
    {
        connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
                         ?? builder.Configuration["DefaultConnection"];
    }

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        Console.WriteLine("Warning: No database connection information found. Falling back to local SQLite.");
        options.UseSqlite("Data Source=auth_fallback.db");
    }
    else if (connectionString.Contains("Data Source") || connectionString.Contains(".db"))
    {
        options.UseSqlite(connectionString.Trim());
    }
    else
    {
        options.UseNpgsql(connectionString.Trim(), x => x.MigrationsHistoryTable("__AuthMigrationsHistory"));
    }
});

// Add Repository
builder.Services.AddScoped<IUserRepository, UserRepository>();

// Add AuthService
builder.Services.AddScoped<IAuthService, AuthService>();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        builder => builder
            .WithOrigins(
                "http://localhost:4200", 
                "http://localhost:60804",
                "https://edulearn-frontend-9lw4.onrender.com",
                "https://edulearn-frontend.onrender.com",
                "https://edulearn-frontends.onrender.com",
                "https://edulearn-frontend-zn5e.onrender.com"
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials());
});

var app = builder.Build();

// Initialize database
try
{
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<EduLearn.AuthService.Data.AuthDbContext>();
        Console.WriteLine("Applying migrations...");
        
        Console.WriteLine("Applying migrations...");
        
        // Final Forced Reset: Wipe the entire public schema to clear any broken/conflicting tables
        // This is safe since it's a new production database and we need a clean start for all 8 microservices.
        try {
            Console.WriteLine("Nuclear Reset: Dropping public schema...");
            dbContext.Database.ExecuteSqlRaw("DROP SCHEMA public CASCADE; CREATE SCHEMA public;");
            Console.WriteLine("Public schema reset successfully.");
        } catch (Exception resetEx) { 
            Console.WriteLine($"Nuclear Reset Warning: {resetEx.Message}");
        }

        dbContext.Database.Migrate();
        Console.WriteLine("Database initialized successfully with fresh tables.");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Critical Error: Database initialization failed: {ex.Message}");
    // We allow the app to continue so we can see health check logs, but it might fail later
}

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "AuthService API V1");
    c.RoutePrefix = "swagger";
});

app.UseCors("AllowFrontend");
app.UseDeveloperExceptionPage(); // Temporary for debugging 500 errors in production
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseAuthentication();
app.UseAuthorization();

// Authentication endpoints
app.MapPost("/api/auth/register", async (RegisterRequest request, IAuthService authService) =>
{
    try 
    {
        var result = await authService.RegisterAsync(request);
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        var errorDetail = ex.InnerException != null ? $"{ex.Message} | Inner: {ex.InnerException.Message}" : ex.Message;
        errorDetail += " | [V2-NUCLEAR]";
        Console.WriteLine($"Registration Error: {ex}");
        return Results.Problem(detail: errorDetail, title: "Registration Failed", statusCode: 500);
    }
})
.WithName("Register")
.WithOpenApi();

app.MapPost("/api/auth/login", async (LoginRequest request, IAuthService authService) =>
{
    var result = await authService.LoginAsync(request);
    if (!result.Success)
    {
        return Results.Unauthorized();
    }
    return Results.Ok(result);
})
.WithName("Login")
.WithOpenApi();

// User management endpoints
app.MapPost("/api/users/logout", async (Guid userId, IAuthService authService) =>
{
    var result = await authService.LogoutAsync(userId);
    return Results.Ok(result);
})
.RequireAuthorization("Authenticated")
.WithName("Logout")
.WithOpenApi();

app.MapGet("/api/users/{userId}", async (Guid userId, HttpContext context, IAuthService authService) =>
{
    var currentUserIdClaim = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (Guid.TryParse(currentUserIdClaim, out Guid currentUserId))
    {
        // Users can view their own profile, Admins can view any
        var userRole = context.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        if (currentUserId != userId && userRole != "ADMIN")
        {
            return Results.Forbid();
        }
    }
    var result = await authService.GetUserByIdAsync(userId);
    return result != null ? Results.Ok(result) : Results.NotFound();
})
.RequireAuthorization("Authenticated")
.WithName("GetUserById")
.WithOpenApi();

app.MapPut("/api/users/{userId}/profile", async (Guid userId, UpdateProfileRequest request, HttpContext context, IAuthService authService) =>
{
    var currentUserIdClaim = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (Guid.TryParse(currentUserIdClaim, out Guid currentUserId))
    {
        // Users can update their own profile, Admins can update any
        var userRole = context.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        if (currentUserId != userId && userRole != "ADMIN")
        {
            return Results.Forbid();
        }
    }
    var result = await authService.UpdateProfileAsync(userId, request);
    return Results.Ok(result);
})
.RequireAuthorization("Authenticated")
.WithName("UpdateProfile")
.WithOpenApi();

app.MapPut("/api/users/{userId}/password", async (Guid userId, ChangePasswordRequest request, HttpContext context, IAuthService authService) =>
{
    var currentUserIdClaim = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (Guid.TryParse(currentUserIdClaim, out Guid currentUserId))
    {
        // Users can only change their own password
        if (currentUserId != userId)
        {
            return Results.Forbid();
        }
    }
    var result = await authService.ChangePasswordAsync(userId, request);
    return Results.Ok(result);
})
.RequireAuthorization("Authenticated")
.WithName("ChangePassword")
.WithOpenApi();

app.MapGet("/api/users/role/{role}", async (UserRoleType role, IAuthService authService) =>
{
    var result = await authService.GetAllByRoleAsync(role);
    return Results.Ok(result);
})
.RequireAuthorization("AdminOnly")
.WithName("GetAllByRole")
.WithOpenApi();

app.MapPut("/api/users/{userId}/deactivate", async (Guid userId, IAuthService authService) =>
{
    var result = await authService.DeactivateAccountAsync(userId);
    return Results.Ok(result);
})
.RequireAuthorization("AdminOnly")
.WithName("DeactivateAccount")
.WithOpenApi();

app.MapPut("/api/users/{userId}/status", async (Guid userId, HttpContext context, IAuthService authService) =>
{
    var currentUserIdClaim = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (Guid.TryParse(currentUserIdClaim, out Guid currentUserId))
    {
        var userRole = context.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        if (userRole != "ADMIN")
        {
            return Results.Forbid();
        }
    }
    
    // Read the request body to get isActive status
    using var reader = new StreamReader(context.Request.Body);
    var body = await reader.ReadToEndAsync();
    var requestData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(body);
    bool isActive = requestData.TryGetValue("isActive", out var isActiveValue) && 
                   bool.TryParse(isActiveValue.ToString(), out var parsedValue) && parsedValue;
    
    var result = await authService.UpdateUserStatusAsync(userId, isActive);
    return Results.Ok(result);
})
.RequireAuthorization("AdminOnly")
.WithName("UpdateUserStatus")
.WithOpenApi();

app.MapDelete("/api/users/{userId}", async (Guid userId, HttpContext context, IAuthService authService) =>
{
    var currentUserIdClaim = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (Guid.TryParse(currentUserIdClaim, out Guid currentUserId))
    {
        var userRole = context.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        if (userRole != "ADMIN")
        {
            return Results.Forbid();
        }
    }
    
    var result = await authService.DeleteUserAsync(userId);
    return Results.Ok(result);
})
.RequireAuthorization("AdminOnly")
.WithName("DeleteUser")
.WithOpenApi();

app.MapPut("/api/users/{userId}/reactivate", async (Guid userId, IAuthService authService) =>
{
    var result = await authService.ReactivateAccountAsync(userId);
    return Results.Ok(result);
})
.RequireAuthorization("AdminOnly")
.WithName("ReactivateAccount")
.WithOpenApi();

app.MapGet("/api/users/recent/{limit}", async (int limit, IAuthService authService) =>
{
    // Get all users and return the most recent ones
    var instructors = await authService.GetAllByRoleAsync(UserRoleType.INSTRUCTOR);
    var students = await authService.GetAllByRoleAsync(UserRoleType.STUDENT);
    
    var allUsers = new List<object>();
    allUsers.AddRange(instructors.Select(u => new { 
        Id = u.Id, FullName = u.FullName, Email = u.Email, Role = u.Role.ToString(), IsActive = u.IsActive, CreatedAt = u.CreatedAt 
    }));
    allUsers.AddRange(students.Select(u => new { 
        Id = u.Id, FullName = u.FullName, Email = u.Email, Role = u.Role.ToString(), IsActive = u.IsActive, CreatedAt = u.CreatedAt 
    }));
    
    var recentUsers = allUsers
        .OrderByDescending(u => ((dynamic)u).CreatedAt)
        .Take(limit)
        .ToList();
    
    return Results.Ok(new { users = recentUsers });
})
.RequireAuthorization("AdminOnly")
.WithName("GetRecentUsers")
.WithOpenApi();

app.MapGet("/api/users/search/{keyword}", async (string keyword, IAuthService authService) =>
{
    var result = await authService.SearchUsersAsync(keyword);
    return Results.Ok(result);
})
.RequireAuthorization("InstructorOrAdmin")
.WithName("SearchUsers")
.WithOpenApi();

app.MapPost("/api/auth/validate", async (string token, IAuthService authService) =>
{
    var result = await authService.ValidateTokenAsync(token);
    return Results.Ok(result);
})
.WithName("ValidateToken")
.WithOpenApi();

// Health check endpoint
app.MapGet("/api/auth/health", () =>
{
    return Results.Ok(new { status = "Healthy", service = "AuthService", timestamp = DateTime.UtcNow });
})
.WithName("HealthCheck")
.WithOpenApi();

app.Run();
