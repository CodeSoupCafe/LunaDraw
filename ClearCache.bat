rem^ ||(
    This is the default license template.
    
    File: ClearCache.bat
    Author: iknow
    Copyright (c) 2025 iknow
    
    To edit this license information: Press Ctrl+Shift+P and press 'Create new License Template...'.
)

dotnet nuget locals all --clear
dotnet clean
for /d /r . %%d in (bin,obj) do @if exist "%%d" rd /s/q "%%d"