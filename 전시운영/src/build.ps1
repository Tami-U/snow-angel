# 전시 시작.exe / 전시 종료.exe 다시 만들기 (Windows 기본 .NET Framework 4 C# 컴파일러 사용)
# 실행: 이 파일을 오른쪽 클릭 → "PowerShell에서 실행"
$ErrorActionPreference = 'Stop'
$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path $csc)) { $csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe' }
Set-Location $PSScriptRoot
$out = Split-Path -Parent $PSScriptRoot

& $csc /nologo /codepage:65001 /target:winexe /optimize+ /r:System.Management.dll /r:System.Windows.Forms.dll "/out:$out\전시 시작.exe" StartExhibit.cs Common.cs
if ($LASTEXITCODE -ne 0) { throw '전시 시작.exe 빌드 실패' }
& $csc /nologo /codepage:65001 /target:winexe /optimize+ /r:System.Management.dll "/out:$out\전시 종료.exe" StopExhibit.cs Common.cs
if ($LASTEXITCODE -ne 0) { throw '전시 종료.exe 빌드 실패' }
Write-Host 'Build OK'
