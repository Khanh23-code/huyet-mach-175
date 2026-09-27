var builder = WebApplication.CreateBuilder(args);

// 1. Cấu hình CORS cho phép React Frontend kết nối
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

// 2. Đăng ký Controllers & API Documentation
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var app = builder.Build();

// 3. Cấu hình HTTP Request Pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// 4. Kích hoạt Middleware CORS (phải đặt trước Routing/Auth)
app.UseCors("AllowReactApp");

app.UseAuthorization();

// 5. Định tuyến đến các Controller của các Module
app.MapControllers();

app.Run();