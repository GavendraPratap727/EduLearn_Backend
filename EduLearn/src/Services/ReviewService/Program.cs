using EduLearn.ReviewService.Data;
using EduLearn.ReviewService.Models;
using EduLearn.ReviewService.Repositories;
using EduLearn.ReviewService.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.ConfigureHttpJsonOptions(options => {
    options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});

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
builder.Services.AddDbContext<ReviewDbContext>(options =>
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
        options.UseSqlite("Data Source=review_fallback.db");
    }
    else if (connectionString.Contains("Data Source") || connectionString.Contains(".db"))
    {
        options.UseSqlite(connectionString.Trim());
    }
    else
    {
        options.UseNpgsql(connectionString.Trim(), x => x.MigrationsHistoryTable("__ReviewMigrationsHistory"));
    }
});

// Add Repository
builder.Services.AddScoped<IReviewRepository, ReviewRepository>();

// Add HttpClient
builder.Services.AddHttpClient<IReviewService, ReviewService>();

// Add Service
builder.Services.AddScoped<IReviewService, ReviewService>();

var app = builder.Build();

// Initialize database
try
{
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<ReviewDbContext>();
        Console.WriteLine("Applying migrations...");
        
        try {
            dbContext.Database.ExecuteSqlRaw("DROP TABLE IF EXISTS \"Reviews\" CASCADE;");
            dbContext.Database.ExecuteSqlRaw("DROP TABLE IF EXISTS \"__EFMigrationsHistory\" CASCADE;");
        } catch { }

        dbContext.Database.Migrate();
        Console.WriteLine("Database initialized successfully.");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Critical Error: Database initialization failed: {ex.Message}");
}

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "ReviewService API V1");
    c.RoutePrefix = "swagger";
});

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

// Review endpoints
app.MapPost("/api/reviews", async (HttpContext context, CreateReviewRequest request, IReviewService reviewService) =>
{
    var currentUserIdClaim = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (!Guid.TryParse(currentUserIdClaim, out Guid studentId))
    {
        return Results.Unauthorized();
    }

    var result = await reviewService.AddReviewAsync(studentId, request);
    return Results.Ok(result);
})
.RequireAuthorization("Authenticated")
.WithName("AddReview")
.WithOpenApi();

app.MapGet("/api/reviews/{id}", async (Guid id, IReviewService reviewService) =>
{
    var result = await reviewService.GetReviewByIdAsync(id);
    return result.Success ? Results.Ok(result) : Results.NotFound(result);
})
.RequireAuthorization("Authenticated")
.WithName("GetReviewById")
.WithOpenApi();

app.MapGet("/api/reviews/course/{courseId}", async (Guid courseId, bool approvedOnly, IReviewService reviewService) =>
{
    var result = await reviewService.GetReviewsByCourseAsync(courseId, approvedOnly);
    return Results.Ok(result);
})
.RequireAuthorization("Authenticated")
.WithName("GetReviewsByCourse")
.WithOpenApi();

app.MapGet("/api/reviews/course/{courseId}/approved", async (Guid courseId, IReviewService reviewService) =>
{
    var result = await reviewService.GetApprovedReviewsAsync(courseId);
    return Results.Ok(result);
})
.RequireAuthorization("Authenticated")
.WithName("GetApprovedReviews")
.WithOpenApi();

app.MapGet("/api/reviews/student/{studentId}", async (Guid studentId, HttpContext context, IReviewService reviewService) =>
{
    var currentUserIdClaim = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (Guid.TryParse(currentUserIdClaim, out Guid currentUserId))
    {
        var userRole = context.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        if (currentUserId != studentId && userRole != "ADMIN" && userRole != "INSTRUCTOR")
        {
            return Results.Forbid();
        }
    }

    var result = await reviewService.GetReviewsByStudentAsync(studentId);
    return Results.Ok(result);
})
.RequireAuthorization("Authenticated")
.WithName("GetReviewsByStudent")
.WithOpenApi();

app.MapPut("/api/reviews/{id}", async (Guid id, HttpContext context, UpdateReviewRequest request, IReviewService reviewService) =>
{
    var currentUserIdClaim = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (!Guid.TryParse(currentUserIdClaim, out Guid studentId))
    {
        return Results.Unauthorized();
    }

    var result = await reviewService.UpdateReviewAsync(id, studentId, request);
    return result.Success ? Results.Ok(result) : Results.NotFound(result);
})
.RequireAuthorization("Authenticated")
.WithName("UpdateReview")
.WithOpenApi();

app.MapPut("/api/reviews/{id}/approve", async (Guid id, IReviewService reviewService) =>
{
    var result = await reviewService.ApproveReviewAsync(id);
    return result.Success ? Results.Ok(result) : Results.NotFound(result);
})
.RequireAuthorization("AdminOnly")
.WithName("ApproveReview")
.WithOpenApi();

app.MapDelete("/api/reviews/{id}", async (Guid id, HttpContext context, IReviewService reviewService) =>
{
    var currentUserIdClaim = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (!Guid.TryParse(currentUserIdClaim, out Guid studentId))
    {
        return Results.Unauthorized();
    }

    var result = await reviewService.DeleteReviewAsync(id, studentId);
    return result.Success ? Results.Ok(result) : Results.NotFound(result);
})
.RequireAuthorization("Authenticated")
.WithName("DeleteReview")
.WithOpenApi();

app.MapGet("/api/reviews/average/{courseId}", async (Guid courseId, IReviewService reviewService) =>
{
    var result = await reviewService.GetAverageRatingAsync(courseId);
    return Results.Ok(result);
})
.RequireAuthorization("Authenticated")
.WithName("GetAverageRating")
.WithOpenApi();

app.MapGet("/api/reviews/count/{courseId}", async (Guid courseId, IReviewService reviewService) =>
{
    var count = await reviewService.GetReviewCountAsync(courseId);
    return Results.Ok(new { Success = true, Count = count });
})
.RequireAuthorization("Authenticated")
.WithName("GetReviewCount")
.WithOpenApi();

app.MapGet("/api/reviews/hasReviewed/{studentId}/{courseId}", async (Guid studentId, Guid courseId, HttpContext context, IReviewService reviewService) =>
{
    var currentUserIdClaim = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (Guid.TryParse(currentUserIdClaim, out Guid currentUserId))
    {
        var userRole = context.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        if (currentUserId != studentId && userRole != "ADMIN")
        {
            return Results.Forbid();
        }
    }

    var result = await reviewService.HasStudentReviewedAsync(studentId, courseId);
    return Results.Ok(result);
})
.RequireAuthorization("Authenticated")
.WithName("HasStudentReviewed")
.WithOpenApi();

// Health check endpoint
app.MapGet("/api/reviews/health", () =>
{
    return Results.Ok(new { status = "Healthy", service = "ReviewService", timestamp = DateTime.UtcNow });
})
.WithName("HealthCheck")
.WithOpenApi();

app.Run();
