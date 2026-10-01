<#
  Precompiles every page, master page, user control and handler so markup errors show up before a
  browser hits them. MSBuild never compiles .aspx markup.

  The site is the repository root, and legacy\TutorConnect carries its own application-level
  Web.config, which aspnet_compiler rejects. So the site alone is mirrored to %TEMP% first.
  Build the solution before running this (the copy needs bin\).

    powershell -File tools\precompile.ps1
#>
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$site = Join-Path $env:TEMP 'slsite'
$out = Join-Path $env:TEMP ('slpc' + (Get-Date -Format 'HHmmss'))

robocopy $root $site /MIR /NFL /NDL /NJH /NJS /NP /XD .git .vs .claude obj legacy Data Services Tests TestResults docs database tools branding packages | Out-Null
if ($LASTEXITCODE -ge 8) { throw "robocopy failed with exit code $LASTEXITCODE" }

& "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\aspnet_compiler.exe" -v / -p $site $out
if ($LASTEXITCODE -ne 0) { throw "aspnet_compiler failed with exit code $LASTEXITCODE" }
"PRECOMPILE OK ($out)"
