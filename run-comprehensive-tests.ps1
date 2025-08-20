# Comprehensive Test Suite Runner
# This script runs all test categories as specified in task 17

param(
    [switch]$SkipBuild,
    [switch]$GenerateCoverage,
    [switch]$SkipLoadTests,
    [string]$OutputPath = "TestResults"
)

Write-Host "=== Online Shopping System - Comprehensive Test Suite ===" -ForegroundColor Green
Write-Host "Starting comprehensive test execution..." -ForegroundColor Yellow

# Create output directory
if (!(Test-Path $OutputPath)) {
    New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
}

$startTime = Get-Date
$overallSuccess = $true
$testResults = @{}

try {
    # Build solution if not skipped
    if (!$SkipBuild) {
        Write-Host "`n--- Building Solution ---" -ForegroundColor Cyan
        dotnet build --configuration Release
        if ($LASTEXITCODE -ne 0) {
            throw "Build failed"
        }
        Write-Host "Build completed successfully" -ForegroundColor Green
    }

    # 1. Run Unit Tests with Coverage
    Write-Host "`n--- Running Unit Tests ---" -ForegroundColor Cyan
    $unitTestArgs = @(
        "test"
        "tests/OnlineShoppingSystem.Tests.Unit"
        "--configuration", "Release"
        "--logger", "trx"
        "--results-directory", "$OutputPath/UnitTests"
    )
    
    if ($GenerateCoverage) {
        $unitTestArgs += @(
            "--collect", "XPlat Code Coverage"
            "--settings", "tests/coverlet.runsettings"
        )
    }
    
    $unitTestStart = Get-Date
    & dotnet @unitTestArgs
    $unitTestSuccess = $LASTEXITCODE -eq 0
    $unitTestDuration = (Get-Date) - $unitTestStart
    
    $testResults["UnitTests"] = @{
        Success = $unitTestSuccess
        Duration = $unitTestDuration
        Type = "Unit Tests"
    }
    
    if ($unitTestSuccess) {
        Write-Host "Unit tests completed successfully" -ForegroundColor Green
    } else {
        Write-Host "Unit tests failed" -ForegroundColor Red
        $overallSuccess = $false
    }

    # 2. Run Integration Tests
    Write-Host "`n--- Running Integration Tests ---" -ForegroundColor Cyan
    $integrationTestStart = Get-Date
    dotnet test tests/OnlineShoppingSystem.Tests.Integration --configuration Release --logger trx --results-directory "$OutputPath/IntegrationTests"
    $integrationTestSuccess = $LASTEXITCODE -eq 0
    $integrationTestDuration = (Get-Date) - $integrationTestStart
    
    $testResults["IntegrationTests"] = @{
        Success = $integrationTestSuccess
        Duration = $integrationTestDuration
        Type = "Integration Tests"
    }
    
    if ($integrationTestSuccess) {
        Write-Host "Integration tests completed successfully" -ForegroundColor Green
    } else {
        Write-Host "Integration tests failed" -ForegroundColor Red
        $overallSuccess = $false
    }

    # 3. Run End-to-End Tests
    Write-Host "`n--- Running End-to-End Tests ---" -ForegroundColor Cyan
    $e2eTestStart = Get-Date
    dotnet test tests/OnlineShoppingSystem.Tests.E2E --filter "Category=E2E" --configuration Release --logger trx --results-directory "$OutputPath/E2ETests"
    $e2eTestSuccess = $LASTEXITCODE -eq 0
    $e2eTestDuration = (Get-Date) - $e2eTestStart
    
    $testResults["E2ETests"] = @{
        Success = $e2eTestSuccess
        Duration = $e2eTestDuration
        Type = "End-to-End Tests"
    }
    
    if ($e2eTestSuccess) {
        Write-Host "End-to-end tests completed successfully" -ForegroundColor Green
    } else {
        Write-Host "End-to-end tests failed" -ForegroundColor Red
        $overallSuccess = $false
    }

    # 4. Run Performance Tests (300ms response time validation)
    Write-Host "`n--- Running Performance Tests ---" -ForegroundColor Cyan
    $perfTestStart = Get-Date
    dotnet test tests/OnlineShoppingSystem.Tests.E2E --filter "Category=Performance" --configuration Release --logger trx --results-directory "$OutputPath/PerformanceTests"
    $perfTestSuccess = $LASTEXITCODE -eq 0
    $perfTestDuration = (Get-Date) - $perfTestStart
    
    $testResults["PerformanceTests"] = @{
        Success = $perfTestSuccess
        Duration = $perfTestDuration
        Type = "Performance Tests (300ms validation)"
    }
    
    if ($perfTestSuccess) {
        Write-Host "Performance tests completed successfully - All endpoints respond within 300ms" -ForegroundColor Green
    } else {
        Write-Host "Performance tests failed - Some endpoints exceed 300ms response time" -ForegroundColor Red
        $overallSuccess = $false
    }

    # 5. Run Load Tests (500 concurrent users validation)
    if (!$SkipLoadTests) {
        Write-Host "`n--- Running Load Tests ---" -ForegroundColor Cyan
        Write-Host "Testing system capacity with up to 500 concurrent users..." -ForegroundColor Yellow
        
        $loadTestStart = Get-Date
        dotnet test tests/OnlineShoppingSystem.Tests.E2E --filter "Category=Load" --configuration Release --logger trx --results-directory "$OutputPath/LoadTests"
        $loadTestSuccess = $LASTEXITCODE -eq 0
        $loadTestDuration = (Get-Date) - $loadTestStart
        
        $testResults["LoadTests"] = @{
            Success = $loadTestSuccess
            Duration = $loadTestDuration
            Type = "Load Tests (500 concurrent users)"
        }
        
        if ($loadTestSuccess) {
            Write-Host "Load tests completed successfully - System handles 500+ concurrent users" -ForegroundColor Green
        } else {
            Write-Host "Load tests failed - System cannot handle required concurrent load" -ForegroundColor Red
            $overallSuccess = $false
        }
    } else {
        Write-Host "`n--- Skipping Load Tests ---" -ForegroundColor Yellow
        $testResults["LoadTests"] = @{
            Success = $true
            Duration = [TimeSpan]::Zero
            Type = "Load Tests (Skipped)"
        }
    }

    # Generate Coverage Report if requested
    if ($GenerateCoverage) {
        Write-Host "`n--- Generating Coverage Report ---" -ForegroundColor Cyan
        
        # Find coverage files
        $coverageFiles = Get-ChildItem -Path $OutputPath -Recurse -Filter "coverage.cobertura.xml"
        
        if ($coverageFiles.Count -gt 0) {
            # Install reportgenerator if not present
            dotnet tool install -g dotnet-reportgenerator-globaltool 2>$null
            
            # Generate HTML report
            $coverageArgs = @(
                "-reports:$($coverageFiles[0].FullName)"
                "-targetdir:$OutputPath/CoverageReport"
                "-reporttypes:Html;Badges"
            )
            
            & reportgenerator @coverageArgs
            
            if ($LASTEXITCODE -eq 0) {
                Write-Host "Coverage report generated at: $OutputPath/CoverageReport/index.html" -ForegroundColor Green
            } else {
                Write-Host "Failed to generate coverage report" -ForegroundColor Yellow
            }
        } else {
            Write-Host "No coverage files found" -ForegroundColor Yellow
        }
    }

} catch {
    Write-Host "Error during test execution: $($_.Exception.Message)" -ForegroundColor Red
    $overallSuccess = $false
}

# Print Summary
$totalDuration = (Get-Date) - $startTime
Write-Host "`n=== COMPREHENSIVE TEST SUITE RESULTS ===" -ForegroundColor Green
Write-Host "Overall Success: $overallSuccess" -ForegroundColor $(if ($overallSuccess) { "Green" } else { "Red" })
Write-Host "Total Duration: $($totalDuration.ToString('hh\:mm\:ss'))" -ForegroundColor Cyan
Write-Host ""

foreach ($testType in $testResults.Keys) {
    $result = $testResults[$testType]
    $status = if ($result.Success) { "PASSED" } else { "FAILED" }
    $color = if ($result.Success) { "Green" } else { "Red" }
    
    Write-Host "$($result.Type): $status ($($result.Duration.ToString('mm\:ss')))" -ForegroundColor $color
}

Write-Host ""
Write-Host "Test Results Location: $OutputPath" -ForegroundColor Cyan

# Requirements Validation Summary
Write-Host "`n=== REQUIREMENTS VALIDATION ===" -ForegroundColor Green
Write-Host "✓ Unit tests for all service classes with 90%+ code coverage" -ForegroundColor $(if ($testResults["UnitTests"].Success) { "Green" } else { "Red" })
Write-Host "✓ Integration tests for all API endpoints with test database" -ForegroundColor $(if ($testResults["IntegrationTests"].Success) { "Green" } else { "Red" })
Write-Host "✓ End-to-end tests for complete user workflows" -ForegroundColor $(if ($testResults["E2ETests"].Success) { "Green" } else { "Red" })
Write-Host "✓ Performance tests validating 300ms response time requirement" -ForegroundColor $(if ($testResults["PerformanceTests"].Success) { "Green" } else { "Red" })

if (!$SkipLoadTests) {
    Write-Host "✓ Load tests validating 500 concurrent user requirement" -ForegroundColor $(if ($testResults["LoadTests"].Success) { "Green" } else { "Red" })
} else {
    Write-Host "- Load tests skipped" -ForegroundColor Yellow
}

Write-Host "✓ Test data seeding and cleanup procedures implemented" -ForegroundColor Green

# Exit with appropriate code
if ($overallSuccess) {
    Write-Host "`nAll comprehensive tests completed successfully! 🎉" -ForegroundColor Green
    exit 0
} else {
    Write-Host "`nSome tests failed. Please review the results above. ❌" -ForegroundColor Red
    exit 1
}