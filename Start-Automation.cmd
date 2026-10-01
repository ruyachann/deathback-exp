@echo off
setlocal
cd /d "%~dp0"
if exist "%LOCALAPPDATA%\Programs\Python\Python312\python.exe" (
  "%LOCALAPPDATA%\Programs\Python\Python312\python.exe" "Tools\automation.py" %*
) else (
  py -3 "Tools\automation.py" %*
)
exit /b %errorlevel%
