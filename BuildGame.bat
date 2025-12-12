@echo off
setlocal

:: Define variables
set UNITY_VERSION=6000.3.1f1
set UNITY_PROJECT_PATH=%cd%
set BUILD_PATH=%cd%\..\TicTacToeBuild
set UNITY_HUB_URL=https://public-cdn.cloud.unity3d.com/hub/prod/UnityHubSetup.exe
set UNITY_HUB_PATH=%ProgramFiles%\Unity Hub\Unity Hub.exe
set UNITY_EXE_PATH=%LOCALAPPDATA%\Unity\Hub\%UNITY_VERSION%\Editor\Unity.exe

:: Check if Unity Hub is installed
if not exist "%UNITY_HUB_PATH%" (
    echo Unity Hub not found. Installing Unity Hub...
    set TEMP_HUB_SETUP=%TEMP%\UnityHubSetup.exe

    :: Download Unity Hub installer
    powershell -Command "Invoke-WebRequest -Uri %UNITY_HUB_URL% -OutFile %TEMP_HUB_SETUP%"
    if %errorlevel% neq 0 (
        echo Failed to download Unity Hub installer.
        exit /b 1
    )

    :: Install Unity Hub
    echo Installing Unity Hub...
    start /wait "" %TEMP_HUB_SETUP% /S
    if %errorlevel% neq 0 (
        echo Unity Hub installation failed.
        exit /b 1
    )

    :: Clean up installer file
    del %TEMP_HUB_SETUP%
) else (
    echo Unity Hub is already installed.
)

:: Check if the specified Unity version is installed
echo Checking if Unity version %UNITY_VERSION% is installed...
"%UNITY_HUB_PATH%" -- --headless editors --installed | find "%UNITY_VERSION%" >nul
if %errorlevel% neq 0 (
    echo Unity version %UNITY_VERSION% not found. Installing...
    "%UNITY_HUB_PATH%" -- --headless install --version %UNITY_VERSION% --changeset 42618b75d40d
    if %errorlevel% neq 0 (
        echo Failed to install Unity version %UNITY_VERSION%.
        exit /b 1
    )
) else (
    echo Unity version %UNITY_VERSION% is already installed.
)

:: Create the build directory if it doesn't exist
if not exist "%BUILD_PATH%" (
    mkdir "%BUILD_PATH%"
)

:: Run Unity in batch mode to build the project
echo Building the Unity project for Windows...
"%UNITY_EXE_PATH%" -quit -batchmode -projectPath "%UNITY_PROJECT_PATH%" -buildWindows64Player "%BUILD_PATH%\Game.exe" -nographics

if %errorlevel% neq 0 (
    echo Build failed.
    exit /b 1
)

echo Build succeeded.
echo Build output located in %BUILD_PATH%

endlocal
pause