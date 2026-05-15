using EduLearn.QuizService.Data;
using EduLearn.QuizService.Models;
using EduLearn.QuizService.Repositories;
using EduLearn.QuizService.Services;
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
builder.Services.AddDbContext<QuizDbContext>(options =>
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
        options.UseSqlite("Data Source=quiz_fallback.db");
    }
    else if (connectionString.Contains("Data Source") || connectionString.Contains(".db"))
    {
        options.UseSqlite(connectionString.Trim());
    }
    else
    {
        options.UseNpgsql(connectionString.Trim(), x => x.MigrationsHistoryTable("__QuizMigrationsHistory"));
    }
});

// Add Repository
builder.Services.AddScoped<IQuizRepository, QuizRepository>();

// Add Service
builder.Services.AddScoped<IQuizService, QuizService>();

var app = builder.Build();

// Initialize database
try
{
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<QuizDbContext>();
        // Nuclear Reset: Drop ALL tables in the public schema using a PostgreSQL-specific block
        try {
            Console.WriteLine("Force Reset: Wiping all tables in public schema...");
            dbContext.Database.ExecuteSqlRaw(@"
                DO $$ DECLARE
                    r RECORD;
                BEGIN
                    FOR r IN (SELECT tablename FROM pg_tables WHERE schemaname = 'public') LOOP
                        EXECUTE 'DROP TABLE IF EXISTS ' || quote_ident(r.tablename) || ' CASCADE';
                    END LOOP;
                END $$;");
            Console.WriteLine("Database wipe successful.");
        } catch (Exception ex) { 
            Console.WriteLine($"Reset Warning: {ex.Message}");
        }

        Console.WriteLine("Applying schema (EnsureCreated)...");
        try {
            dbContext.Database.EnsureCreated();
            Console.WriteLine("Database initialized successfully.");
        } catch (Exception dbEx) {
            Console.WriteLine($"CRITICAL: Database initialization failed: {dbEx.Message}");
            if (dbEx.InnerException != null) 
                Console.WriteLine($"INNER ERROR: {dbEx.InnerException.Message}");
            throw; 
        }
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
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "QuizService API V1");
    c.RoutePrefix = "swagger";
});

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

// Quiz endpoints
app.MapPost("/api/quizzes", async (CreateQuizRequest request, IQuizService quizService) =>
{
    var result = await quizService.CreateQuizAsync(request);
    return Results.Ok(result);
})
.RequireAuthorization("InstructorOrAdmin")
.WithName("CreateQuiz")
.WithOpenApi();

app.MapGet("/api/quizzes/{id}", async (Guid id, IQuizService quizService) =>
{
    var result = await quizService.GetQuizByIdAsync(id);
    return result.Success ? Results.Ok(result) : Results.NotFound(result);
})
.RequireAuthorization("Authenticated")
.WithName("GetQuizById")
.WithOpenApi();

app.MapGet("/api/quizzes/course/{courseId}", async (Guid courseId, IQuizService quizService) =>
{
    var result = await quizService.GetQuizzesByCourseAsync(courseId);
    return Results.Ok(result);
})
.RequireAuthorization("Authenticated")
.WithName("GetQuizzesByCourse")
.WithOpenApi();

app.MapGet("/api/quizzes/lesson/{lessonId}", async (Guid lessonId, IQuizService quizService) =>
{
    var result = await quizService.GetQuizByLessonAsync(lessonId);
    return Results.Ok(result);
})
.RequireAuthorization("Authenticated")
.WithName("GetQuizByLesson")
.WithOpenApi();

app.MapPut("/api/quizzes/{id}", async (Guid id, UpdateQuizRequest request, IQuizService quizService) =>
{
    var result = await quizService.UpdateQuizAsync(id, request);
    return result.Success ? Results.Ok(result) : Results.NotFound(result);
})
.RequireAuthorization("InstructorOrAdmin")
.WithName("UpdateQuiz")
.WithOpenApi();

app.MapDelete("/api/quizzes/{id}", async (Guid id, IQuizService quizService) =>
{
    var result = await quizService.DeleteQuizAsync(id);
    return result.Success ? Results.Ok(result) : Results.NotFound(result);
})
.RequireAuthorization("InstructorOrAdmin")
.WithName("DeleteQuiz")
.WithOpenApi();

app.MapPut("/api/quizzes/{id}/publish", async (Guid id, IQuizService quizService) =>
{
    var result = await quizService.PublishQuizAsync(id);
    return result.Success ? Results.Ok(result) : Results.NotFound(result);
})
.RequireAuthorization("InstructorOrAdmin")
.WithName("PublishQuiz")
.WithOpenApi();

app.MapPost("/api/quizzes/attempt/start", async (HttpContext context, StartAttemptRequest request, IQuizService quizService, ILogger<Program> logger) =>
{
    logger.LogInformation("User authenticated: {IsAuthenticated}", context.User.Identity?.IsAuthenticated);
    foreach (var claim in context.User.Claims)
    {
        logger.LogInformation("Claim: {Type} = {Value}", claim.Type, claim.Value);
    }
    
    var currentUserIdClaim = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    logger.LogInformation("Found nameid claim: {Claim}", currentUserIdClaim);
    
    if (!Guid.TryParse(currentUserIdClaim, out Guid studentId))
    {
        logger.LogWarning("Failed to parse user ID from claim: {Claim}", currentUserIdClaim);
        return Results.Unauthorized();
    }

    var result = await quizService.StartAttemptAsync(studentId, request);
    return Results.Ok(result);
})
.RequireAuthorization("Authenticated")
.WithName("StartAttempt")
.WithOpenApi();

app.MapPut("/api/quizzes/attempt/{id}/submit", async (Guid id, SubmitAttemptRequest request, IQuizService quizService) =>
{
    var result = await quizService.SubmitAttemptAsync(id, request);
    return result.Success ? Results.Ok(result) : Results.NotFound(result);
})
.RequireAuthorization("Authenticated")
.WithName("SubmitAttempt")
.WithOpenApi();

app.MapGet("/api/quizzes/attempts/{studentId}/{quizId}", async (Guid studentId, Guid quizId, HttpContext context, IQuizService quizService) =>
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

    var result = await quizService.GetAttemptsByStudentAsync(studentId, quizId);
    return Results.Ok(result);
})
.RequireAuthorization("Authenticated")
.WithName("GetAttemptsByStudent")
.WithOpenApi();

app.MapGet("/api/quizzes/best-attempt/{studentId}/{quizId}", async (Guid studentId, Guid quizId, HttpContext context, IQuizService quizService) =>
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

    var result = await quizService.GetBestAttemptAsync(studentId, quizId);
    return Results.Ok(result);
})
.RequireAuthorization("Authenticated")
.WithName("GetBestAttempt")
.WithOpenApi();

app.MapGet("/api/quizzes/attempt-count/{studentId}/{quizId}", async (Guid studentId, Guid quizId, HttpContext context, IQuizService quizService) =>
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

    var result = await quizService.GetAttemptCountAsync(studentId, quizId);
    return Results.Ok(result);
})
.RequireAuthorization("Authenticated")
.WithName("GetAttemptCount")
.WithOpenApi();

// Health check endpoint
app.MapGet("/api/quizzes/health", () =>
{
    return Results.Ok(new { status = "Healthy", service = "QuizService", timestamp = DateTime.UtcNow });
})
.WithName("HealthCheck")
.WithOpenApi();

app.Run();
