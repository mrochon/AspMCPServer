using Microsoft.AspNetCore.Mvc;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace AspMCPServer.Controllers
{
    [ApiController]
    [Route("mcp")]
    public class McpWebSocketController : ControllerBase
    {
        // WebSocket endpoint for bidirectional MCP communication
        [HttpGet("ws")]
        public async Task HandleWebSocket()
        {
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                await HandleWebSocketCommunication(webSocket);
            }
            else
            {
                HttpContext.Response.StatusCode = 400;
                await HttpContext.Response.WriteAsync("WebSocket connection required");
            }
        }

        private async Task HandleWebSocketCommunication(WebSocket webSocket)
        {
            var buffer = new byte[1024 * 4];
            
            try
            {
                // Send initial connection message
                var welcomeMessage = new
                {
                    type = "connected",
                    message = "MCP WebSocket Server connected",
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                };
                
                await SendWebSocketMessage(webSocket, JsonSerializer.Serialize(welcomeMessage));

                // Listen for incoming messages
                while (webSocket.State == WebSocketState.Open)
                {
                    var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    
                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        var response = await ProcessMcpMessage(message);
                        await SendWebSocketMessage(webSocket, response);
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, 
                            "Connection closed", CancellationToken.None);
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle connection errors
                if (webSocket.State == WebSocketState.Open)
                {
                    var errorResponse = new
                    {
                        jsonrpc = "2.0",
                        error = new
                        {
                            code = -32603,
                            message = "Internal error",
                            data = ex.Message
                        },
                        id = (string?)null
                    };
                    
                    await SendWebSocketMessage(webSocket, JsonSerializer.Serialize(errorResponse));
                }
            }
        }

        private async Task<string> ProcessMcpMessage(string message)
        {
            try
            {
                var request = JsonSerializer.Deserialize<JsonElement>(message);
                
                // Extract JSON-RPC method and parameters
                if (!request.TryGetProperty("method", out var methodElement))
                {
                    return CreateErrorResponseJson(null, -32600, "Invalid Request - Missing method");
                }

                var method = methodElement.GetString();
                var id = request.TryGetProperty("id", out var idElement) ? idElement.GetString() : null;
                var hasParams = request.TryGetProperty("params", out var paramsElement);

                var response = method switch
                {
                    "initialize" => HandleInitialize(id),
                    "tools/list" => HandleListTools(id),
                    "tools/call" => HandleCallTool(id, hasParams ? paramsElement : default),
                    "resources/list" => HandleListResources(id),
                    "prompts/list" => HandleListPrompts(id),
                    _ => CreateErrorResponse(id, -32601, "Method not found")
                };

                return JsonSerializer.Serialize(response);
            }
            catch (Exception ex)
            {
                return CreateErrorResponseJson(null, -32700, $"Parse error: {ex.Message}");
            }
        }

        private async Task SendWebSocketMessage(WebSocket webSocket, string message)
        {
            var bytes = Encoding.UTF8.GetBytes(message);
            await webSocket.SendAsync(new ArraySegment<byte>(bytes), 
                WebSocketMessageType.Text, true, CancellationToken.None);
        }

        private string CreateErrorResponseJson(string? id, int code, string message)
        {
            var error = new
            {
                jsonrpc = "2.0",
                error = new { code, message },
                id
            };
            return JsonSerializer.Serialize(error);
        }

        // Reuse the same handler methods from McpStreamController
        private object HandleInitialize(string? id)
        {
            return new
            {
                jsonrpc = "2.0",
                result = new
                {
                    protocolVersion = "2024-11-05",
                    capabilities = new
                    {
                        tools = new { },
                        resources = new { },
                        prompts = new { }
                    },
                    serverInfo = new
                    {
                        name = "ASP.NET MCP WebSocket Server",
                        version = "1.0.0"
                    }
                },
                id
            };
        }

        private object HandleListTools(string? id)
        {
            return new
            {
                jsonrpc = "2.0",
                result = new
                {
                    tools = new object[]
                    {
                        new
                        {
                            name = "echo",
                            description = "Echo back the input text",
                            inputSchema = new
                            {
                                type = "object",
                                properties = new
                                {
                                    text = new { type = "string", description = "Text to echo back" }
                                },
                                required = new[] { "text" }
                            }
                        },
                        new
                        {
                            name = "timestamp",
                            description = "Get the current timestamp",
                            inputSchema = new
                            {
                                type = "object",
                                properties = new { }
                            }
                        },
                        new
                        {
                            name = "weather",
                            description = "Get weather information for a location (streamed response)",
                            inputSchema = new
                            {
                                type = "object",
                                properties = new
                                {
                                    location = new { type = "string", description = "Location to get weather for" }
                                },
                                required = new[] { "location" }
                            }
                        },
                        new
                        {
                            name = "calculate",
                            description = "Perform basic arithmetic calculations",
                            inputSchema = new
                            {
                                type = "object",
                                properties = new
                                {
                                    expression = new { type = "string", description = "Mathematical expression to evaluate" }
                                },
                                required = new[] { "expression" }
                            }
                        }
                    }
                },
                id
            };
        }

        private object HandleCallTool(string? id, JsonElement paramsElement)
        {
            try
            {
                if (!paramsElement.TryGetProperty("name", out var nameElement))
                {
                    return CreateErrorResponse(id, -32602, "Missing tool name");
                }

                var toolName = nameElement.GetString();
                var hasArguments = paramsElement.TryGetProperty("arguments", out var argumentsElement);

                return toolName switch
                {
                    "echo" => HandleEchoTool(id, hasArguments ? argumentsElement : default),
                    "timestamp" => HandleTimestampTool(id),
                    "weather" => HandleWeatherTool(id, hasArguments ? argumentsElement : default),
                    "calculate" => HandleCalculateTool(id, hasArguments ? argumentsElement : default),
                    _ => CreateErrorResponse(id, -32602, $"Unknown tool: {toolName}")
                };
            }
            catch (Exception ex)
            {
                return CreateErrorResponse(id, -32603, $"Error calling tool: {ex.Message}");
            }
        }

        private object HandleEchoTool(string? id, JsonElement argumentsElement)
        {
            var text = "No text provided";
            
            if (argumentsElement.ValueKind != JsonValueKind.Undefined &&
                argumentsElement.TryGetProperty("text", out var textElement))
            {
                text = textElement.GetString() ?? "No text provided";
            }

            return new
            {
                jsonrpc = "2.0",
                result = new
                {
                    content = new[]
                    {
                        new { type = "text", text = $"Echo: {text}" }
                    }
                },
                id
            };
        }

        private object HandleTimestampTool(string? id)
        {
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

            return new
            {
                jsonrpc = "2.0",
                result = new
                {
                    content = new[]
                    {
                        new { type = "text", text = $"Current UTC timestamp: {timestamp}" }
                    }
                },
                id
            };
        }

        private object HandleListResources(string? id)
        {
            return new
            {
                jsonrpc = "2.0",
                result = new
                {
                    resources = new object[]
                    {
                        new
                        {
                            uri = "file:///sample.txt",
                            name = "Sample Text File",
                            description = "A sample text resource",
                            mimeType = "text/plain"
                        }
                    }
                },
                id
            };
        }

        private object HandleListPrompts(string? id)
        {
            return new
            {
                jsonrpc = "2.0",
                result = new
                {
                    prompts = new object[]
                    {
                        new
                        {
                            name = "greeting",
                            description = "Generate a friendly greeting",
                            arguments = new object[]
                            {
                                new
                                {
                                    name = "name",
                                    description = "Name of the person to greet",
                                    required = true
                                }
                            }
                        }
                    }
                },
                id
            };
        }

        private object HandleWeatherTool(string? id, JsonElement argumentsElement)
        {
            var location = "Unknown location";
            
            if (argumentsElement.ValueKind != JsonValueKind.Undefined &&
                argumentsElement.TryGetProperty("location", out var locationElement))
            {
                location = locationElement.GetString() ?? "Unknown location";
            }

            // Simulate weather data
            var weather = new
            {
                location,
                temperature = new Random().Next(-10, 35),
                condition = new[] { "Sunny", "Cloudy", "Rainy", "Snowy" }[new Random().Next(4)],
                humidity = new Random().Next(30, 90),
                windSpeed = new Random().Next(5, 25),
                pressure = new Random().Next(980, 1030)
            };

            // Return streaming response with 3 chunks
            return new
            {
                jsonrpc = "2.0",
                result = new
                {
                    content = new[]
                    {
                        new
                        {
                            type = "text",
                            text = $"🌍 Weather Report for {weather.location}:\n"
                        },
                        new
                        {
                            type = "text",
                            text = $"🌡️ Temperature: {weather.temperature}°C\n🌤️ Condition: {weather.condition}\n"
                        },
                        new
                        {
                            type = "text",
                            text = $"💧 Humidity: {weather.humidity}%\n💨 Wind Speed: {weather.windSpeed} km/h\n🔽 Pressure: {weather.pressure} hPa"
                        }
                    },
                    isStreaming = true,
                    streamingInfo = new
                    {
                        totalChunks = 3,
                        description = "Weather data streamed in 3 parts: location, temperature/condition, and atmospheric details"
                    }
                },
                id
            };
        }

        private object HandleCalculateTool(string? id, JsonElement argumentsElement)
        {
            try
            {
                var expression = "0";
                
                if (argumentsElement.ValueKind != JsonValueKind.Undefined &&
                    argumentsElement.TryGetProperty("expression", out var expressionElement))
                {
                    expression = expressionElement.GetString() ?? "0";
                }

                // Simple calculator - in production, use a proper expression evaluator
                var result = EvaluateSimpleExpression(expression);

                return new
                {
                    jsonrpc = "2.0",
                    result = new
                    {
                        content = new[]
                        {
                            new
                            {
                                type = "text",
                                text = $"Result: {expression} = {result}"
                            }
                        }
                    },
                    id
                };
            }
            catch (Exception ex)
            {
                return CreateErrorResponse(id, -32603, $"Error calculating expression: {ex.Message}");
            }
        }

        private double EvaluateSimpleExpression(string expression)
        {
            // Very basic expression evaluator - replace with proper library in production
            expression = expression.Replace(" ", "");
            
            // Handle simple operations like "2+3", "10-5", "4*3", "12/4"
            if (expression.Contains('+'))
            {
                var parts = expression.Split('+');
                return double.Parse(parts[0]) + double.Parse(parts[1]);
            }
            if (expression.Contains('-'))
            {
                var parts = expression.Split('-');
                return double.Parse(parts[0]) - double.Parse(parts[1]);
            }
            if (expression.Contains('*'))
            {
                var parts = expression.Split('*');
                return double.Parse(parts[0]) * double.Parse(parts[1]);
            }
            if (expression.Contains('/'))
            {
                var parts = expression.Split('/');
                return double.Parse(parts[0]) / double.Parse(parts[1]);
            }
            
            return double.Parse(expression);
        }

        private object CreateErrorResponse(string? id, int code, string message)
        {
            return new
            {
                jsonrpc = "2.0",
                error = new { code, message },
                id
            };
        }
    }
}
