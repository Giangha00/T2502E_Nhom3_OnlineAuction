@echo off
cd /d "%~dp0"
echo SQLite mode: migrations are NOT required.
echo Schema is applied automatically on app startup (Program.cs).
echo.
echo Starting application...
dotnet run
