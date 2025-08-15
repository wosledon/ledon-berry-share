using Ledon.BerryShare.Api.Data;
using Ledon.BerryShare.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Ledon.BerryShare.Api.Controllers;
using Ledon.BerryShare.Api.Middlewares;
using Scalar.AspNetCore;
using Ledon.BerryShare.Api.Extensions;
using Ledon.BerryShare.Shared;

var builder = WebApplication.CreateBuilder(args);

// 动态端口: 支持通过 --port 或环境变量 BERRY_API_PORT 指定；否则使用默认 0 (不改变默认行为)
int? customPort = null;
for (int i = 0; i < args.Length - 1; i++)
{
    if (string.Equals(args[i], "--port", StringComparison.OrdinalIgnoreCase) && int.TryParse(args[i + 1], out var p))
    {
        customPort = p;
        break;
    }
}
if (customPort == null && int.TryParse(Environment.GetEnvironmentVariable("BERRY_API_PORT"), out var envPort))
{
    customPort = envPort;
}
if (customPort is > 0)
{
    builder.WebHost.UseUrls($"http://127.0.0.1:{customPort}");
}

// EF Core Sqlite & 懒加载代理
builder.Services.AddDbContext<BerryShareDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"))
           .UseLazyLoadingProxies()
);

// JWT 认证配置
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = false,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key is not configured")))
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine("Authentication failed: " + context.Exception.Message);
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                Console.WriteLine("OnChallenge: " + context.ErrorDescription);
                return Task.CompletedTask;
            }
        };
    });

// CORS 配置
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Add services to the container.
builder.Services.AddControllers();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi(opts =>
{
    opts.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
});

// Register UnitOfWork
builder.Services.AddScoped<UnitOfWork>();

var app = builder.Build();


// init database
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<BerryShareDbContext>();
    dbContext.Database.EnsureCreated(); // 确保数据库已创建
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapScalarApiReference();
    app.MapOpenApi();
    // app.UseWebAssemblyDebugging();
}


app.UseMiddleware<ExceptionMiddleware>();

// app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
