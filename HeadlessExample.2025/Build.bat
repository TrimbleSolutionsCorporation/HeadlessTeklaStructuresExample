@echo off 
echo Restoring NuGet packages... 
dotnet restore HeadlessExample.sln 
if errorlevel 1 goto :fail 
 
echo Building... 
dotnet build HeadlessExample.sln -c Release --no-restore 
if errorlevel 1 goto :fail 
 
echo Build succeeded. 
goto :end 
:fail 
echo Build FAILED. 
:end 
pause
