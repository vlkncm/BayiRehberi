$ErrorActionPreference = 'Stop'
$project = Split-Path -Parent $MyInvocation.MyCommand.Path
$output = Join-Path $project 'dist'
New-Item -ItemType Directory -Force -Path $output | Out-Null
$compiler = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (!(Test-Path $compiler)) { $compiler = 'C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe' }
$compilerArgs = @(
  '/nologo', '/target:winexe', '/optimize+', '/platform:anycpu',
  "/win32icon:$project\BayiRehberi.ico",
  "/resource:$project\BayiRehberi.ico,BayiRehberi.ico",
  "/resource:$project\saatDunyasiLogo.jpg,saatDunyasiLogo.jpg",
  "/out:$output\BayiRehberi.exe",
  '/reference:System.dll', '/reference:System.Core.dll', '/reference:System.Drawing.dll', '/reference:System.Windows.Forms.dll',
  "$project\Program.cs", "$project\SeedData.cs"
)
& $compiler $compilerArgs
if ($LASTEXITCODE -ne 0) { throw 'Derleme başarısız oldu.' }
Write-Host "Hazır: $output\BayiRehberi.exe"
