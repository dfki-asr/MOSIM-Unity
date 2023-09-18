@echo off
REM SPDX-License-Identifier: MIT
REM The content of this file has been developed in the context of the MOSIM research project.
REM Original author(s): Bhuvaneshwaran Ilanthirayan

IF EXIST build (
  RD /S/Q build
)

if %ERRORLEVEL% EQU 0 (
  ECHO [92mSuccessfully cleaned UnityIKService[0m
  exit /b 0
) else (
  ECHO [31mCleaning of UnityIKService failed. Please consider the build.log for more information. [0m
  exit /b 1
)