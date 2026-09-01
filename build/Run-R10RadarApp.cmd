@echo off
setlocal
cd /d "%~dp0"
dotnet "%~dp0R10RadarApp.dll"
if errorlevel 1 pause