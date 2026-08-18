using MutualFund.Investment.Application;
using MutualFund.Investment.Infrastructure;
using MutualFund.Mcp.API.Services;
using MutualFund.Mcp.API.Tools;

var builder = WebApplication.CreateBuilder(args);

// Add Controllers & Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "MutualFund messaging-mcp API",
        Version = "v1",
        Description = "WhatsApp & Telegram Scheme-Wise Daily Digest MCP Server"
    });
});

// Register HTTP Client & Messaging Services
builder.Services.AddHttpClient();
builder.Services.AddScoped<TelegramService>();
builder.Services.AddScoped<WhatsAppService>();

// Register Investment Infrastructure & Application Services
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();

// Register MCP Tools
builder.Services.AddScoped<MessagingMcpTools>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

app.UseCors("AllowAll");
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "messaging-mcp v1");
    c.RoutePrefix = "swagger";
});

app.MapGet("/", () => Results.Redirect("/swagger"));
app.MapControllers();

app.Run();
