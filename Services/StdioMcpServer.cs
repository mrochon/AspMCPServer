using System.Text.Json;

namespace AspMCPServer.Services
{
    public class StdioMcpServer
    {
        private readonly SimpleMcpHandler _mcpHandler;

        public StdioMcpServer(SimpleMcpHandler mcpHandler)
        {
            _mcpHandler = mcpHandler;
        }

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            // Send server info to stderr for debugging
            await Console.Error.WriteLineAsync("MCP Server starting on stdio transport...");
            await Console.Error.WriteLineAsync($"Process ID: {Environment.ProcessId}");
            await Console.Error.WriteLineAsync($"Working Directory: {Directory.GetCurrentDirectory()}");

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    // Read JSON-RPC message from stdin
                    var line = await Console.In.ReadLineAsync();
                    
                    if (string.IsNullOrEmpty(line))
                    {
                        // End of input stream
                        break;
                    }

                    try
                    {
                        // Process the JSON-RPC message
                        var response = await ProcessMessage(line);
                        
                        if (response != null)
                        {
                            // Write response to stdout
                            await Console.Out.WriteLineAsync(response);
                            await Console.Out.FlushAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        await Console.Error.WriteLineAsync($"Error processing message: {ex.Message}");
                        
                        // Send error response
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
                        
                        await Console.Out.WriteLineAsync(JsonSerializer.Serialize(errorResponse));
                        await Console.Out.FlushAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                await Console.Error.WriteLineAsync($"Stdio server error: {ex.Message}");
                throw;
            }
            finally
            {
                await Console.Error.WriteLineAsync("MCP Server shutting down...");
            }
        }

        private async Task<string?> ProcessMessage(string message)
        {
            try
            {
                var request = JsonSerializer.Deserialize<JsonElement>(message);
                
                // Extract JSON-RPC method and parameters
                if (!request.TryGetProperty("method", out var methodElement))
                {
                    return CreateErrorResponse(null, -32600, "Invalid Request - Missing method");
                }

                var method = methodElement.GetString();
                var id = request.TryGetProperty("id", out var idElement) ? idElement.GetString() : null;
                var hasParams = request.TryGetProperty("params", out var paramsElement);

                await Console.Error.WriteLineAsync($"Processing method: {method} with id: {id}");

                var response = method switch
                {
                    "initialize" => HandleInitialize(id, hasParams ? paramsElement : default),
                    "initialized" => null, // Notification - no response needed
                    "tools/list" => HandleListTools(id),
                    "tools/call" => HandleCallTool(id, hasParams ? paramsElement : default),
                    "resources/list" => HandleListResources(id),
                    "resources/read" => HandleReadResource(id, hasParams ? paramsElement : default),
                    "prompts/list" => HandleListPrompts(id),
                    "prompts/get" => HandleGetPrompt(id, hasParams ? paramsElement : default),
                    "ping" => HandlePing(id),
                    _ => CreateErrorResponseObject(id, -32601, "Method not found")
                };

                return response != null ? JsonSerializer.Serialize(response) : null;
            }
            catch (JsonException ex)
            {
                await Console.Error.WriteLineAsync($"JSON parse error: {ex.Message}");
                return CreateErrorResponse(null, -32700, $"Parse error: {ex.Message}");
            }
        }

        private object HandleInitialize(string? id, JsonElement paramsElement)
        {
            var clientInfo = new { name = "Unknown", version = "1.0.0" };
            
            if (paramsElement.ValueKind != JsonValueKind.Undefined &&
                paramsElement.TryGetProperty("clientInfo", out var clientInfoElement))
            {
                clientInfo = new
                {
                    name = clientInfoElement.TryGetProperty("name", out var nameEl) ? nameEl.GetString() ?? "Unknown" : "Unknown",
                    version = clientInfoElement.TryGetProperty("version", out var versionEl) ? versionEl.GetString() ?? "1.0.0" : "1.0.0"
                };
            }

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
                        name = "ASP.NET MCP Server (stdio)",
                        version = "1.0.0"
                    },
                    instructions = "Server initialized successfully. Connected via stdio transport."
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
                            description = "Get weather information for a location",
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
                    return CreateErrorResponseObject(id, -32602, "Missing tool name");
                }

                var toolName = nameElement.GetString();
                var hasArguments = paramsElement.TryGetProperty("arguments", out var argumentsElement);

                return toolName switch
                {
                    "echo" => HandleEchoTool(id, hasArguments ? argumentsElement : default),
                    "timestamp" => HandleTimestampTool(id),
                    "weather" => HandleWeatherTool(id, hasArguments ? argumentsElement : default),
                    "calculate" => HandleCalculateTool(id, hasArguments ? argumentsElement : default),
                    _ => CreateErrorResponseObject(id, -32602, $"Unknown tool: {toolName}")
                };
            }
            catch (Exception ex)
            {
                return CreateErrorResponseObject(id, -32603, $"Error calling tool: {ex.Message}");
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

        private object HandleWeatherTool(string? id, JsonElement argumentsElement)
        {
            var location = "Unknown location";
            
            if (argumentsElement.ValueKind != JsonValueKind.Undefined &&
                argumentsElement.TryGetProperty("location", out var locationElement))
            {
                location = locationElement.GetString() ?? "Unknown location";
            }

            var weather = new
            {
                location,
                temperature = Random.Shared.Next(-10, 35),
                condition = new[] { "Sunny", "Cloudy", "Rainy", "Snowy" }[Random.Shared.Next(4)],
                humidity = Random.Shared.Next(30, 90)
            };

            return new
            {
                jsonrpc = "2.0",
                result = new
                {
                    content = new[]
                    {
                        new { type = "text", text = $"Weather in {weather.location}: {weather.temperature}°C, {weather.condition}, {weather.humidity}% humidity" }
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

                var result = EvaluateSimpleExpression(expression);

                return new
                {
                    jsonrpc = "2.0",
                    result = new
                    {
                        content = new[]
                        {
                            new { type = "text", text = $"Result: {expression} = {result}" }
                        }
                    },
                    id
                };
            }
            catch (Exception ex)
            {
                return CreateErrorResponseObject(id, -32603, $"Error calculating expression: {ex.Message}");
            }
        }

        private double EvaluateSimpleExpression(string expression)
        {
            expression = expression.Replace(" ", "");
            
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
                            uri = "file:///readme.txt",
                            name = "Server README",
                            description = "Information about this MCP server",
                            mimeType = "text/plain"
                        },
                        new
                        {
                            uri = "file:///config.json",
                            name = "Server Configuration",
                            description = "Server configuration details",
                            mimeType = "application/json"
                        }
                    }
                },
                id
            };
        }

        private object HandleReadResource(string? id, JsonElement paramsElement)
        {
            try
            {
                if (!paramsElement.TryGetProperty("uri", out var uriElement))
                {
                    return CreateErrorResponseObject(id, -32602, "Missing resource URI");
                }

                var uri = uriElement.GetString();

                var content = uri switch
                {
                    "file:///readme.txt" => "This is the ASP.NET MCP Server running on stdio transport.\n\nAvailable tools:\n- echo: Echo text back\n- timestamp: Get current time\n- weather: Get weather info\n- calculate: Basic math\n\nUse tools/list to see detailed schemas.",
                    "file:///config.json" => JsonSerializer.Serialize(new
                    {
                        server = new
                        {
                            name = "ASP.NET MCP Server",
                            version = "1.0.0",
                            transport = "stdio"
                        },
                        tools = new { count = 4 },
                        features = new[] { "tools", "resources", "prompts" }
                    }, new JsonSerializerOptions { WriteIndented = true }),
                    _ => throw new FileNotFoundException($"Resource not found: {uri}")
                };

                return new
                {
                    jsonrpc = "2.0",
                    result = new
                    {
                        contents = new[]
                        {
                            new
                            {
                                uri,
                                mimeType = uri.EndsWith(".json") ? "application/json" : "text/plain",
                                text = content
                            }
                        }
                    },
                    id
                };
            }
            catch (Exception ex)
            {
                return CreateErrorResponseObject(id, -32603, $"Error reading resource: {ex.Message}");
            }
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
                                },
                                new
                                {
                                    name = "language",
                                    description = "Language for the greeting (en, es, fr)",
                                    required = false
                                }
                            }
                        }
                    }
                },
                id
            };
        }

        private object HandleGetPrompt(string? id, JsonElement paramsElement)
        {
            try
            {
                if (!paramsElement.TryGetProperty("name", out var nameElement))
                {
                    return CreateErrorResponseObject(id, -32602, "Missing prompt name");
                }

                var promptName = nameElement.GetString();
                var hasArguments = paramsElement.TryGetProperty("arguments", out var argumentsElement);

                if (promptName == "greeting")
                {
                    var name = "World";
                    var language = "en";

                    if (hasArguments)
                    {
                        if (argumentsElement.TryGetProperty("name", out var nameEl))
                            name = nameEl.GetString() ?? "World";
                        if (argumentsElement.TryGetProperty("language", out var langEl))
                            language = langEl.GetString() ?? "en";
                    }

                    var greeting = language switch
                    {
                        "es" => $"¡Hola, {name}! ¿Cómo estás?",
                        "fr" => $"Bonjour, {name}! Comment allez-vous?",
                        _ => $"Hello, {name}! How are you doing today?"
                    };

                    return new
                    {
                        jsonrpc = "2.0",
                        result = new
                        {
                            description = $"A friendly greeting in {language}",
                            messages = new[]
                            {
                                new
                                {
                                    role = "user",
                                    content = new
                                    {
                                        type = "text",
                                        text = greeting
                                    }
                                }
                            }
                        },
                        id
                    };
                }

                return CreateErrorResponseObject(id, -32602, $"Unknown prompt: {promptName}");
            }
            catch (Exception ex)
            {
                return CreateErrorResponseObject(id, -32603, $"Error getting prompt: {ex.Message}");
            }
        }

        private object HandlePing(string? id)
        {
            return new
            {
                jsonrpc = "2.0",
                result = new
                {
                    status = "pong",
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    transport = "stdio"
                },
                id
            };
        }

        private string CreateErrorResponse(string? id, int code, string message)
        {
            var error = new
            {
                jsonrpc = "2.0",
                error = new { code, message },
                id
            };
            return JsonSerializer.Serialize(error);
        }

        private object CreateErrorResponseObject(string? id, int code, string message)
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
