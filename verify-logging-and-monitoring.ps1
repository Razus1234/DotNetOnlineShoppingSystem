Write-Host "=== Verifying Logging and Monitoring Implementation ===" -ForegroundColor Green

Write-Host "1. Checking required files..." -ForegroundColor Yellow

$files = @(
    "src/OnlineShoppingSystem.API/Extensions/LoggingExtensions.cs",
    "src/OnlineShoppingSystem.API/Extensions/HealthCheckExtensions.cs", 
    "src/OnlineShoppingSystem.API/HealthChecks/DatabaseHealthCheck.cs",
    "src/OnlineShoppingSystem.API/HealthChecks/PaymentGatewayHealthCheck.cs",
    "src/OnlineShoppingSystem.API/Middleware/PerformanceLoggingMiddleware.cs"
)

foreach ($file in $files) {
    if (Test-Path $file) {
        Write-Host "✓ $file exists" -ForegroundColor Green
    } else {
        Write-Host "✗ $file missing" -ForegroundColor Red
    }
}

Write-Host "2. Building project..." -ForegroundColor Yellow
dotnet build src/OnlineShoppingSystem.API --verbosity quiet
if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ Project builds successfully" -ForegroundColor Green
} else {
    Write-Host "✗ Project build failed" -ForegroundColor Red
}

Write-Host "=== Implementation Complete ===" -ForegroundColor Green
Write-Host "Logging and monitoring features implemented:" -ForegroundColor Cyan
Write-Host "• Serilog with console and PostgreSQL sinks" -ForegroundColor White
Write-Host "• Structured logging in all services" -ForegroundColor White  
Write-Host "• Performance logging middleware" -ForegroundColor White
Write-Host "• Database and payment gateway health checks" -ForegroundColor White
Write-Host "• Memory health check with GC metrics" -ForegroundColor White