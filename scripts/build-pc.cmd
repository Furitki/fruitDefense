@echo off
setlocal
for %%I in ("%~dp0..") do set "PROJECT_ROOT=%%~fI"
set "UNITY_PATH=C:\Program Files\Unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe"
set "BUILD_OUTPUT=%PROJECT_ROOT%\Builds\Windows\FruitDefense.exe"
set "BUILD_LOG=%PROJECT_ROOT%\Logs\quick-pc-build.log"

echo Building a quick Fruit Defense Windows PC preview...
echo This shortcut skips the P0 release gate.
echo Close the Unity Editor before continuing so the project is not locked.
echo.

if not exist "%UNITY_PATH%" goto unity_missing
if not exist "%PROJECT_ROOT%\Builds\Windows" mkdir "%PROJECT_ROOT%\Builds\Windows"
if not exist "%PROJECT_ROOT%\Logs" mkdir "%PROJECT_ROOT%\Logs"

start "" /wait "%UNITY_PATH%" -batchmode -nographics -quit -projectPath "%PROJECT_ROOT%" -buildWindows64Player "%BUILD_OUTPUT%" -logFile "%BUILD_LOG%"
set "BUILD_EXIT_CODE=%ERRORLEVEL%"

echo.
if not "%BUILD_EXIT_CODE%"=="0" goto build_failed
findstr /C:"Build Finished, Result: Success." "%BUILD_LOG%" >nul
if errorlevel 1 goto build_result_missing
if not exist "%BUILD_OUTPUT%" goto build_output_missing

echo PC build completed successfully.
echo Output: %BUILD_OUTPUT%
pause
exit /b 0

:build_failed
echo PC build failed with exit code %BUILD_EXIT_CODE%.
echo Review the build log: %BUILD_LOG%
pause
exit /b %BUILD_EXIT_CODE%

:build_result_missing
echo Unity exited without the expected successful build result.
echo Review the build log: %BUILD_LOG%
pause
exit /b 1

:build_output_missing
echo Unity reported success, but the expected executable is missing.
echo Expected output: %BUILD_OUTPUT%
echo Review the build log: %BUILD_LOG%
pause
exit /b 1

:unity_missing
echo Required Unity editor was not found:
echo %UNITY_PATH%
pause
exit /b 1
