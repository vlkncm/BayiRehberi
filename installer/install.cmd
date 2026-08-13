@echo off
set "APPDIR=%LOCALAPPDATA%\Programs\BayiRehberi"
if not exist "%APPDIR%" mkdir "%APPDIR%"
copy /Y "BayiRehberi.exe" "%APPDIR%\BayiRehberi.exe" >nul
powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "$w=New-Object -ComObject WScript.Shell;$s=$w.CreateShortcut([IO.Path]::Combine([Environment]::GetFolderPath('Desktop'),'Bayi Rehberi.lnk'));$s.TargetPath=[IO.Path]::Combine($env:LOCALAPPDATA,'Programs\BayiRehberi\BayiRehberi.exe');$s.WorkingDirectory=[IO.Path]::GetDirectoryName($s.TargetPath);$s.Save()"
start "" "%APPDIR%\BayiRehberi.exe"
exit /b 0
