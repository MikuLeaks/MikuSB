using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MikuSB.Proxy;
using MikuSB.SdkServer.Handlers;
using MikuSB.SdkServer.Utils;
using MikuSB.Util;
using System.Text.Json;

namespace MikuSB.SdkServer;

public static class SdkServer
{
    public static void Start(string[] args)
    {
        var builder = Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder
                    .UseStartup<Startup>()
                    .ConfigureLogging((_, logging) => { logging.ClearProviders(); })
                    .UseUrls(ConfigManager.Config.HttpServer.GetDisplayAddress());
            });

        var host = builder.Build();
        host.RunAsync();
    }
}

public class Startup
{
    private static bool LooksLikeServerListRequest(string path, string? query)
    {
        var value = $"{path}?{query}".ToLowerInvariant();
        return value.Contains("server")
            || value.Contains("version")
            || value.Contains("query_version")
            || value.Contains("serverlist");
    }

    public static void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment()) app.UseDeveloperExceptionPage();

        app.UseRouting();
        app.UseCors("AllowAll");
        app.UseAuthorization();
        app.UseMiddleware<RequestLoggingMiddleware>();
        app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapFallback(async context =>
            {
                var path = context.Request.Path.Value ?? "";
                if (LooksLikeServerListRequest(path, context.Request.QueryString.Value))
                {
                    var response = RouteController.BuildServerList("");
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(JsonSerializer.Serialize(response));
                    return;
                }
                var fallbackResponse = new
                {
                    code = 0,
                    message = "ok",
                    service = ConfigManager.Config.GameServer.GameServerName,
                    path = path,
                    query = context.Request.QueryString.Value ?? ""
                };

                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(fallbackResponse));
            });
        });
    }

    public static void ConfigureServices(IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("AllowAll",
                builder => { builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader(); });
        });
        services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
            });
        services.AddSingleton<Logger>(_ => new Logger("HttpServer"));
        services.AddMikuSbProxy(ConfigManager.Config.Proxy);
    }
}
