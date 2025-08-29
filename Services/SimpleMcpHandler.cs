namespace AspMCPServer.Services
{
    public class SimpleMcpHandler
    {
        public Task<object> HandleRequestAsync(string method, object? parameters, CancellationToken cancellationToken = default)
        {
            return method switch
            {
                "initialize" => HandleInitialize(parameters),
                "tools/list" => HandleListTools(),
                "tools/call" => HandleCallTool(parameters),
                "resources/list" => HandleListResources(),
                "resources/read" => HandleReadResource(parameters),
                "prompts/list" => HandleListPrompts(),
                "prompts/get" => HandleGetPrompt(parameters),
                _ => throw new InvalidOperationException($"Unknown method: {method}")
            };
        }

        private Task<object> HandleInitialize(object? parameters)
        {
            return Task.FromResult<object>(new
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
                    name = "ASP.NET MCP Server",
                    version = "1.0.0"
                }
            });
        }

        private Task<object> HandleListTools()
        {
            var tools = new object[]
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
                    name = "calculate",
                    description = "Perform basic arithmetic calculations",
                    inputSchema = new
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
                            location = new
                            {
                                type = "string",
                                description = "Location to get weather for"
                            }
                        },
                        required = new[] { "location" }
                    }
                }
            };

            return Task.FromResult<object>(new { tools });
        }

        private Task<object> HandleCallTool(object? parameters)
        {
            // This would need proper parameter parsing based on the actual package API
            dynamic? args = parameters;
            string? toolName = args?.name;
            dynamic? toolArgs = args?.arguments;

            return toolName switch
            {
                "echo" => HandleEchoTool(toolArgs),
                "timestamp" => HandleTimestampTool(),
                "calculate" => HandleCalculateTool(toolArgs),
                "weather" => HandleWeatherTool(toolArgs),
                _ => Task.FromResult<object>(new
                {
                    isError = true,
                    content = new[]
                    {
                        new
                        {
                            type = "text",
                            text = $"Unknown tool: {toolName}"
                        }
                    }
                })
            };
        }

        private Task<object> HandleEchoTool(dynamic? arguments)
        {
            string text = arguments?.text ?? "No text provided";

            return Task.FromResult<object>(new
            {
                content = new[]
                {
                    new
                    {
                        type = "text",
                        text = $"Echo: {text}"
                    }
                }
            });
        }

        private Task<object> HandleTimestampTool()
        {
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

            return Task.FromResult<object>(new
            {
                content = new[]
                {
                    new
                    {
                        type = "text",
                        text = $"Current UTC timestamp: {timestamp}"
                    }
                }
            });
        }

        private Task<object> HandleCalculateTool(dynamic? arguments)
        {
            try
            {
                string expression = arguments?.expression ?? "0";
                
                // Simple calculator - in production, use a proper expression evaluator
                var result = EvaluateSimpleExpression(expression);

                return Task.FromResult<object>(new
                {
                    content = new[]
                    {
                        new
                        {
                            type = "text",
                            text = $"Result: {expression} = {result}"
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                return Task.FromResult<object>(new
                {
                    isError = true,
                    content = new[]
                    {
                        new
                        {
                            type = "text",
                            text = $"Error calculating '{arguments?.expression}': {ex.Message}"
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

        private Task<object> HandleWeatherTool(dynamic? arguments)
        {
            try
            {
                string location = arguments?.location ?? "Unknown location";

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
                    content = new[]
                    {
                        new
                        {
                            type = "text",
                            text = $"Weather in {weather.location}: {weather.temperature}°C, {weather.condition}, {weather.humidity}% humidity"
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                return Task.FromResult<object>(new
                {
                    isError = true,
                    content = new[]
                    {
                        new
                        {
                            type = "text",
                            text = $"Error getting weather for '{arguments?.location}': {ex.Message}"
                        }
                    }
                });
            }
        }

        private Task<object> HandleListResources()
        {
            var resources = new object[]
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
            };

            return Task.FromResult<object>(new { resources });
        }

        private Task<object> HandleReadResource(object? parameters)
        {
            dynamic? args = parameters;
            string? uri = args?.uri;

            return uri switch
            {
                "file:///readme.txt" => Task.FromResult<object>(new
                {
                    contents = new[]
                    {
                        new
                        {
                            uri,
                            mimeType = "text/plain",
                            text = "# ASP.NET MCP Server\n\nThis is a simple Model Context Protocol server built with ASP.NET Core.\n\nFeatures:\n- Echo tool\n- Timestamp tool\n- Basic calculator\n- Resource reading\n- Prompt templates"
                        }
                    }
                }),
                "file:///config.json" => Task.FromResult<object>(new
                {
                    contents = new[]
                    {
                        new
                        {
                            uri,
                            mimeType = "application/json",
                            text = """
                            {
                              "server": {
                                "name": "ASP.NET MCP Server",
                                "version": "1.0.0",
                                "protocol": "2024-11-05"
                              },
                              "capabilities": {
                                "tools": true,
                                "resources": true,
                                "prompts": true
                              }
                            }
                            """
                        }
                    }
                }),
                _ => throw new ArgumentException($"Resource not found: {uri}")
            };
        }

        private Task<object> HandleListPrompts()
        {
            var prompts = new object[]
            {
                new
                {
                    name = "greeting",
                    description = "Generate a personalized greeting message",
                    arguments = new[]
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
                            description = "Language for the greeting (default: English)",
                            required = false
                        }
                    }
                },
                new
                {
                    name = "code_review",
                    description = "Generate a code review template",
                    arguments = new[]
                    {
                        new
                        {
                            name = "language",
                            description = "Programming language",
                            required = true
                        },
                        new
                        {
                            name = "complexity",
                            description = "Code complexity level (simple, medium, complex)",
                            required = false
                        }
                    }
                }
            };

            return Task.FromResult<object>(new { prompts });
        }

        private Task<object> HandleGetPrompt(object? parameters)
        {
            dynamic? args = parameters;
            string? promptName = args?.name;
            dynamic? promptArgs = args?.arguments;

            return promptName switch
            {
                "greeting" => HandleGreetingPrompt(promptArgs),
                "code_review" => HandleCodeReviewPrompt(promptArgs),
                _ => throw new ArgumentException($"Unknown prompt: {promptName}")
            };
        }

        private Task<object> HandleGreetingPrompt(dynamic? arguments)
        {
            string name = arguments?.name ?? "friend";
            string language = arguments?.language ?? "English";

            var greeting = language.ToLower() switch
            {
                "spanish" => $"¡Hola {name}! ¿Cómo estás?",
                "french" => $"Bonjour {name}! Comment allez-vous?",
                "german" => $"Hallo {name}! Wie geht es Ihnen?",
                _ => $"Hello {name}! How are you doing today?"
            };

            return Task.FromResult<object>(new
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
            });
        }

        private Task<object> HandleCodeReviewPrompt(dynamic? arguments)
        {
            string language = arguments?.language ?? "generic";
            string complexity = arguments?.complexity ?? "medium";

            var template = $"""
            # Code Review Checklist for {language}

            ## General Code Quality
            - [ ] Code follows {language} best practices and conventions
            - [ ] Variable and function names are descriptive and meaningful
            - [ ] Code is properly commented where necessary
            - [ ] No duplicate code or unnecessary complexity

            ## {complexity.ToUpper()} Complexity Specific Checks
            {GetComplexitySpecificChecks(complexity)}

            ## Security & Performance
            - [ ] No obvious security vulnerabilities
            - [ ] Efficient algorithms and data structures used
            - [ ] Proper error handling implemented

            ## Testing
            - [ ] Unit tests cover main functionality
            - [ ] Edge cases are considered
            - [ ] Tests are maintainable and readable
            """;

            return Task.FromResult<object>(new
            {
                description = $"Code review template for {language} ({complexity} complexity)",
                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = new
                        {
                            type = "text",
                            text = template
                        }
                    }
                }
            });
        }

        private string GetComplexitySpecificChecks(string complexity)
        {
            return complexity.ToLower() switch
            {
                "simple" => """
                - [ ] Functions are small and focused
                - [ ] Logic is straightforward and easy to follow
                - [ ] Minimal dependencies
                """,
                "complex" => """
                - [ ] Architecture patterns are appropriately used
                - [ ] Complex logic is well-documented
                - [ ] Performance implications are considered
                - [ ] Scalability concerns are addressed
                - [ ] Integration points are well-defined
                """,
                _ => """
                - [ ] Functions have single responsibility
                - [ ] Reasonable abstraction levels
                - [ ] Dependencies are managed appropriately
                """
            };
        }
    }
}
