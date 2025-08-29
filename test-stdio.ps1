# Test script for MCP Server stdio transport
# This script demonstrates how to interact with the MCP server via stdin/stdout

Write-Host "Testing MCP Server stdio transport..." -ForegroundColor Green
Write-Host "Building the project first..." -ForegroundColor Yellow

# Build the project
dotnet build
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host "Starting stdio test..." -ForegroundColor Green

# Create a temporary script to send messages
$testScript = @"
{`"jsonrpc`": `"2.0`", `"id`": `"1`", `"method`": `"initialize`", `"params`": {`"protocolVersion`": `"2024-11-05`", `"clientInfo`": {`"name`": `"PowerShell Test Client`", `"version`": `"1.0.0`"}}}
{`"jsonrpc`": `"2.0`", `"method`": `"initialized`"}
{`"jsonrpc`": `"2.0`", `"id`": `"2`", `"method`": `"tools/list`"}
{`"jsonrpc`": `"2.0`", `"id`": `"3`", `"method`": `"tools/call`", `"params`": {`"name`": `"echo`", `"arguments`": {`"text`": `"Hello from stdio!`"}}}
{`"jsonrpc`": `"2.0`", `"id`": `"4`", `"method`": `"tools/call`", `"params`": {`"name`": `"timestamp`"}}
{`"jsonrpc`": `"2.0`", `"id`": `"5`", `"method`": `"resources/list`"}
{`"jsonrpc`": `"2.0`", `"id`": `"6`", `"method`": `"prompts/list`"}
"@

# Save test script to temp file
$tempFile = [System.IO.Path]::GetTempFileName()
Set-Content -Path $tempFile -Value $testScript

try {
    Write-Host "Sending test messages to MCP server..." -ForegroundColor Cyan
    Write-Host "Server output:" -ForegroundColor Yellow
    
    # Run the server in stdio mode and pipe the test input
    Get-Content $tempFile | dotnet run --project . -- --stdio
    
    Write-Host "`nTest completed!" -ForegroundColor Green
}
catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
}
finally {
    # Clean up temp file
    Remove-Item $tempFile -ErrorAction SilentlyContinue
}

Write-Host "`nTo manually test stdio mode:" -ForegroundColor Cyan
Write-Host "  dotnet run -- --stdio" -ForegroundColor White
Write-Host "Then paste JSON-RPC messages line by line, for example:" -ForegroundColor Cyan
Write-Host '  {"jsonrpc": "2.0", "id": "1", "method": "initialize"}' -ForegroundColor White
