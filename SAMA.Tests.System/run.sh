#!/bin/bash
cd $(dirname "$0")
dotnet test --settings system.runsettings
