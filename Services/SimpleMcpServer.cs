namespace AspMCPServer.Services
{
    public class SimpleMcpServer
    {
        public Task<object> GetServerInfoAsync()
        {
            return Task.FromResult<object>(new
            {
                Name = "Simple ASP.NET MCP Server",
                Version = "1.0.0",
                ProtocolVersion = "2024-11-05"
            });
        }

        public Task<object> ListToolsAsync()
        {
            var tools = new object[]
            {
                new
                {
                    Name = "echo",
                    Description = "Echo back the input text",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            text = new
                            {
                                type = "string",
                                description = "Text to echo back"
                            }
                        },
                        required = new[] { "text" }
                    }
                },
                new
                {
                    Name = "timestamp",
                    Description = "Get the current timestamp",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new { }
                    }
                },
                new
                {
                    Name = "weather",
                    Description = "Get weather information for a location",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            location = new
                            {
                                type = "string",
                                description = "Location to get weather for"
                            }
                        },
                        required = new[] { "location" }
                    }
                },
                new
                {
                    Name = "calculate",
                    Description = "Perform basic arithmetic calculations",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            expression = new
                            {
                                type = "string",
                                description = "Mathematical expression to evaluate (e.g., '2 + 3 * 4')"
                            }
                        },
                        required = new[] { "expression" }
                    }
                }
            };

            return Task.FromResult<object>(tools);
        }

        public Task<object> CallToolAsync(string name, object? arguments)
        {
            return name switch
            {
                "echo" => HandleEchoTool(arguments),
                "timestamp" => HandleTimestampTool(),
                "weather" => HandleWeatherTool(arguments),
                "calculate" => HandleCalculateTool(arguments),
                _ => Task.FromResult<object>(new
                {
                    IsError = true,
                    Content = new[] { new { Type = "text", Text = $"Unknown tool: {name}" } }
                })
            };
        }

        private Task<object> HandleEchoTool(object? arguments)
        {
            try
            {
                dynamic? args = arguments;
                string text = args?.text ?? "No text provided";

                return Task.FromResult<object>(new
                {
                    Content = new[]
                    {
                        new
                        {
                            Type = "text",
                            Text = $"Echo: {text}"
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                return Task.FromResult<object>(new
                {
                    IsError = true,
                    Content = new[]
                    {
                        new
                        {
                            Type = "text",
                            Text = $"Error processing echo tool: {ex.Message}"
                        }
                    }
                });
            }
        }

        private Task<object> HandleTimestampTool()
        {
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

            return Task.FromResult<object>(new
            {
                Content = new[]
                {
                    new
                    {
                        Type = "text",
                        Text = $"Current UTC timestamp: {timestamp}"
                    }
                }
            });
        }

        private Task<object> HandleWeatherTool(object? arguments)
        {
            try
            {
                dynamic? args = arguments;
                string location = args?.location ?? "Unknown location";

                // Simulate weather data
                var weather = new
                {
                    location,
                    temperature = new Random().Next(-10, 35),
                    condition = new[] { "Sunny", "Cloudy", "Rainy", "Snowy" }[new Random().Next(4)],
                    humidity = new Random().Next(30, 90)
                };

                return Task.FromResult<object>(new
                {
                    Content = new[]
                    {
                        new
                        {
                            Type = "text",
                            Text = $"Weather in {weather.location}: {weather.temperature}°C, {weather.condition}, {weather.humidity}% humidity"
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                return Task.FromResult<object>(new
                {
                    IsError = true,
                    Content = new[]
                    {
                        new
                        {
                            Type = "text",
                            Text = $"Error processing weather tool: {ex.Message}"
                        }
                    }
                });
            }
        }

        private Task<object> HandleCalculateTool(object? arguments)
        {
            try
            {
                dynamic? args = arguments;
                string expression = args?.expression ?? "0";
                
                // Simple calculator - in production, use a proper expression evaluator
                var result = EvaluateSimpleExpression(expression);

                return Task.FromResult<object>(new
                {
                    Content = new[]
                    {
                        new
                        {
                            Type = "text",
                            Text = $"Result: {expression} = {result}"
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                return Task.FromResult<object>(new
                {
                    IsError = true,
                    Content = new[]
                    {
                        new
                        {
                            Type = "text",
                            Text = $"Error calculating expression: {ex.Message}"
                        }
                    }
                });
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

        public Task<object> ListResourcesAsync()
        {
            var resources = new object[]
            {
                new
                {
                    Uri = "file:///example.txt",
                    Name = "Example Text File",
                    Description = "A sample text resource",
                    MimeType = "text/plain"
                }
            };

            return Task.FromResult<object>(resources);
        }

        public Task<object> ReadResourceAsync(string uri)
        {
            return uri switch
            {
                "file:///example.txt" => Task.FromResult<object>(new
                {
                    Uri = uri,
                    MimeType = "text/plain",
                    Text = "This is a sample text file content from the MCP server."
                }),
                _ => throw new ArgumentException($"Resource not found: {uri}")
            };
        }

        public Task<object> ListPromptsAsync()
        {
            var prompts = new object[]
            {
                new
                {
                    Name = "greeting",
                    Description = "Generate a friendly greeting",
                    Arguments = new[]
                    {
                        new
                        {
                            Name = "name",
                            Description = "Name of the person to greet",
                            Required = true
                        }
                    }
                }
            };

            return Task.FromResult<object>(prompts);
        }

        public Task<object> GetPromptAsync(string name, Dictionary<string, object>? arguments)
        {
            return name switch
            {
                "greeting" => HandleGreetingPrompt(arguments),
                _ => throw new ArgumentException($"Unknown prompt: {name}")
            };
        }

        private Task<object> HandleGreetingPrompt(Dictionary<string, object>? arguments)
        {
            var personName = arguments?.GetValueOrDefault("name")?.ToString() ?? "friend";

            return Task.FromResult<object>(new
            {
                Description = "A friendly greeting message",
                Messages = new[]
                {
                    new
                    {
                        Role = "user",
                        Content = new
                        {
                            Type = "text",
                            Text = $"Hello {personName}! Welcome to our simple MCP server. How can I help you today?"
                        }
                    }
                }
            });
        }
    }
}
