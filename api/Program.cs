using api.Hubs;
using api.Services;
using api.Hubs;
using api.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. 添加服务
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 添加 SignalR
builder.Services.AddSignalR();

// 注册游戏引擎逻辑
builder.Services.AddSingleton<GameEngine>();

// 配置跨域 (允许 Vue 前端访问)
builder.Services.AddCors(options =>
{
    options.AddPolicy("VueClientPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:5173") // 你的 Vue 运行地址
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // SignalR 必须允许凭证
    });
});

var app = builder.Build();

// 2. 配置 HTTP 管道
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("VueClientPolicy"); // 启用 CORS

app.UseAuthorization();

app.MapControllers();

// 映射 SignalR Hub 的 URL
app.MapHub<GameHub>("/gameHub");

app.Run();