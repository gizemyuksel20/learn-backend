using LearnAPI.Extensions;
using LearnAPI.Middlewares;
using Rollbar;
using Rollbar.NetCore.AspNet;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables(); 



// Rollbar ayarlarını al
var rollbarAccessToken = builder.Configuration["Rollbar:AccessToken"];
var rollbarEnvironment = builder.Configuration["Rollbar:Environment"] ?? "Development";
ConfigureRollbarInfrastructure(rollbarAccessToken, rollbarEnvironment);

// Rollbar log servisini ekle
builder.Services.AddRollbarLogger(loggerOptions =>
{
    loggerOptions.Filter = (loggerName, logLevel) => logLevel >= LogLevel.Information;

});

// Serilog yapılandırması
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Service kayıtları
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddOpenApi();

builder.Services.AddProjectServices(builder.Configuration);

var app = builder.Build();

// Swagger sadece dev ortamda aktif
//if (app.Environment.IsDevelopment())
//{
//    app.UseSwagger();
//    app.UseSwaggerUI(c =>
//    {
//        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Learn API V1");
//        c.RoutePrefix = "swagger";
//    });
//}


if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<GlobalExceptionMiddleware>();

// Global Rollbar middleware
app.UseRollbarMiddleware();

app.UseHttpsRedirection();
app.UseAuthorization();
app.UseStaticFiles();
app.MapControllers();
app.Run();

// Rollbar altyapı yapılandırması
void ConfigureRollbarInfrastructure(string token, string env)
{
    var config = new RollbarInfrastructureConfig(token, env);

    var securityOptions = new RollbarDataSecurityOptions
    {
        ScrubFields = new[] { "password", "authorization", "token", "apikey" }
    };

    config.RollbarLoggerConfig.RollbarDataSecurityOptions.Reconfigure(securityOptions);

    RollbarInfrastructure.Instance.Init(config);
}
