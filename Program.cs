using HolyWater.Server.Interfaces;
using HolyWater.Server.Models;
using HolyWater.Server.services;
using Microsoft.EntityFrameworkCore;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// --- 1. RENDER DYNAMIC PORT SETUP ---
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// --- 2. DATABASE SETUP (FIXED FOR POSTGRESQL://) ---
var rawConnString = Environment.GetEnvironmentVariable("DATABASE_URL")
                    ?? builder.Configuration.GetConnectionString("DefaultConnection");

string finalConnString = "";

// Check for both postgres:// and postgresql:// prefixes
if (!string.IsNullOrEmpty(rawConnString) && (rawConnString.StartsWith("postgres://") || rawConnString.StartsWith("postgresql://")))
{
    try
    {
        // Standardize prefix for Uri parser
        var formattedUri = rawConnString.Replace("postgresql://", "postgres://");
        var databaseUri = new Uri(formattedUri);

        var userInfo = databaseUri.UserInfo.Split(':');
        finalConnString = $"Host={databaseUri.Host};Port={databaseUri.Port};Database={databaseUri.LocalPath.Substring(1)};Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Require;Trust Server Certificate=true;";
    }
    catch (Exception ex)
    {
        // Fallback to raw string if parsing fails
        Console.WriteLine($"DATABASE PARSING ERROR: {ex.Message}");
        finalConnString = rawConnString;
    }
}
else
{
    finalConnString = rawConnString ?? "";
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(finalConnString));

// --- 3. SERVICES ---
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IProductsService, ProductsService>();
builder.Services.AddScoped<IProductsAdminService, ProductsAdminService>();
builder.Services.AddScoped<ITokenService, TokenService>();

// JWT Setup
var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY") ?? builder.Configuration["Jwt:Key"];
var key = Encoding.UTF8.GetBytes(jwtKey ?? "YourFallbackSecretKey_MustBeLongEnough");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? builder.Configuration["Jwt:Issuer"],
        ValidateAudience = true,
        ValidAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? builder.Configuration["Jwt:Audience"],
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromSeconds(30)
    };
});

// --- 4. CORS CONFIGURATION ---
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp",
        policy => policy.WithOrigins("https://holy-essence-angular.onrender.com")
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials());
});

// Large File Upload Support
builder.Services.Configure<IISServerOptions>(options => { options.MaxRequestBodySize = int.MaxValue; });
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(x =>
{
    x.ValueLengthLimit = int.MaxValue;
    x.MultipartBodyLengthLimit = int.MaxValue;
    x.MemoryBufferThreshold = int.MaxValue;
});

var app = builder.Build();

// --- 5. MIDDLEWARE PIPELINE (ORDER MATTERS) ---

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(async context =>
        {
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            // Explicitly append CORS headers so Angular can read the 500 error
            context.Response.Headers.Append("Access-Control-Allow-Origin", "https://holy-essence-angular.onrender.com");
            context.Response.Headers.Append("Access-Control-Allow-Credentials", "true");
            await context.Response.WriteAsync("{\"error\":\"Internal Server Error. Check Render logs.\"}");
        });
    });
}

app.UseStaticFiles();
app.UseRouting();

// CORS must be AFTER Routing and BEFORE Auth
app.UseCors("AllowAngularApp");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();