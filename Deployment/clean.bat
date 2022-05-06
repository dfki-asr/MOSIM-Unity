@echo off 

ECHO " ------------------------------------------------- "
ECHO "       _   __ ___                                  " 
ECHO " |\/| / \ (_   |  |\/|     __     | | ._  o _|_    "
ECHO " |  | \_/ __) _|_ |  |            |_| | | |  |_ \/ "
ECHO "                                                /  "
ECHO " ------------------------------------------------- "
ECHO.

ECHO Cleaning Repository

SET VERBOSE=0

call :CheckEnv

call :argparse %*

goto :eof

:: argparse
:argparse
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
	
	SET REPO=%~dp0..\
		

	if "%1"=="-v" (
		ECHO Running in Verbose mode
		SET VERBOSE=1
		SHIFT
	)
	
	if [%1]==[] (
		call :CleanAll
		ECHO.
		ECHO Sucessfully cleaned repository 
		ECHO.
		pause
		exit /b 0
	)
exit /b 0

:CleanAll
	call :CleanUnity %REPO%\Adapter\MMIAdapterUnity
	call :CleanUnity %REPO%\Demos\SimpleUnityDemo
	call :CleanUnity %REPO%\Services\UnityPathPlanning\UnityPathPlanningService
	call :CleanMMUs
exit /b 0
	
	
:CleanMMUs
	call :CleanVS "%REPO%\MMUs\UnityMMUs.sln"
	RD /S/Q %REPO%\MMUs\build
	RD /S/Q %REPO%\MMUs\Idle\build
	RD /S/Q %REPO%\MMUs\Idle\obj
	RD /S/Q %REPO%\MMUs\Locomotion\build
	RD /S/Q %REPO%\MMUs\Locomotion\obj
exit /b 0

:CleanUnity
	RD /S/Q %1\build
	RD /S/Q %1\Library
exit /b 0

:CleanVS
  for /F "delims=" %%i in (%1) do set dirname="%%~dpi"
  for /F "delims=" %%i in (%1) do set filename="%%~nxi"
  
  set mode=Debug
  SETLOCAL EnableDelayedExpansion 
  
  set back=%CD%
  
  echo %dirname% %filename%
  
  if exist %dirname% (
	cd %dirname%
	
	if %VERBOSE%==1 (
		"%MOSIM_MSBUILD%" %filename% -t:Clean
	) else (
		>deploy.log (
			"%MOSIM_MSBUILD%" %filename% -t:Clean
		)
	)
  ) else (
    ECHO -----------
	ECHO [31m Path %1 does not exist and thus will not be cleaned.[0m
	ECHO -----------
  )
  cd %back%
exit /b 0

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

::DisplayUsage
:DisplayUsage
	echo Usage
exit /b 0

::FolderNotFound
:FolderNotFound
	echo Folder Not Found
exit /b 0



::Check Environment Variables
:CheckEnv
	IF NOT "%MOSIM_MSBUILD%"=="" (
		IF NOT EXIST "%MOSIM_MSBUILD%" (
			ECHO Please update your environment variable MOSIM_MSBUILD to point to Visual Studio MSBUILD.
			ECHO example: setx MOSIM_MSBUILD "C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\MSBuild.exe"
			call :halt 1
		)
	) ELSE (
		ECHO Compilation requires Visual Studio. Please setup the variable MOSIM_MSBUILD to point to Visual Studio MSBUILD.
		ECHO example: setx MOSIM_MSBUILD "C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\MSBuild.exe"
		call :halt 1
	)
	IF NOT "%MOSIM_UNITY%"=="" (
		IF NOT EXIST "%MOSIM_UNITY%" (
			ECHO Please update your environment variable MOSIM_UNITY to point to your Unity 2019 executable.
			ECHO example: setx MOSIM_UNITY "C:\Program Files\Unity\Hub\Editor\2019.4.25f1\Editor\Unity.exe"
			call :halt 1
		)
	) ELSE (
		ECHO Compilation requires Unity 2019. Please setup the variable MOSIM_UNITY to point to your Unity 2019 executable.
		ECHO example: setx MOSIM_UNITY "C:\Program Files\Unity\Hub\Editor\2019.4.25f1\Editor\Unity.exe"
		call :halt 1
	)

exit /b 0


:CheckPowershell
SET "PSCMD=$ppid=$pid;while($i++ -lt 3 -and ($ppid=(Get-CimInstance Win32_Process -Filter ('ProcessID='+$ppid)).ParentProcessId)) {}; (Get-Process -EA Ignore -ID $ppid).Name"

for /f "tokens=*" %%i in ('powershell -noprofile -command "%PSCMD%"') do SET %1=%%i

IF ["%PARENT%"] == ["powershell"] (
	ECHO This script should not run from within a Powershell but a Command Prompt aka cmd
	call :halt 1
) ELSE (
    exit /b 1
)



:: Sets the errorlevel and stops the batch immediately
:halt
call :__SetErrorLevel %1
call :__ErrorExit 2> nul
goto :eof

:__ErrorExit
rem Creates a syntax error, stops immediately
pause
() 
goto :eof

:__SetErrorLevel
exit /b %time:~-2%
goto :eof

REM ErrorExit should not be called, just goto'ed. It assumes, that the ERRORLEVEL variable was set before to the appropriate value. 
REM exit /b %ERRORLEVEL%
REM Nothing should folow after this. 