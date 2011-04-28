REM Enviroment Setup
@SET path=%path%;%WinDir%\Microsoft.NET\Framework\v2.0.50727\
@tools\nant\NAnt.exe -buildfile:OscarLib.build %*
