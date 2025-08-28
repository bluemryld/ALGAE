#!/usr/bin/env pwsh
# Import Game Signatures into ALGAE Database
# This script imports a comprehensive set of popular game signatures

param(
    [string]$DatabasePath = ".\ALGAE\bin\Debug\net8.0-windows\ALGAE.db",
    [switch]$ClearExisting = $false
)

Write-Host "🎮 ALGAE Game Signatures Import Script" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan

# Check if SQLite is available
try {
    $sqliteVersion = sqlite3 --version
    Write-Host "✅ SQLite found: $($sqliteVersion.Split()[0])" -ForegroundColor Green
} catch {
    Write-Host "❌ SQLite not found. Please install SQLite or ensure it's in your PATH." -ForegroundColor Red
    Write-Host "   Download from: https://sqlite.org/download.html" -ForegroundColor Yellow
    exit 1
}

# Check if database exists
if (!(Test-Path $DatabasePath)) {
    Write-Host "❌ Database not found at: $DatabasePath" -ForegroundColor Red
    Write-Host "   Please ensure the ALGAE application has been run at least once to create the database." -ForegroundColor Yellow
    exit 1
}

Write-Host "✅ Database found at: $DatabasePath" -ForegroundColor Green

# Check if SQL file exists
$sqlFile = ".\game_signatures.sql"
if (!(Test-Path $sqlFile)) {
    Write-Host "❌ SQL file not found: $sqlFile" -ForegroundColor Red
    exit 1
}

Write-Host "✅ SQL file found: $sqlFile" -ForegroundColor Green

# Backup database
$backupPath = "$DatabasePath.backup_$(Get-Date -Format 'yyyyMMdd_HHmmss')"
Write-Host "📁 Creating backup: $backupPath" -ForegroundColor Yellow
Copy-Item $DatabasePath $backupPath

# Clear existing signatures if requested
if ($ClearExisting) {
    Write-Host "🗑️ Clearing existing game signatures..." -ForegroundColor Yellow
    sqlite3 $DatabasePath "DELETE FROM GameSignatures;"
    Write-Host "✅ Existing signatures cleared" -ForegroundColor Green
}

# Count existing signatures
$existingCount = sqlite3 $DatabasePath "SELECT COUNT(*) FROM GameSignatures;"
Write-Host "📊 Existing signatures in database: $existingCount" -ForegroundColor Cyan

# Import signatures
Write-Host "📥 Importing game signatures..." -ForegroundColor Yellow
try {
    sqlite3 $DatabasePath ".read $sqlFile"
    Write-Host "✅ Import completed successfully!" -ForegroundColor Green
} catch {
    Write-Host "❌ Error importing signatures: $_" -ForegroundColor Red
    Write-Host "🔄 Restoring backup..." -ForegroundColor Yellow
    Copy-Item $backupPath $DatabasePath
    Write-Host "✅ Backup restored" -ForegroundColor Green
    exit 1
}

# Count new signatures
$newCount = sqlite3 $DatabasePath "SELECT COUNT(*) FROM GameSignatures;"
$addedCount = $newCount - $existingCount

Write-Host "" -ForegroundColor White
Write-Host "📈 Import Summary:" -ForegroundColor Cyan
Write-Host "   Previous count: $existingCount" -ForegroundColor White
Write-Host "   New count: $newCount" -ForegroundColor White
Write-Host "   Added: $addedCount signatures" -ForegroundColor Green

# Show sample of imported signatures
Write-Host "" -ForegroundColor White
Write-Host "🎮 Sample of imported signatures:" -ForegroundColor Cyan
sqlite3 $DatabasePath -header -column "SELECT Name, Publisher, ExecutableName FROM GameSignatures ORDER BY Name LIMIT 10;"

Write-Host "" -ForegroundColor White
Write-Host "✅ Game signatures import completed!" -ForegroundColor Green
Write-Host "   You can now use 'Scan for Games' in ALGAE to detect these games automatically." -ForegroundColor Yellow
Write-Host "   Backup saved at: $backupPath" -ForegroundColor Gray
