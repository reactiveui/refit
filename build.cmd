@echo off
tools\nuget\nuget.exe update -self
tools\nuget\nuget.exe install xunit.runner.console -OutputDirectory tools -ExcludeVersion
tools\nuget\nuget.exe install Cake -OutputDirectory tools -ExcludeVersion

tools\Cake\Cake.exe build.cake

exit /b %errorlevel%
