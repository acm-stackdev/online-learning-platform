using System.Text;
using CloudinaryDotNet;
using LearnHub.Data;
using LearnHub.Helpers;
using LearnHub.Hubs;
using LearnHub.Middleware;
using LearnHub.Models.Entities;
using LearnHub.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;



Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

try{
    DotNetEnv.Env.Load();

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog();
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
    builder.Services.AddSignalR();

    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

    builder.Services.AddHealthChecks()
        .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!, name: "postgres");

    builder.Services.AddSingleton(new Cloudinary(new Account(
        builder.Configuration["Cloudinary:CloudName"],
        builder.Configuration["Cloudinary:ApiKey"],
        builder.Configuration["Cloudinary:ApiSecret"]
    )));

    builder.Services.AddSingleton<JwtHelper>();
    builder.Services.AddSingleton<IEmailService, EmailService>();
    builder.Services.AddSingleton<IPresenceTracker, PresenceTracker>();
    builder.Services.AddScoped<IFileUploadService, CloudinaryUploadService>();
    builder.Services.AddScoped<AuthService>();
    builder.Services.AddScoped<CourseService>();
    builder.Services.AddScoped<SectionService>();
    builder.Services.AddScoped<LessonService>();
    builder.Services.AddScoped<EnrollmentService>();
    builder.Services.AddScoped<CertificateService>();
    builder.Services.AddScoped<ProgressService>();
    builder.Services.AddScoped<MessagingService>();
    builder.Services.AddScoped<InstructorApplicationService>();
    builder.Services.AddScoped<AdminService>();
    builder.Services.AddScoped<DashboardService>();
    builder.Services.AddHttpClient<IGeminiClient, GeminiClient>(client =>
    {
        client.BaseAddress = new Uri("https://generativelanguage.googleapis.com/");
        client.DefaultRequestHeaders.Add("x-goog-api-key", builder.Configuration["Gemini:ApiKey"]);
    });
    builder.Services.AddScoped<ChatbotService>();

    builder.Services.Configure<Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions>(options =>
    {
        options.Limits.MaxRequestBodySize = 500L * 1024 * 1024; // allow large video uploads
    });

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = builder.Configuration["Jwt:Issuer"],
                ValidAudience = builder.Configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]!)),
            };

            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    if (context.Request.Cookies.TryGetValue("access_token", out var token))
                        context.Token = token;
                    return Task.CompletedTask;
                }
            };
        });

    builder.Services.AddAuthorization();

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowFrontend", policy =>
        {
            policy.WithOrigins(builder.Configuration["Frontend:BaseUrl"]!)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
    });


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (!await db.Users.AnyAsync(u => u.Role == Role.Admin))
    {
        var adminEmail = builder.Configuration["Admin:Email"] ?? "admin@learnhub.local";
        var adminPassword = builder.Configuration["Admin:Password"] ?? "Admin123!";
        var admin = new User
        {
            Username = "admin",
            Email = adminEmail,
            Role = Role.Admin,
            IsEmailVerified = true,
            CreatedAt = DateTime.UtcNow,
        };
        admin.PasswordHash = new PasswordHasher<User>().HashPassword(admin, adminPassword);
        db.Users.Add(admin);
        await db.SaveChangesAsync();
        Log.Information("Seeded dev admin account: {Email} / {Password}", adminEmail, adminPassword);
    }
}

app.UseSerilogRequestLogging();
app.UseCors("AllowFrontend");
app.UseMiddleware<CsrfGuardMiddleware>();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<MessagingHub>("/hubs/messaging");
app.MapHealthChecks("/health");


app.Run();
        
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application failed to start");
}
finally
{
    Log.CloseAndFlush();
}

