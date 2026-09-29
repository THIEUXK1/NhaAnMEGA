@echo off
rem Vo boc cho Task Scheduler: chay tool va noi output vao logs\dongbo-<ngay>.log
setlocal
set "THUMUC=%~dp0"
for /f %%i in ('powershell -NoProfile -Command "Get-Date -Format yyyyMMdd"') do set "NGAY=%%i"
if not exist "%THUMUC%logs" mkdir "%THUMUC%logs"
set "FILELOG=%THUMUC%logs\dongbo-%NGAY%.log"
echo.>> "%FILELOG%"
echo ===== %DATE% %TIME% =====>> "%FILELOG%"
"%THUMUC%DongBoSuatAnPF.exe" %* 1>> "%FILELOG%" 2>&1
exit /b %ERRORLEVEL%
