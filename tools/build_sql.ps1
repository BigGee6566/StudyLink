<#
  Builds database\StudyLink.sql: one script that creates the StudyLink database with all its tables,
  indexes and constraints (generated from the EF6 migrations) and the lookup data every installation
  needs (read from Data\Seed\LookupData.cs, the same list the site seeds on start-up).

  Re-run after adding a migration or changing LookupData:   powershell -File tools\build_sql.ps1
  Needs SQL Server reachable as .\SQLEXPRESS (ef6 asks it for the provider version; nothing is created).
#>
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root 'Data\StudyLink.Data.csproj'
$output = Join-Path $root 'database\StudyLink.sql'

$msbuild = & "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" -latest -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
& $msbuild $project /p:Configuration=Debug /v:quiet /nologo
if ($LASTEXITCODE -ne 0) { throw "Building StudyLink.Data failed." }
$dll = (& $msbuild $project -getProperty:TargetPath /p:Configuration=Debug /nologo).Trim()

# Pointing ef6 at a database that does not exist makes it script every migration from the start.
$ef = Join-Path $env:USERPROFILE '.nuget\packages\entityframework\6.5.1\tools\net45\any\ef6.exe'
$scratch = 'StudyLink_ScriptOnly_' + [guid]::NewGuid().ToString('N')
$raw = [IO.Path]::GetTempFileName()
& $ef database update --script --assembly $dll --project-dir (Join-Path $root 'Data') --root-namespace StudyLink.Data `
    --connection-string "Data Source=.\SQLEXPRESS;Initial Catalog=$scratch;Integrated Security=True" `
    --connection-provider System.Data.SqlClient *> $raw
if ($LASTEXITCODE -ne 0) { Get-Content $raw; throw "ef6 could not script the migrations." }
$schema = (Get-Content $raw | Where-Object { $_ -notmatch '^(Applying |Running Seed)' }) -join "`r`n"
Remove-Item $raw

$lookup = [Reflection.Assembly]::LoadFrom($dll).GetType('StudyLink.Data.Seed.LookupData')
function Field($name) { $lookup.GetField($name).GetValue($null) }
function Q($text) { "N'" + ([string]$text).Replace("'", "''") + "'" }
function Rows($items, [scriptblock]$columns) { ($items | ForEach-Object { '    (' + ((& $columns $_) -join ', ') + ')' }) -join ",`r`n" }
function Insert($table, $columns, $select, $key, $rows) {
    "INSERT dbo.$table ($columns)`r`nSELECT $select FROM (VALUES`r`n$rows`r`n) v($(($columns -split ', ' | Where-Object { $_ -notin 'IsActive', 'UpdatedAt' }) -join ', '))`r`n" +
    "WHERE NOT EXISTS (SELECT 1 FROM dbo.$table t WHERE t.$key = v.$key);`r`n"
}

$seed = @(
    Insert 'Campuses' 'Name, IsActive' 'v.Name, 1' 'Name' (Rows (Field 'Campuses') { Q $args[0] })
    Insert 'Roles' 'Name, Description' 'v.Name, v.Description' 'Name' (Rows (Field 'Roles') { (Q $args[0].Item1), (Q $args[0].Item2) })
    Insert 'StudyLevels' 'Name, SortOrder' 'v.Name, v.SortOrder' 'Name' (Rows (Field 'StudyLevels') { (Q $args[0].Item1), $args[0].Item2 })
    Insert 'Subjects' 'Name, IsActive' 'v.Name, 1' 'Name' (Rows (Field 'Subjects') { Q $args[0] })
    Insert 'Categories' 'Name, Description, IsActive' 'v.Name, v.Description, 1' 'Name' (Rows (Field 'Categories') { (Q $args[0].Item1), (Q $args[0].Item2) })
    Insert 'SystemSettings' 'SettingKey, Value, Description, UpdatedAt' 'v.SettingKey, v.Value, v.Description, SYSDATETIME()' 'SettingKey' (Rows (Field 'Settings') { (Q $args[0].Item1), (Q $args[0].Item2), (Q $args[0].Item3) })
) -join "`r`n"

$migrations = (Get-ChildItem (Join-Path $root 'Data\Migrations') -Filter '*_*.cs' | Where-Object { $_.Name -notmatch 'Designer' } |
    ForEach-Object { $_.BaseName }) -join ', '

$sql = @"
/* =============================================================================
   StudyLink.sql - creates the StudyLink database, ready for the web site
   Books, Tutoring & Academic Resource Hub  |  Nova Tech Developers
   Target: Microsoft SQL Server 2016 or later (Express is fine)

   HOW TO USE
   1. Open this file in SQL Server Management Studio, connect to your server
      and press Execute (F5). Or from a command prompt:
          sqlcmd -S .\SQLEXPRESS -E -i database\StudyLink.sql
   2. Point the site at the server in Web.config, <connectionStrings>:
          <add name="StudyLink" connectionString="Data Source=YOUR-SERVER;Initial Catalog=StudyLink;
               Integrated Security=True;MultipleActiveResultSets=True;Application Name=StudyLink"
               providerName="System.Data.SqlClient" />
      For a SQL login instead of Windows authentication, replace
      Integrated Security=True with User ID=...;Password=...
   3. Start the site. With no accounts yet, the first visit opens Account/Setup to
      create the first System Admin (or set Seed:Demo=true in
      App_Data\local.settings.config for the demo accounts and sample data).

   Safe to run again: if the tables already exist the schema part is skipped, and
   lookup rows are only inserted when missing (an admin's changes are kept).
   The site recognises the database through __MigrationHistory: it will not try
   to recreate it, and it applies any newer migrations itself on start-up.

   GENERATED by tools\build_sql.ps1 from the EF6 migrations ($migrations)
   and Data\Seed\LookupData.cs. Do not hand-edit; change the model or the lookup
   list and regenerate. Enum columns are INT; values are in Data\Entities\Enums.cs
   (e.g. Bookings.Status 1 = AwaitingPayment, 3 = Confirmed, 4 = Completed).
   ============================================================================= */
SET NOCOUNT ON;
-- Filtered indexes need QUOTED_IDENTIFIER ON; sqlcmd defaults it to OFF.
SET QUOTED_IDENTIFIER ON;
GO
IF DB_ID(N'StudyLink') IS NULL
    EXEC (N'CREATE DATABASE StudyLink;');
GO
-- SQL Express creates databases with AUTO_CLOSE ON, which makes every new connection slow.
ALTER DATABASE StudyLink SET AUTO_CLOSE OFF;
GO
USE StudyLink;
GO
IF OBJECT_ID(N'dbo.__MigrationHistory') IS NOT NULL
BEGIN
    PRINT 'StudyLink tables already exist: skipping the schema, topping up lookup data only.';
    SET NOEXEC ON;
END
GO
/* ---------------------------------------------------------------- Schema */
$schema
GO
SET NOEXEC OFF;
GO
/* ---------------------------------------------------------- Lookup data */
$seed
PRINT 'StudyLink database is ready.';
GO
"@
[IO.File]::WriteAllText($output, $sql, (New-Object Text.UTF8Encoding($false)))
"Wrote $output"
