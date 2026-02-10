using HolyWater.Server.Interfaces;
using HolyWater.Server.Models;
using HolyWater.Server.services;
using Microsoft.EntityFrameworkCore;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Npgsql; // 1. Make sure to install: dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL

var builder = WebApplication.CreateBuilder(args);

// --- RENDER DYNAMIC PORT SETUP ---
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// --- DATABASE SETUP ---
// Locally, it uses your appsettings. In Render, it uses the Environment Variable.
var rawConnString = Environment.GetEnvironmentVariable("DATABASE_URL")
                    ?? builder.Configuration.GetConnectionString("DefaultConnection");

string finalConnString;

if (rawConnString!.StartsWith("postgres://"))
{
    // Convert Render's postgres:// URI to .NET Connection String format
    var databaseUri = new Uri(rawConnString);
    var userInfo = databaseUri.UserInfo.Split(':');
    finalConnString = $"Host={databaseUri.Host};Port={databaseUri.Port};Database={databaseUri.LocalPath.Substring(1)};Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Require;Trust Server Certificate=true;";
}
else
{
    finalConnString = rawConnString;
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(finalConnString)); // 2. Changed from UseSqlServer to UseNpgsql

// --- THE REST OF YOUR SERVICES ---
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IProductsService, ProductsService>();
builder.Services.AddScoped<IProductsAdminService, ProductsAdminService>();
builder.Services.AddScoped<ITokenService, TokenService>();

// JWT Setup (Ensure these Env Vars are set in Render later!)
var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY") ?? builder.Configuration["Jwt:Key"];
var key = Encoding.UTF8.GetBytes(jwtKey!);

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

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp",
        policy => policy.AllowAnyOrigin() 
                        .AllowAnyHeader()
                        .AllowAnyMethod());
});

var app = builder.Build();

app.UseRouting();
// --- MIDDLEWARE ---
app.UseCors("AllowAngularApp");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    // Important: Render handles HTTPS termination. 
    // Only use HSTS in production, avoid UseHttpsRedirection if it causes loop issues.
    app.UseHsts();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();