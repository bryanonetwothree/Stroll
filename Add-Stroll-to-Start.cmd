@echo off
setlocal
set "STROLL_EXE=%~dp0Stroll.exe"
if not exist "%STROLL_EXE%" set "STROLL_EXE=%~dp0dist\Stroll.exe"
if not exist "%STROLL_EXE%" (
  echo Stroll.exe was not found. Build Stroll first or keep this file beside Stroll.exe.
  pause
  exit /b 1
)
start "" /wait "%STROLL_EXE%" --add-shortcut
if errorlevel 1 (
  echo Windows could not create the Start-menu shortcut.
  echo Open Stroll, then Timing, then Add to Start menu to see the error.
  pause
  exit /b 1
)
set "STROLL_SHORTCUT=%APPDATA%\Microsoft\Windows\Start Menu\Programs\Stroll.lnk"
if not exist "%STROLL_SHORTCUT%" (
  echo The shortcut was not found in your Programs folder.
  echo Open Stroll, then Timing, then Add to Start menu to open its location.
  pause
  exit /b 1
)
echo Created: %STROLL_SHORTCUT%
explorer.exe /select,"%STROLL_SHORTCUT%"
echo Right-click Stroll to pin it to Start or the taskbar.
pause
