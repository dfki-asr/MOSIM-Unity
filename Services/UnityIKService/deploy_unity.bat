@echo off
REM SPDX-License-Identifier: MIT
REM The content of this file has been developed in the context of the MOSIM research project.
REM Original author(s): Janis Sprenger

ECHO.
ECHO _______________________________________________________
ECHO [33mdeploy_unity.bat[0m at %cd%\deploy_unity.bat Deploying the Unity IK Service project. 
ECHO _______________________________________________________
ECHO.


if not defined MOSIM_UNITY (
  ECHO [31mMOSIM_UNITY Environment variable pointing to the Unity.exe for Unity version 2019.18.1f1 is missing.[0m
  ECHO    e.g. SETS MOSIM_UNITY "C:\Program Files\Unity Environments\2018.4.1f1\Editor\Unity.exe\"
  ECHO MOSIM_UNITY defined as: "%MOSIM_UNITY%"
  pause
  exit /b 1
) else (
  if not exist "%MOSIM_UNITY%" (
    ECHO Unity does not seem to be installed at "%MOSIM_UNITY%" or path name in deploy_variables.bat is wrong.
    exit /b 2
  )
)

IF EXIST build (
  RD /S/Q build
)

REM Build Unity Project:
ECHO Building the  Unity IK Service project. This step may take some while, so please wait...
if "%MOSIM_DEPLOY_LINUX%"=="1" (
	ECHO ... Deploying for Linux
	call "%MOSIM_UNITY%" -quit -batchmode -logFile build.log -projectPath "." -executeMethod BuildIKService.CreateServerBuildLinux
) else (
	ECHO ... Deploying for Windows
	call "%MOSIM_UNITY%" -quit -batchmode -logFile build.log -projectPath "." -executeMethod BuildIKService.CreateServerBuild 
)

if %ERRORLEVEL% EQU 0 (
  IF EXIST build\configurations (
    RD /S/Q build\configurations
  )
  MD build\configurations
  COPY .\Assets\configurations\avatar.mos build\configurations\
  COPY .\description.json .\build\
  ECHO [92mSuccessfully deployed  Unity IK Service[0m
  exit /b 0
) else (
  ECHO [31mDeployment of  Unity IK Service failed. Please consider the build.log for more information. [0m
  exit /b 1
)