using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using SnapBlast.Api.Models;
using SnapBlast.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<MongoOptions>(builder.Configuration.GetSection("Mongo"));
builder.Services.Configure<GeminiOptions>(builder.Configuration.GetSection("Gemini"));
builder.Services.Configure<UploadOptions>(builder.Configuration.GetSection("Uploads"));
builder.Services.Configure<AuthenticationOptions>(builder.Configuration.GetSection("Authentication"));

var mongoConnection = builder.Configuration["Mongo:ConnectionString"] ?? builder.Configuration.GetConnectionString("MongoDb");
if (string.IsNullOrWhiteSpace(mongoConnection)) throw new InvalidOperationException("Mongo:ConnectionString must be configured.");
builder.Services.AddSingleton<IMongoClient>(_ => new MongoClient(mongoConnection));
builder.Services.AddSingleton<IUserRepository, MongoUserRepository>();
builder.Services.AddSingleton<IQuizRepository, MongoQuizRepository>();
builder.Services.AddSingleton<IPasswordHasher<UserDocument>, PasswordHasher<UserDocument>>();
builder.Services.AddSingleton<JwtTokenService>();
builder.Services.AddHttpClient<GeminiQuizService>(client => client.BaseAddress = new Uri("https://generativelanguage.googleapis.com/"));

var authOptions = builder.Configuration.GetSection("Authentication").Get<AuthenticationOptions>() ?? new();
if (Encoding.UTF8.GetByteCount(authOptions.JwtSecret) < 32) throw new InvalidOperationException("Authentication:JwtSecret must be configured with at least 32 bytes.");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(authOptions.JwtSecret)),
        ValidateIssuer = true,
        ValidIssuer = authOptions.Issuer,
        ValidateAudience = true,
        ValidAudience = authOptions.Audience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(1)
    };
});
builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
