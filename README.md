# ASP.NET MCP Server

A comprehensive Model Context Protocol (MCP) server implementation using ASP.NET Core and C#, providing multiple communication patterns to suit different client needs.

## 🚀 Features

- **Multiple Communication Patterns**: REST, HTTP+SSE, and WebSocket implementations
- **MCP JSON-RPC Protocol**: Full compliance with MCP specification
- **Real-time Communication**: WebSocket and Server-Sent Events support
- **Multiple Tools**: Built-in tools for echo, timestamp, weather simulation, and calculations
- **Resource Management**: File-based resource serving
- **Prompt Templates**: Customizable templates for various use cases
- **Cross-Origin Support**: CORS enabled for web client integration
- **API Documentation**: Swagger UI for testing and exploration

## 📋 Architecture Overview

This server implements **four different communication patterns** to provide maximum flexibility for different client scenarios:

### 1. **McpController** - Simple REST API
**Best for**: Testing, simple integrations, non-MCP clients
- Traditional HTTP endpoints
- Easy to test and integrate
- No MCP protocol knowledge required

### 2. **McpStreamController** - HTTP + Server-Sent Events
**Best for**: MCP clients without WebSocket support, corporate firewall environments
- JSON-RPC over HTTP for commands
- SSE for server updates and monitoring
- Works in restricted network environments

### 3. **McpWebSocketController** - Full Bidirectional WebSocket
**Best for**: Real-time applications, proper MCP implementation
- True bidirectional communication
- Low latency
- Full MCP protocol compliance

### 4. **StdioMcpServer** - Standard Input/Output Transport
**Best for**: CLI tools, MCP clients, integration with editors and LSPs
- Communicates via stdin/stdout
- Compatible with MCP client libraries
- Perfect for automation and scripting

## 🔧 API Endpoints

### McpController (REST API)
```
GET    /api/mcp/info              - Server information
GET    /api/mcp/tools             - List available tools
POST   /api/mcp/tools/{toolName}  - Execute a specific tool
GET    /api/mcp/resources         - List available resources
GET    /api/mcp/prompts           - List available prompts
```

### McpStreamController (HTTP + SSE)
```
POST   /mcp                       - JSON-RPC commands
GET    /mcp/sse                   - Server-Sent Events stream
```

### McpWebSocketController (WebSocket)
```
WS     /mcp/ws                    - WebSocket connection
```

### StdioMcpServer (Standard I/O)
```
CLI    dotnet run -- --stdio      - Stdio transport mode
```

## 🛠️ Available Tools

All controllers provide the same four tools:

| Tool | Description | Parameters |
|------|-------------|------------|
| **echo** | Echo back input text | `text: string` |
| **timestamp** | Get current UTC timestamp | None |
| **weather** | Get weather for location (simulated) | `location: string` |
| **calculate** | Basic arithmetic calculations | `expression: string` |

## 📚 Usage Examples

### 1. REST API Usage (McpController)

**Get server info:**
```bash
curl https://localhost:7140/api/mcp/info
```

**List tools:**
```bash
curl https://localhost:7140/api/mcp/tools
```

**Call echo tool:**
```bash
curl -X POST https://localhost:7140/api/mcp/tools/echo \
  -H "Content-Type: application/json" \
  -d '{"text": "Hello World!"}'
```

### 2. HTTP + SSE Usage (McpStreamController)

**Connect to SSE for updates:**
```javascript
const eventSource = new EventSource('https://localhost:7140/mcp/sse');

eventSource.addEventListener('capabilities', (event) => {
  console.log('Server capabilities:', JSON.parse(event.data));
});

eventSource.addEventListener('heartbeat', (event) => {
  console.log('Heartbeat:', JSON.parse(event.data));
});
```

**Send JSON-RPC commands:**
```javascript
const response = await fetch('https://localhost:7140/mcp', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    jsonrpc: "2.0",
    id: "1",
    method: "tools/list"
  })
});
```

**Full JSON-RPC examples:**
```json
// Initialize
{
  "jsonrpc": "2.0",
  "id": "1",
  "method": "initialize"
}

// List tools
{
  "jsonrpc": "2.0",
  "id": "2", 
  "method": "tools/list"
}

// Call tool
{
  "jsonrpc": "2.0",
  "id": "3",
  "method": "tools/call",
  "params": {
    "name": "echo",
    "arguments": {
      "text": "Hello, MCP!"
    }
  }
}
```

### 3. WebSocket Usage (McpWebSocketController)

```javascript
const ws = new WebSocket('wss://localhost:7140/mcp/ws');

ws.onopen = () => {
  // Send initialize message
  ws.send(JSON.stringify({
    jsonrpc: "2.0",
    id: "1",
    method: "initialize"
  }));
};

ws.onmessage = (event) => {
  const response = JSON.parse(event.data);
  console.log('Server response:', response);
  
  if (response.id === "1") {
    // Server initialized, now list tools
    ws.send(JSON.stringify({
      jsonrpc: "2.0",
      id: "2",
      method: "tools/list"
    }));
  }
};
```

### 4. Stdio Transport Usage (StdioMcpServer)

**Run in stdio mode:**
```bash
# Build the project
dotnet build

# Run the stdio MCP server
dotnet run --no-launch-profile -- --stdio
```

**Send requests via echo/pipe:**
```bash
# Initialize server
echo '{"jsonrpc": "2.0", "id": "1", "method": "initialize"}' | dotnet run --no-launch-profile -- --stdio

# List available tools
echo '{"jsonrpc": "2.0", "id": "2", "method": "tools/list"}' | dotnet run --no-launch-profile -- --stdio

# Call echo tool
echo '{"jsonrpc": "2.0", "id": "3", "method": "tools/call", "params": {"name": "echo", "arguments": {"text": "Hello stdio!"}}}' | dotnet run --no-launch-profile -- --stdio
```

**Interactive mode:**
```bash
# Start server in interactive mode
dotnet run -- --stdio

# Then type JSON-RPC messages line by line:
{"jsonrpc": "2.0", "id": "1", "method": "initialize"}
{"jsonrpc": "2.0", "id": "2", "method": "tools/list"}
```

**Test script:**
```bash
# Run comprehensive tests
./test-stdio.ps1
```

## 🚦 Getting Started

### Prerequisites
- .NET 8.0 SDK
- Visual Studio or VS Code (optional)

### Installation & Running

1. **Clone the project:**
   ```bash
   git clone <repository-url>
   cd AspMCPServer
   ```

2. **Restore dependencies:**
   ```bash
   dotnet restore
   ```

3. **Run the server:**
   ```bash
   dotnet run
   ```

4. **Access the server:**
   - Server runs at: `https://localhost:7140`
   - Swagger UI: `https://localhost:7140/swagger`
   - Test page: `https://localhost:7140/communication-demo.html`

### Testing

**Using the built-in test page:**
1. Navigate to `https://localhost:7140/communication-demo.html`
2. Test all three communication patterns interactively

**Using PowerShell (included script):**
```powershell
.\test-mcp-server.ps1
```

**Using Swagger UI:**
1. Go to `https://localhost:7140/swagger`
2. Test the REST API endpoints

## 🏗️ Project Structure

```
AspMCPServer/
├── Controllers/
│   ├── McpController.cs           # REST API endpoints
│   ├── McpStreamController.cs     # HTTP + SSE implementation  
│   └── McpWebSocketController.cs  # WebSocket implementation
├── Services/
│   ├── SimpleMcpHandler.cs        # Business logic handler
│   └── SimpleMcpServer.cs         # Alternative service implementation
├── wwwroot/
│   └── communication-demo.html    # Interactive test page
├── test-mcp-server.ps1            # PowerShell test script
├── Program.cs                     # Application configuration
└── README.md                      # This documentation
```

## 🔀 When to Use Each Controller

### Use **McpController** when:
- ✅ Building simple integrations
- ✅ Testing individual tools
- ✅ Client doesn't support MCP protocol
- ✅ Need RESTful semantics
- ✅ Prototyping or development

### Use **McpStreamController** when:
- ✅ Client supports MCP but not WebSocket
- ✅ Corporate firewall blocks WebSocket
- ✅ Need server status monitoring via SSE
- ✅ Prefer HTTP request/response pattern
- ✅ Building web applications with updates

### Use **McpWebSocketController** when:
- ✅ Need real-time bidirectional communication
- ✅ Building proper MCP client
- ✅ Want lowest latency
- ✅ Client supports WebSocket
- ✅ Building interactive applications

### Use **StdioMcpServer** when:
- ✅ Building CLI tools and automation scripts
- ✅ Integrating with editors and LSPs
- ✅ Need standard MCP client compatibility
- ✅ Working in headless environments
- ✅ Building command-line MCP clients
- ✅ Need process-to-process communication

## 📦 Dependencies

```xml
<PackageReference Include="ModelContextProtocol.AspNetCore" Version="0.3.0-preview.4" />
<PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="8.0.0" />
<PackageReference Include="Swashbuckle.AspNetCore" Version="6.5.0" />
```

## ⚙️ Configuration

The server is configured with:
- **CORS**: Enabled for all origins (configure for production)
- **WebSocket**: 2-minute keep-alive interval
- **Static Files**: Serves test pages from `wwwroot`
- **Swagger**: Enabled in development mode
- **JSON Options**: Configured for MCP message format

## 🔧 Extending the Server

### Adding New Tools

1. **Update all controllers** to include the new tool:
   ```csharp
   // In HandleListTools() methods
   new {
       name = "mytool",
       description = "My custom tool",
       inputSchema = new { /* schema */ }
   }
   ```

2. **Implement tool logic** in each controller:
   ```csharp
   // In HandleCallTool() switch statements
   "mytool" => HandleMyTool(id, argumentsElement),
   ```

3. **Add the handler method:**
   ```csharp
   private object HandleMyTool(string? id, JsonElement args) {
       // Implementation
   }
   ```

### Adding New Resources

Update `HandleListResources()` methods:
```csharp
new {
    uri = "file:///myresource.txt",
    name = "My Resource",
    description = "Custom resource",
    mimeType = "text/plain"
}
```

## 🔐 Production Considerations

- **Authentication**: Add JWT or API key authentication
- **Rate Limiting**: Implement request throttling
- **CORS**: Configure specific allowed origins
- **HTTPS**: Use proper SSL certificates
- **Logging**: Add structured logging
- **Error Handling**: Implement comprehensive error responses
- **Health Checks**: Add monitoring endpoints
- **Validation**: Add input validation and sanitization

## 📄 License

This project is provided as a sample implementation for educational and demonstration purposes.

---

**Note**: The `ModelContextProtocol.AspNetCore` package is in preview. APIs may change in future versions.
