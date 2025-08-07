using Ledon.BerryShare.Api.Data;
using Ledon.BerryShare.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Ledon.BerryShare.Api.Controllers;
using Ledon.BerryShare.Api.Middlewares;
using Scalar.AspNetCore;
// ...existing code...
var builder = WebApplication.CreateBuilder(args);

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
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key is not configured")))
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
builder.Services.AddOpenApi();

// Register UnitOfWork
builder.Services.AddScoped<UnitOfWork>();

var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();

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
}

app.UseHttpsRedirection();

app.UseCors("AllowAll");


// 自定义认证失败响应中间件
app.UseAuthentication();
app.Use(async (context, next) =>
{
    await next();
    if (context.Response.StatusCode == 401)
    {
        context.Response.StatusCode = 200;
        context.Response.ContentType = "application/json";
        var result = System.Text.Json.JsonSerializer.Serialize(new BerryResult
        {
            Code = BerryResult.StatusCodeEnum.Unauthorized,
            Message = "未授权访问"
        });
        await context.Response.WriteAsync(result);
    }
});
app.UseAuthorization();

app.MapControllers();

app.Run();
