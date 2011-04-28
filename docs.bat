REM Enviroment Setup
IF EXIST "C:\Program Files\Microsoft Visual Studio 8\Common7\Tools\vsvars32.bat" GOTO VSEnv
IF EXIST "C:\Program Files\Microsoft.NET\SDK\v2.0\Bin\sdkvars.bat" GOTO SDK1
IF EXIST "C:\Program Files\Microsoft Visual Studio 8\SDK\v2.0\Bin\sdkvars.bat" GOTO SDK2
GOTO Error
:VSEnv
ECHO "Visual Studio"
CALL "C:\Program Files\Microsoft Visual Studio 8\Common7\Tools\vsvars32.bat"
GOTO RunNant 
:SDK1
ECHO "SDK"
CALL "C:\Program Files\Microsoft.NET\SDK\v2.0\Bin\sdkvars.bat"
GOTO RunNant 
:SDK2
ECHO "SDK"
CALL "C:\Program Files\Microsoft Visual Studio 8\SDK\v2.0\Bin\sdkvars.bat"
GOTO RunNant 
:Error
ECHO "Could not find enviroment batch file, attempting to continue . . ."
:RunNant
REM Yeah the thing we are really trying to run
@tools\nant\NAnt.exe -buildfile:OscarLib.build docs %*
