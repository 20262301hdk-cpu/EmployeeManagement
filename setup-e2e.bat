@echo off
setlocal ENABLEEXTENSIONS
cd /d "%~dp0"

set "APP_NAME=EmployeeManagement"

echo === E2E setup for %APP_NAME% (reduced) ===
echo.

echo [1/3] Building solution (dotnet build %APP_NAME%.sln)...
dotnet build "%APP_NAME%.sln" -nologo
if errorlevel 1 goto :err_build

echo.
echo [2/3] Installing Chromium for Playwright...
dotnet exec ^
  --runtimeconfig "%APP_NAME%.E2ETests\bin\Debug\net8.0\%APP_NAME%.E2ETests.runtimeconfig.json" ^
  --depsfile      "%APP_NAME%.E2ETests\bin\Debug\net8.0\%APP_NAME%.E2ETests.deps.json" ^
                  "%APP_NAME%.E2ETests\bin\Debug\net8.0\Microsoft.Playwright.dll" ^
  install chromium
if errorlevel 1 goto :err_chromium

echo.
echo [3/3] Starting LocalDB (MSSQLLocalDB)...
sqllocaldb start MSSQLLocalDB
if errorlevel 1 goto :err_localdb

echo.
echo === Setup complete. You can now run dotnet test or open %APP_NAME%.sln in Visual Studio. ===
pause
endlocal
exit /b 0

:err_build
echo.
echo [ERROR] dotnet build failed. Fix compile errors, then rerun setup-e2e.bat.
pause
endlocal
exit /b 1

:err_chromium
echo.
echo [ERROR] Playwright Chromium install failed. Check network connectivity, then rerun setup-e2e.bat.
pause
endlocal
exit /b 1

:err_localdb
echo.
echo [ERROR] Failed to start LocalDB (MSSQLLocalDB). Run `sqllocaldb info MSSQLLocalDB` for details.
pause
endlocal
exit /b 1
