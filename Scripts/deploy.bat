@echo off

REM SPDX-License-Identifier: MIT
REM The content of this file has been developed in the context of the MOSIM research project.
REM Original author(s): Janis Sprenger, Bhuvaneshwaran Ilanthirayan, Klaus Fischer

REM This is a deploy script to auto-generate the components of the MOSIM-CSharp projects and move them to a new environment folder. 

SET VERBOSE=0

call :argparse %*

goto :eof

REM call :safeCall deploy_variables.bat "There has been an error when setting the deploy vaiables!"

REM if not exist %BUILDENV% (
REM   md %BUILDENV%
REM )

REM COPY Scripts\enableFirewall.exe .\build\

REM echo Removing doublicated MMUs
pause
REM call .\remove_double_mmus.bat

echo Removing doublicated MMUs done.

REM the link currently does not yet work. 
REM RD build\
REM 
REM call ..\Scripts\link.vbs StartFramework.lnk Environment\Launcher\MMILauncher.exe
REM CD ..\

ECHO  __  __  ___  ____ ___ __  __ 
ECHO ^|  \/  ^|/ _ \/ ___^|_ _^|  \/  ^|
ECHO ^| ^|\/^| ^| ^| ^| \___ \^| ^|^| ^|\/^| ^|
ECHO ^| ^|  ^| ^| ^|_^| ^|___) ^| ^|^| ^|  ^| ^|
ECHO ^|_^|  ^|_^|\___/^|____/___^|_^|  ^|_^|
ECHO.   

ECHO [92mSuccessfully deployed the Framework to %cd%/build/Environment.   [0m
ECHO If this is the first time, the framework was deployed, consider utilizing the script %cd%\build\enableFirewall.exe to setup all firewall exceptions. 
ECHO [92mTo start the framework[0m, start the launcher at %cd%\build\Environment\Launcher\MMILauncher.exe To use the framework, please open the Unity Demo-Scene at %cd%\Demos\Unity or any other MOSIM-enabled Project.

REM explorer.exe %cd%\build

pause

goto :eof

REM Method Section

:: argparse
:argparse
	if [%1]==[] (
		call :DisplayUsage
		exit /b 0
	)
	if "%1"=="-h" (
		call :DisplayUsage
		exit /b 0
	)
	if "%1"=="--help" ( 
		call :DisplayUsage
		exit /b 0
	)
	if "%1"=="\?" ( 
		call :DisplayUsage
		exit /b 0
	)
	
	if not exist %1 (
		call :FolderNotFound
		exit /b 1
	) else (
		FOR /F %%i IN ("%1") DO SET "MOSIM_HOME=%%~fi"
		
		echo Deploying to: %MOSIM_HOME%
		SET BUILDENV=%MOSIM_HOME%\Environment
		SET LIBRARYPATH=%MOSIM_HOME%\Libraries
		SET REPO=%~dp0\..\
		

		SHIFT
	)
	
	if "%1"=="-v" (
		ECHO Running in Verbose mode
		SET VERBOSE=1
		SHIFT
	)
	
	if [%1]==[] (
		call :DeployAll
		exit /b 0
	)

	:argparse_loop
	if not [%1]==[] (
		if "%1"=="-m" (
			if "%2"=="Adapter" (
				call :DeployAdapter 
			) else (
				if "%2"=="MMUs" (
					call :DeployMMUs 
				) else ( 
					if "%2"=="PathPlanning" ( 
						call :DeployPathPlanning
					)
				)
			)
			SHIFT
		) else ( 
			if "%1"=="-a" (
				call :DeployAll
			)
		)
		SHIFT
		goto :argparse_loop
	)
exit /b 0

::DisplayUsage
:DisplayUsage
	echo Usage
exit /b 0

::FolderNotFound
:FolderNotFound
	echo Folder Not Found
exit /b 0

::DeployAdapter
:DeployAdapter
	call :DeployMethod %REPO%\Adapter\MMIAdapterUnity Adapters\UnityAdapter\ build
exit /b 0

::DeployMMUs 
:DeployMMUs 
echo Deploy MMUs
	echo "Currently No Unity Generator MMUs tracked"
	REM call :DeployMethod %REPO%Core\BasicMMus\CS-Unity-MMUs MMUs\ build
exit /b 0
	
::DeployPathPlanning
:DeployPathPlanning
	call :DeployMethod %REPO%\Services\UnityPathPlanning Services\UnityPathPlanning UnityPathPlanningService\build
exit /b 0

::DeployAll
:DeployAll
	call :DeployAdapter
	call :DeployMMUs
	call :DeployPathPlanning
	REM TODO: Add Deployment of Tools and MMUs 
exit /b 0

:DeployUnity
    cd .\Core
    call .\distribute_unity.bat
    cd %MOSIM_HOME%
    if %ERRORLEVEL% NEQ 0 call :halt %ERRORLEVEL%

	
	REM Copy core artifacts to services:
    REM Copy MMIUnity artifacts to UnityPathPlanning
    cmd /c xcopy /S/Y/Q .\Core\Framework\EngineSupport\Unity\MMIUnity\build\* .\Services\UnityPathPlanning\UnityPathPlanningService\Assets\Plugins\
    if %ERRORLEVEL% NEQ 0 call :halt %ERRORLEVEL%

    REM Copy MMIUnityTarget engine to UnityDemo
    cmd /c xcopy /S/Y/Q .\Core\Framework\EngineSupport\Unity\MMIUnity.TargetEngine\MMIUnity.TargetEngine\build\* .\Demos\Unity\Assets\MMI\Plugins\
    if %ERRORLEVEL% NEQ 0 call :halt %ERRORLEVEL%

	REM Copy core artifacts to Tools
	REM Copy MMIUnity to SkeletonTesting
	cmd /c xcopy /S/Y/Q .\Core\Framework\EngineSupport\Unity\MMIUnity\build .\Tools\SkeletonTesting\Assets\Plugins\
    if %ERRORLEVEL% NEQ 0 call :halt %ERRORLEVEL%
	

	cmd /c xcopy /S/Y/Q .\Core\Framework\EngineSupport\Unity\MMIUnity.TargetEngine\MMIUnity.TargetEngine\build\* %LIBRARYPATH%
		
	cmd /c xcopy /S/Y/Q .\Core\Framework\EngineSupport\Unity\MMIUnity.TargetEngine\MMIUnity.TargetEngine\build\* .\Tools\SkeletonConfigurator\SkeletonConfigurator\Assets\MMI\Plugins\

exit /b


::DeployMethod 
::  %1 path to component
::  %2 target path
::  %3 build path in component
:DeployMethod
  REM Build Adapters
  set back=%CD%
  
  if exist %1 (
	  cd %1
	  call :safeCall .\deploy.bat "There has been an error when deploying %1" %back%
	  cd %back%
	  if not [%2]==[] (
		  md ".\%BUILDENV%\%2"
		  echo  "%1\%3\*" "%BUILDENV%\%2\"
		  cmd /c xcopy /S/Y/Q "%1\%3\*" "%BUILDENV%\%2\"
		  if %ERRORLEVEL% NEQ 0 echo There has been an error during copy. 
		  REM if %ERRORLEVEL% NEQ 0 cd %MOSIM_HOME% && call :halt %ERRORLEVEL%
	  )
  ) else (
    ECHO -----------
	ECHO [31m Path %1 does not exist and thus will not be deployed.[0m
	ECHO -----------
  )
exit /b

::MSBUILD
:MSBUILD
  for /F "delims=" %%i in (%1) do set dirname="%%~dpi"
  for /F "delims=" %%i in (%1) do set filename="%%~nxi"
  
  set mode=Debug
  SETLOCAL EnableDelayedExpansion 
  
  set back=%CD%
  
  if exist %dirname% (
	cd %dirname%
	
	if %VERBOSE%==1 (
		"%MSBUILD%" %filename% -t:Build -p:Configuration=%mode% -flp:logfile=build.log
	) else (
		>deploy.log (
			"%MSBUILD%" %filename% -t:Build -p:Configuration=%mode% -flp:logfile=build.log
		)
	)
	REM If the build was sucessfull, copy all files to the respective build folders. 

	if !ERRORLEVEL! EQU 0 (
		if not [%2]==[] (
			if %VERBOSE%==1 (
				ECHO copy from ".\%2\%mode%\*" to "%BUILDENV%\%3\"
				cmd /c xcopy /S/Y/Q ".\%2\%mode%\*" "%BUILDENV%\%3\"
			) else (
				>>deploy.log (
					cmd /c xcopy /S/Y/Q ".\%2\%mode%\*" "%BUILDENV%\%3\"
				)
			)
		)
		if not [%4]==[] (
			if %VERBOSE%==1 (
				cmd /c xcopy /S/Y/Q ".\%4\%mode%\*" "%BUILDENV%\%5\"
			) else (
				>>deploy.log (
					cmd /c xcopy /S/Y/Q ".\%4\%mode%\*" "%BUILDENV%\%5\"
				)
			)
		)
		if not [%6]==[] (
			if %VERBOSE%==1 (
				cmd /c xcopy /S/Y/Q ".\%6\%mode%\*" "%BUILDENV%\%7\"
			) else (
				>>deploy.log (
					cmd /c xcopy /S/Y/Q ".\%6\%mode%\*" "%BUILDENV%\%7\"
				)
			)
		)
		ECHO [92mSuccessfully deployed %filename%. [0m
	) else (
		type deploy.log 
		ECHO [31mDeployment of %filename% failed. Please consider the build.log for more information.[0m 
		cd %back%
		call :halt %ERRORLEVEL%
	)
  ) else (
    ECHO -----------
	ECHO [31m Path %1 does not exist and thus will not be deployed.[0m
	ECHO -----------
  )
cd %back%
exit /b

:: Calls a method %1 and checks the error level. If %1 failed, text %2 will be reported. 
:safeCall
SET back=%3
call %1
if %ERRORLEVEL% NEQ 0 (
  ECHO [31m %~2 [0m
  cd %back%
  call :halt %ERRORLEVEL%
) else (
	exit /b
)

:: Sets the errorlevel and stops the batch immediately
:halt
call :__SetErrorLevel %1
call :__ErrorExit 2> nul
goto :eof

:__ErrorExit
rem Creates a syntax error, stops immediately
() 
goto :eof

:__SetErrorLevel
exit /b %time:~-2%
goto :eof

REM ErrorExit should not be called, just goto'ed. It assumes, that the ERRORLEVEL variable was set before to the appropriate value. 
REM exit /b %ERRORLEVEL%
REM Nothing should folow after this. 