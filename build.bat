@ECHO OFF
@REM Note you need to set up the path to nant on your machine.
nant
IF %ERRORLEVEL% NEQ 0 PAUSE
