// This file shows the current structure - we'll update it based on the actual package API
using Microsoft.AspNetCore.Mvc;

namespace AspMCPServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class McpController : ControllerBase
    {
        [HttpGet("info")]
        public IActionResult GetServerInfo()
        {
            return Ok(new
            {
                name = "Simple ASP.NET MCP Server",
                version = "1.0.0",
                protocolVersion = "1.0.0"
            });
        }

        [HttpGet("tools")]
        public IActionResult ListTools()
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
                }
            };

            return Ok(tools);
        }

        [HttpPost("tools/{toolName}")]
        public IActionResult CallTool(string toolName, [FromBody] object? arguments)
        {
            return toolName switch
            {
                "echo" => HandleEchoTool(arguments),
                "timestamp" => HandleTimestampTool(),
                "weather" => HandleWeatherTool(arguments),
                "calculate" => HandleCalculateTool(arguments),
                _ => BadRequest(new { error = $"Unknown tool: {toolName}" })
            };
        }

        private IActionResult HandleEchoTool(object? arguments)
        {
            try
            {
                dynamic? args = arguments;
                string text = args?.text?.ToString() ?? "No text provided";

                return Ok(new
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
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    error = $"Error processing echo tool: {ex.Message}"
                });
            }
        }

        private IActionResult HandleTimestampTool()
        {
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

            return Ok(new
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

        private IActionResult HandleWeatherTool(object? arguments)
        {
            try
            {
                dynamic? args = arguments;
                string location = args?.location?.ToString() ?? "Unknown location";

                // Simulate weather data
                var weather = new
                {
                    location,
                    temperature = new Random().Next(-10, 35),
                    condition = new[] { "Sunny", "Cloudy", "Rainy", "Snowy" }[new Random().Next(4)],
                    humidity = new Random().Next(30, 90)
                };

                return Ok(new
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
                return BadRequest(new
                {
                    error = $"Error processing weather tool: {ex.Message}"
                });
            }
        }

        private IActionResult HandleCalculateTool(object? arguments)
        {
            try
            {
                dynamic? args = arguments;
                string expression = args?.expression?.ToString() ?? "0";
                
                // Simple calculator - in production, use a proper expression evaluator
                var result = EvaluateSimpleExpression(expression);

                return Ok(new
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
                return BadRequest(new
                {
                    error = $"Error calculating '{arguments?.GetType().GetProperty("expression")?.GetValue(arguments)}': {ex.Message}"
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
    }
}
