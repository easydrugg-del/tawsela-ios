# تشغيل بناء iOS على Codemagic من سطر الأوامر.
#
# الاستخدام:
#   .\codemagic-build.ps1 -Token "<API_TOKEN>"
#   .\codemagic-build.ps1 -Token "<API_TOKEN>" -Workflow ios-simulator
#
# التوكن من: Codemagic → Teams → Personal account → Integrations → Codemagic API

param(
    [Parameter(Mandatory = $true)][string]$Token,
    [string]$Workflow = 'ios-device-ipa',
    [string]$Branch = 'main',
    [string]$Repo = 'https://github.com/easydrugg-del/tawsela-ios',
    [string]$OutDir = "$env:TEMP\codemagic-artifacts"
)

$ErrorActionPreference = 'Stop'
$api = 'https://api.codemagic.io'
$headers = @{ 'x-auth-token' = $Token; 'Content-Type' = 'application/json' }

function Get-AppId {
    $apps = Invoke-RestMethod -Uri "$api/apps" -Headers $headers -Method Get
    $existing = $apps.applications | Where-Object { $_.appName -eq 'tawsela-ios' } | Select-Object -First 1
    if ($existing) {
        Write-Host ("✓ التطبيق موجود: " + $existing._id) -ForegroundColor Green
        return $existing._id
    }

    Write-Host "→ إضافة التطبيق لأول مرة…" -ForegroundColor Cyan
    $created = Invoke-RestMethod -Uri "$api/apps" -Headers $headers -Method Post `
        -Body (@{ repositoryUrl = $Repo } | ConvertTo-Json)
    $id = if ($created._id) { $created._id } else { $created.application._id }
    Write-Host ("✓ تمت الإضافة: " + $id) -ForegroundColor Green
    return $id
}

$appId = Get-AppId

Write-Host "→ بدء البناء ($Workflow على $Branch)…" -ForegroundColor Cyan
$started = Invoke-RestMethod -Uri "$api/builds" -Headers $headers -Method Post `
    -Body (@{ appId = $appId; workflowId = $Workflow; branch = $Branch } | ConvertTo-Json)
$buildId = $started.buildId
Write-Host ("✓ Build ID: " + $buildId) -ForegroundColor Green
Write-Host ("  https://codemagic.io/app/" + $appId + "/build/" + $buildId) -ForegroundColor DarkGray

# متابعة الحالة
$last = ''
while ($true) {
    Start-Sleep -Seconds 20
    $b = (Invoke-RestMethod -Uri "$api/builds/$buildId" -Headers $headers -Method Get).build
    if ($b.status -ne $last) {
        Write-Host ("  الحالة: " + $b.status)
        $last = $b.status
    }
    if ($b.status -in @('finished', 'failed', 'canceled', 'timeout')) { break }
}

if ($b.status -ne 'finished') {
    Write-Host ("✗ البناء انتهى بحالة: " + $b.status) -ForegroundColor Red
    exit 1
}

Write-Host "`n✓ البناء نجح. المخرجات:" -ForegroundColor Green
if (-not (Test-Path $OutDir)) { New-Item -ItemType Directory -Path $OutDir | Out-Null }

foreach ($a in $b.artifacts) {
    Write-Host ("  - " + $a.name)
    $dest = Join-Path $OutDir $a.name
    try {
        Invoke-WebRequest -Uri $a.url -Headers @{ 'x-auth-token' = $Token } -OutFile $dest -UseBasicParsing
        $mb = [math]::Round((Get-Item $dest).Length / 1MB, 2)
        Write-Host ("    نُزّل → $dest  ($mb MB)") -ForegroundColor Green
    }
    catch {
        Write-Host ("    تعذّر التنزيل: " + $_.Exception.Message) -ForegroundColor Yellow
        Write-Host ("    الرابط: " + $a.url) -ForegroundColor DarkGray
    }
}
