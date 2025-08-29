using AspMCPServer.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AspMCPServer
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            // Check if we should run in stdio mode
            if (args.Contains("--stdio") || args.Contains("stdio"))
            {
                return await RunStdioMode(args);
            }

            // Default: run as HTTP server
            return await RunHttpMode(args);
        }

        private static async Task<int> RunStdioMode(string[] args)
        {
            try
            {
                // Create a minimal host for dependency injection
                var host = Host.CreateDefaultBuilder(args)
                    .ConfigureServices((context, services) =>
                    {
                        services.AddSingleton<StdioMcpServer>();
                    })
                    .ConfigureLogging(logging =>
                    {
                        // Disable all logging to console to avoid interfering with stdio
                        logging.ClearProviders();
                    })
                    .UseConsoleLifetime()
                    .Build();

                var stdioServer = host.Services.GetRequiredService<StdioMcpServer>();
                
                // Create cancellation token for graceful shutdown
                using var cts = new CancellationTokenSource();
                Console.CancelKeyPress += (sender, e) =>
                {
                    e.Cancel = true;
                    cts.Cancel();
                };

                // Start the stdio server
                await stdioServer.StartAsync(cts.Token);
                
                return 0;
            }
            catch (Exception ex)
            {
                await Console.Error.WriteLineAsync($"Stdio server failed: {ex.Message}");
                return 1;
            }
        }

        private static async Task<int> RunHttpMode(string[] args)
        {
            try
            {
                var builder = WebApplication.CreateBuilder(args);

                // Add services to the container
                builder.Services.AddControllers();
                builder.Services.AddEndpointsApiExplorer();
                builder.Services.AddSwaggerGen();

                // Add MCP handler services
                builder.Services.AddSingleton<SimpleMcpHandler>();
                builder.Services.AddSingleton<SimpleMcpServer>();

                // Add CORS for MCP client connections
                builder.Services.AddCors(options =>
                {
                    options.AddDefaultPolicy(policy =>
                    {
                        policy.AllowAnyOrigin()
                              .AllowAnyMethod()
                              .AllowAnyHeader();
                    });
                });

                var app = builder.Build();

                // Add WebSocket support
                app.UseWebSockets(new WebSocketOptions
                {
                    KeepAliveInterval = TimeSpan.FromMinutes(2)
                });

                // Configure the HTTP request pipeline
                if (app.Environment.IsDevelopment())
                {
                    app.UseSwagger();
                    app.UseSwaggerUI();
                }

                app.UseHttpsRedirection();
                app.UseStaticFiles();
                app.UseCors();
                app.UseAuthorization();

                app.MapControllers();

                await app.RunAsync();
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"HTTP server failed: {ex.Message}");
                return 1;
            }
        }
    }
}
