using EduLearn.PaymentService.Data;
using EduLearn.PaymentService.Dtos;
using EduLearn.PaymentService.Repositories;
using EduLearn.PaymentService.Services;
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
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Please enter a valid token",
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "bearer"
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
builder.Services.AddDbContext<PaymentDbContext>(options =>
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
        options.UseSqlite("Data Source=payment_fallback.db");
    }
    else if (connectionString.Contains("Data Source") || connectionString.Contains(".db"))
    {
        options.UseSqlite(connectionString.Trim());
    }
    else
    {
        options.UseNpgsql(connectionString.Trim(), x => x.MigrationsHistoryTable("__PaymentMigrationsHistory"));
    }
});

// Add Repository
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();

// Add HttpClient
builder.Services.AddHttpClient<IPaymentService, PaymentService>();

// Add Services
builder.Services.AddScoped<IRazorpayService, RazorpayService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();

// Add RabbitMQ Service
builder.Services.AddSingleton<IRabbitMqService, RabbitMqService>();

var app = builder.Build();

// Initialize database
try
{
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
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
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "PaymentService API V1");
    c.RoutePrefix = "swagger";
});

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

// Payment endpoints
app.MapPost("/api/payments/create-order", async (HttpContext context, CreatePaymentOrderRequest request, IPaymentService paymentService) =>
{
    var currentUserIdClaim = context.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
    if (!Guid.TryParse(currentUserIdClaim, out Guid studentId))
    {
        return Results.Unauthorized();
    }

    var result = await paymentService.CreatePaymentOrderAsync(studentId, request);
    return result.Success ? Results.Ok(result) : Results.BadRequest(result);
})
.RequireAuthorization("Authenticated")
.WithName("CreatePaymentOrder");

app.MapPost("/api/payments/verify", async (HttpContext context, VerifyPaymentRequest request, IPaymentService paymentService) =>
{
    var currentUserIdClaim = context.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
    if (!Guid.TryParse(currentUserIdClaim, out Guid studentId))
    {
        return Results.Unauthorized();
    }

    var result = await paymentService.VerifyPaymentAsync(studentId, request);
    return result.Success ? Results.Ok(result) : Results.BadRequest(result);
})
.RequireAuthorization("Authenticated")
.WithName("VerifyPayment");

app.MapGet("/api/payments/{id}", async (Guid id, HttpContext context, IPaymentService paymentService) =>
{
    var currentUserIdClaim = context.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
    if (!Guid.TryParse(currentUserIdClaim, out Guid studentId))
    {
        return Results.Unauthorized();
    }

    var result = await paymentService.GetPaymentStatusAsync(id, studentId);
    return result.Success ? Results.Ok(result) : Results.NotFound(result);
})
.RequireAuthorization("Authenticated")
.WithName("GetPaymentStatus");

app.MapGet("/api/payments/student/{studentId}", async (Guid studentId, HttpContext context, IPaymentService paymentService) =>
{
    var currentUserIdClaim = context.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
    if (Guid.TryParse(currentUserIdClaim, out Guid currentUserId))
    {
        var userRole = context.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        if (currentUserId != studentId && userRole != "ADMIN")
        {
            return Results.Forbid();
        }
    }

    var result = await paymentService.GetStudentPaymentsAsync(studentId);
    return Results.Ok(new { Success = true, Payments = result });
})
.RequireAuthorization("Authenticated")
.WithName("GetStudentPayments");

app.MapGet("/api/payments/course/{courseId}", async (Guid courseId, IPaymentService paymentService) =>
{
    var result = await paymentService.GetCoursePaymentsAsync(courseId);
    return Results.Ok(new { Success = true, Payments = result });
})
.RequireAuthorization("InstructorOrAdmin")
.WithName("GetCoursePayments");

app.MapPost("/api/payments/refund", async (RefundPaymentRequest request, IPaymentService paymentService) =>
{
    var result = await paymentService.RefundPaymentAsync(request.PaymentId, request);
    return result.Success ? Results.Ok(result) : Results.BadRequest(result);
})
.RequireAuthorization("AdminOnly")
.WithName("RefundPayment");

app.MapGet("/api/payments/revenue/total", async (IPaymentService paymentService) =>
{
    var revenue = await paymentService.GetTotalRevenueAsync();
    return Results.Ok(new { Success = true, Revenue = revenue });
})
.RequireAuthorization("AdminOnly")
.WithName("GetTotalRevenue");

app.MapGet("/api/payments/count", async (string status, IPaymentService paymentService) =>
{
    var count = await paymentService.GetPaymentCountAsync(status);
    return Results.Ok(new { Success = true, Count = count });
})
.RequireAuthorization("AdminOnly")
.WithName("GetPaymentCount");

// Webhook endpoint for Razorpay
app.MapPost("/api/payments/webhook/razorpay", async (HttpContext context, IPaymentService paymentService) =>
{
    try
    {
        using var reader = new StreamReader(context.Request.Body);
        var webhookData = await reader.ReadToEndAsync();
        
        // TODO: Verify webhook signature
        // TODO: Process webhook events (payment.captured, payment.failed, etc.)
        
        return Results.Ok(new { Success = true, Message = "Webhook received" });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { Success = false, Message = ex.Message });
    }
})
.WithName("RazorpayWebhook");

// Health check endpoint
app.MapGet("/api/payments/health", () =>
{
    return Results.Ok(new { status = "Healthy", service = "PaymentService", timestamp = DateTime.UtcNow });
})
.WithName("HealthCheck")
.WithOpenApi();

app.Run();
