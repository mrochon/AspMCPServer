#!/usr/bin/env pwsh

# Simple test script for the ASP.NET MCP Server
# This script demonstrates how to interact with the MCP server using PowerShell

$baseUrl = "https://localhost:5001"

Write-Host "Testing ASP.NET MCP Server at $baseUrl" -ForegroundColor Green
Write-Host "=" * 50

# Test 1: Get server info via REST API
Write-Host "`n1. Testing REST API - Server Info:" -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/mcp/info" -Method Get -SkipCertificateCheck
    $response | ConvertTo-Json -Depth 3
} catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 2: List tools via REST API
Write-Host "`n2. Testing REST API - List Tools:" -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/mcp/tools" -Method Get -SkipCertificateCheck
    $response | ConvertTo-Json -Depth 3
} catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 3: Call echo tool via REST API
Write-Host "`n3. Testing REST API - Echo Tool:" -ForegroundColor Yellow
try {
    $body = @{ text = "Hello from PowerShell!" } | ConvertTo-Json
    $response = Invoke-RestMethod -Uri "$baseUrl/api/mcp/tools/echo" -Method Post -Body $body -ContentType "application/json" -SkipCertificateCheck
    $response | ConvertTo-Json -Depth 3
} catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 4: MCP JSON-RPC - Initialize
Write-Host "`n4. Testing MCP JSON-RPC - Initialize:" -ForegroundColor Yellow
try {
    $initRequest = @{
        jsonrpc = "2.0"
        id = "1"
        method = "initialize"
        params = @{
            protocolVersion = "2024-11-05"
            capabilities = @{}
            clientInfo = @{
                name = "PowerShell Test Client"
                version = "1.0.0"
            }
        }
    } | ConvertTo-Json -Depth 4

    $response = Invoke-RestMethod -Uri "$baseUrl/mcp" -Method Post -Body $initRequest -ContentType "application/json" -SkipCertificateCheck
    $response | ConvertTo-Json -Depth 3
} catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 5: MCP JSON-RPC - List Tools
Write-Host "`n5. Testing MCP JSON-RPC - List Tools:" -ForegroundColor Yellow
try {
    $listToolsRequest = @{
        jsonrpc = "2.0"
        id = "2"
        method = "tools/list"
    } | ConvertTo-Json

    $response = Invoke-RestMethod -Uri "$baseUrl/mcp" -Method Post -Body $listToolsRequest -ContentType "application/json" -SkipCertificateCheck
    $response | ConvertTo-Json -Depth 4
} catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 6: MCP JSON-RPC - Call Echo Tool
Write-Host "`n6. Testing MCP JSON-RPC - Call Echo Tool:" -ForegroundColor Yellow
try {
    $callToolRequest = @{
        jsonrpc = "2.0"
        id = "3"
        method = "tools/call"
        params = @{
            name = "echo"
            arguments = @{
                text = "Hello from MCP JSON-RPC!"
            }
        }
    } | ConvertTo-Json -Depth 3

    $response = Invoke-RestMethod -Uri "$baseUrl/mcp" -Method Post -Body $callToolRequest -ContentType "application/json" -SkipCertificateCheck
    $response | ConvertTo-Json -Depth 4
} catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 7: MCP JSON-RPC - Call Timestamp Tool
Write-Host "`n7. Testing MCP JSON-RPC - Call Timestamp Tool:" -ForegroundColor Yellow
try {
    $timestampRequest = @{
        jsonrpc = "2.0"
        id = "4"
        method = "tools/call"
        params = @{
            name = "timestamp"
            arguments = @{}
        }
    } | ConvertTo-Json -Depth 3

    $response = Invoke-RestMethod -Uri "$baseUrl/mcp" -Method Post -Body $timestampRequest -ContentType "application/json" -SkipCertificateCheck
    $response | ConvertTo-Json -Depth 4
} catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 8: MCP JSON-RPC - Call Weather Tool
Write-Host "`n8. Testing MCP JSON-RPC - Call Weather Tool:" -ForegroundColor Yellow
try {
    $weatherRequest = @{
        jsonrpc = "2.0"
        id = "5"
        method = "tools/call"
        params = @{
            name = "weather"
            arguments = @{
                location = "New York"
            }
        }
    } | ConvertTo-Json -Depth 3

    $response = Invoke-RestMethod -Uri "$baseUrl/mcp" -Method Post -Body $weatherRequest -ContentType "application/json" -SkipCertificateCheck
    $response | ConvertTo-Json -Depth 4
} catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n" + "=" * 50
Write-Host "Testing completed!" -ForegroundColor Green
Write-Host "Visit $baseUrl/swagger for interactive API documentation" -ForegroundColor Cyan
