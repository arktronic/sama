@echo off
cd /d %~dp0
dotnet test --settings system.runsettings
