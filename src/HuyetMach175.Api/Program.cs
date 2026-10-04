using Microsoft.EntityFrameworkCore;
using HuyetMach175.Api.Data;

var builder = WebApplication.CreateBuilder(args);

// 1. Cấu hình EF Core với PostgreSQL (Npgsql)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Cấu hình CORS cho phép React Frontend kết nối
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// 3. Đăng ký Controllers & API Documentation
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var app = builder.Build();

// 4. Cấu hình HTTP Request Pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    // Tự động áp dụng Migration khi chạy trong môi trường Development
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await dbContext.Database.MigrateAsync();
}

app.UseHttpsRedirection();

// 5. Kích hoạt Middleware CORS (phải đặt trước Routing/Auth)
app.UseCors("AllowReactApp");

app.UseAuthorization();

// 6. Định tuyến đến các Controller của các Module
app.MapControllers();

app.Run();