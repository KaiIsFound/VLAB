[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('Close', 'Verify')]
    [string]$Action,

    [Parameter(Mandatory = $true)]
    [string]$ProjectRoot,

    [Parameter(Mandatory = $true)]
    [string]$ArtifactRoot,

    [Parameter(Mandatory = $true)]
    [string]$CheckpointRoot,

    [Parameter(Mandatory = $true)]
    [string]$CheckpointId
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$utf8 = [Text.UTF8Encoding]::new($false)

function Resolve-PathFrom([string]$Path, [string]$Base) {
    if ([IO.Path]::IsPathRooted($Path)) { return [IO.Path]::GetFullPath($Path) }
    return [IO.Path]::GetFullPath((Join-Path $Base $Path))
}

function Assert-True([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
}

function Get-Sha([string]$Path) {
    return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
}

function Get-TextSha([string]$Text) {
    $algorithm = [Security.Cryptography.SHA256]::Create()
    try {
        return ([BitConverter]::ToString($algorithm.ComputeHash($utf8.GetBytes($Text)))).Replace('-', '').ToLowerInvariant()
    }
    finally { $algorithm.Dispose() }
}

function Get-Relative([string]$Base, [string]$Target) {
    $baseUri = [Uri]::new(([IO.Path]::GetFullPath($Base).TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar))
    $targetUri = [Uri]::new([IO.Path]::GetFullPath($Target))
    return [Uri]::UnescapeDataString($baseUri.MakeRelativeUri($targetUri).ToString()).Replace('\', '/')
}

function New-SourceInventory([string]$Root) {
    $files = foreach ($relativeRoot in @('Assets', 'Packages', 'ProjectSettings')) {
        Get-ChildItem -LiteralPath (Join-Path $Root $relativeRoot) -Recurse -Force -File | ForEach-Object {
            [ordered]@{
                relativePath = Get-Relative $Root $_.FullName
                sizeBytes = $_.Length
                sha256 = Get-Sha $_.FullName
            }
        }
    }
    $files = @($files | Sort-Object { $_.relativePath })
    $canonical = ($files | ForEach-Object { '{0}|{1}|{2}' -f $_.relativePath, $_.sizeBytes, $_.sha256 }) -join "`n"
    if ($files.Count -gt 0) { $canonical += "`n" }
    return [ordered]@{
        schema = 'vlab.sha256-inventory.v1'
        fileCount = $files.Count
        aggregateSha256 = Get-TextSha $canonical
        files = $files
    }
}

function Assert-Inventory([object]$Inventory, [string]$Root) {
    $lines = [Collections.Generic.List[string]]::new()
    foreach ($entry in $Inventory.files) {
        $path = Join-Path $Root ([string]$entry.relativePath)
        Assert-True (Test-Path -LiteralPath $path -PathType Leaf) "Source file missing: $($entry.relativePath)"
        $item = Get-Item -LiteralPath $path
        Assert-True ($item.Length -eq [long]$entry.sizeBytes) "Source size changed: $($entry.relativePath)"
        Assert-True ((Get-Sha $path) -eq [string]$entry.sha256) "Source hash changed: $($entry.relativePath)"
        $lines.Add(('{0}|{1}|{2}' -f $entry.relativePath, $entry.sizeBytes, $entry.sha256))
    }
    $canonical = $lines -join "`n"
    if ($lines.Count -gt 0) { $canonical += "`n" }
    Assert-True ((Get-TextSha $canonical) -eq [string]$Inventory.aggregateSha256) 'Source inventory aggregate changed.'
}

function Write-Json([object]$Value, [string]$Path) {
    [IO.File]::WriteAllText($Path, (($Value | ConvertTo-Json -Depth 30) + "`n"), $utf8)
}

function Assert-TestXml([string]$Path, [int]$Minimum) {
    [xml]$xml = Get-Content -LiteralPath $Path -Raw
    $run = $xml.'test-run'
    Assert-True ([int]$run.total -ge $Minimum) "Test discovery below $Minimum in $Path"
    Assert-True ([int]$run.failed -eq 0) "Test failures reported in $Path"
    Assert-True ([int]$run.passed -eq [int]$run.total) "Not every discovered test passed in $Path"
    return [ordered]@{ total = [int]$run.total; passed = [int]$run.passed; failed = [int]$run.failed }
}

$project = Resolve-PathFrom $ProjectRoot (Get-Location).Path
$artifact = Resolve-PathFrom $ArtifactRoot $project
$checkpoint = Resolve-PathFrom $CheckpointRoot $project
$plan = Join-Path $project 'plans/VLAB-PCVR-OPENXR-DESKTOP-BLUEPRINT.md'
$gatePath = Join-Path $artifact 'gate-manifest.json'
$inventoryPath = Join-Path $artifact 'source-inventory.json'

Assert-True (Test-Path -LiteralPath $project -PathType Container) 'Project root is missing.'
Assert-True (Test-Path -LiteralPath $checkpoint -PathType Container) 'Checkpoint root is missing.'
Assert-True ((Split-Path $checkpoint -Leaf) -ceq $CheckpointId) 'Checkpoint directory does not match CheckpointId.'

$evidencePaths = [ordered]@{
    evidenceBootstrap = 'Artifacts/VLABUpgrade/plan-1.4/P1.evidence-bootstrap/20260901T120000Z-bootstrap/gate-manifest.json'
    p1d1EditMode = 'Artifacts/VLABUpgrade/plan-1.4/P1.d1/20260901T131500Z-editmode-retry/editmode-results.xml'
    p1d1PlayMode = 'Artifacts/VLABUpgrade/plan-1.4/P1.d1/20260901T133000Z-playmode/playmode-results.xml'
    p1d2EditMode = 'Artifacts/VLABUpgrade/plan-1.4/P1.d2/20260901T151500Z-postbuild-editmode/test-results.xml'
    p1d2PlayMode = 'Artifacts/VLABUpgrade/plan-1.4/P1.d2/20260901T153000Z-postbuild-playmode/test-results.xml'
    p1d3EditMode = 'Artifacts/VLABUpgrade/plan-1.4/P1.d3/20260901T181500Z-final-editmode/test-results.xml'
    p1d3PlayMode = 'Artifacts/VLABUpgrade/plan-1.4/P1.d3/20260901T170000Z-playmode-guard-fix/playmode-results.xml'
    releaseBuild = 'Artifacts/VLABUpgrade/plan-1.4/P1.d3/20260901T173000Z-release-guard-build/windows-build-evidence.json'
    releaseBuildLog = 'Artifacts/VLABUpgrade/plan-1.4/P1.d3/20260901T173000Z-release-guard-build.unity.log'
    developmentBuild = 'Artifacts/VLABUpgrade/plan-1.4/P1.gate/20260901T180000Z-windows-development-smoke/windows-build-evidence.json'
    playerSmoke = 'Artifacts/VLABUpgrade/plan-1.4/P1.gate/20260901T180000Z-windows-development-smoke/player-smoke.json'
    playerLog = 'Artifacts/VLABUpgrade/plan-1.4/P1.gate/20260901T180000Z-windows-development-smoke/player.log'
    openXrConfiguration = 'Artifacts/VLABUpgrade/plan-1.3/P1.b/20260831T155433Z-openxr-tests-retry/openxr-configuration.json'
}

if ($Action -eq 'Close') {
    Assert-True (-not (Test-Path -LiteralPath $gatePath)) 'Refusing to overwrite a closed P1 gate.'
    [IO.Directory]::CreateDirectory($artifact) | Out-Null
    foreach ($entry in $evidencePaths.GetEnumerator()) {
        Assert-True (Test-Path -LiteralPath (Join-Path $project $entry.Value) -PathType Leaf) "Evidence missing: $($entry.Value)"
    }

    $tests = [ordered]@{
        p1d1EditMode = Assert-TestXml (Join-Path $project $evidencePaths.p1d1EditMode) 46
        p1d1PlayMode = Assert-TestXml (Join-Path $project $evidencePaths.p1d1PlayMode) 2
        p1d2EditMode = Assert-TestXml (Join-Path $project $evidencePaths.p1d2EditMode) 46
        p1d2PlayMode = Assert-TestXml (Join-Path $project $evidencePaths.p1d2PlayMode) 2
        p1d3EditMode = Assert-TestXml (Join-Path $project $evidencePaths.p1d3EditMode) 46
        p1d3PlayMode = Assert-TestXml (Join-Path $project $evidencePaths.p1d3PlayMode) 3
    }

    $release = Get-Content -LiteralPath (Join-Path $project $evidencePaths.releaseBuild) -Raw | ConvertFrom-Json
    $development = Get-Content -LiteralPath (Join-Path $project $evidencePaths.developmentBuild) -Raw | ConvertFrom-Json
    $smoke = Get-Content -LiteralPath (Join-Path $project $evidencePaths.playerSmoke) -Raw | ConvertFrom-Json
    $openXr = Get-Content -LiteralPath (Join-Path $project $evidencePaths.openXrConfiguration) -Raw | ConvertFrom-Json
    Assert-True ($release.result -eq 'Succeeded' -and [int]$release.totalErrors -eq 0) 'Release build did not succeed cleanly.'
    Assert-True ((Get-Content -LiteralPath (Join-Path $project $evidencePaths.releaseBuildLog) -Raw) -match 'Release guard stripped 1 simulator root') 'Release simulator exclusion was not observed.'
    Assert-True ($development.result -eq 'Succeeded' -and [int]$development.playerExitCode -eq 0) 'Development player build/smoke failed.'
    Assert-True ($smoke.result -eq 'Pass' -and $smoke.mode -eq 'Desktop' -and $smoke.failureReason -ne 'None') 'No-runtime Desktop fallback smoke failed.'
    Assert-True ([int]$smoke.activeCameraCount -eq 1 -and [int]$smoke.activeAudioListenerCount -eq 1 -and [int]$smoke.mainCameraTagCount -eq 1) 'Player camera/listener/tag invariant failed.'
    Assert-True ([int]$openXr.mandatoryErrorCount -eq 0) 'OpenXR Project Validation contains mandatory errors.'

    $graphics = Get-Content -LiteralPath (Join-Path $project 'ProjectSettings/GraphicsSettings.asset') -Raw
    $quality = Get-Content -LiteralPath (Join-Path $project 'ProjectSettings/QualitySettings.asset') -Raw
    $playerSettings = Get-Content -LiteralPath (Join-Path $project 'ProjectSettings/ProjectSettings.asset') -Raw
    $scene = Join-Path $project 'Assets/ChemistryLab.unity'
    Assert-True ($graphics -match '(?m)^\s*m_CustomRenderPipeline:\s*\{fileID:\s*0\}\s*$') 'Built-in Graphics sentinel failed.'
    $qualityPipelines = [regex]::Matches($quality, '(?m)^\s*customRenderPipeline:\s*\{fileID:\s*(\d+)')
    Assert-True ($qualityPipelines.Count -gt 0) 'Quality renderer matrix is empty.'
    foreach ($pipeline in $qualityPipelines) {
        Assert-True ($pipeline.Groups[1].Value -eq '0') 'A Quality level activates an SRP.'
    }
    Assert-True ($playerSettings -match '(?m)^\s*activeInputHandler:\s*2\s*$') 'Input Handling changed from Both.'
    Assert-True ((Get-Content -LiteralPath $scene -Raw) -match '__ChemistryLab_Generated_v9') 'Generated scene marker is not v9.'

    $inventory = New-SourceInventory $project
    Write-Json $inventory $inventoryPath
    $evidence = foreach ($entry in $evidencePaths.GetEnumerator()) {
        [ordered]@{ id = $entry.Key; path = $entry.Value; sha256 = Get-Sha (Join-Path $project $entry.Value); status = 'PASS' }
    }
    $gate = [ordered]@{
        schema = 'vlab.phase-gate.v2'
        planVersion = '1.4'
        planSha256 = Get-Sha $plan
        phase = 'P1.gate'
        checkpoint = [ordered]@{ id = $CheckpointId; root = $checkpoint.Replace('\', '/'); sceneSha256 = Get-Sha (Join-Path $checkpoint 'Assets/ChemistryLab.unity') }
        source = [ordered]@{ fileCount = $inventory.fileCount; aggregateSha256 = $inventory.aggregateSha256; sceneSha256 = Get-Sha $scene }
        renderer = 'PASS — Built-in Render Pipeline remains effective in Graphics and every Quality override.'
        inputHandling = 'PASS — Both (2) retained.'
        tests = $tests
        openXrValidation = [ordered]@{ mandatoryErrors = [int]$openXr.mandatoryErrorCount; warnings = [int]$openXr.warningCount; status = 'PASS' }
        releaseSimulatorExclusion = 'PASS — release build processor stripped one simulator root from the build scene copy.'
        windowsNoRuntimeFallback = [ordered]@{ status = 'PASS'; mode = $smoke.mode; reason = $smoke.failureReason; camera = 1; listener = 1; mainCameraTag = 1 }
        openXrRuntimeAvailablePath = 'NOT_TESTED — loader initialization failed on this host; no working runtime/headset path available.'
        hardware = 'NOT_TESTED — headset/controller required.'
        requiredEvidence = @($evidence)
        blockers = @()
        status = 'PASS_WINDOWS_BUILD_VERIFIED_FOR_PHASE_1'
    }
    Write-Json $gate $gatePath
    Write-Output "P1_GATE=PASS SOURCE_SHA256=$($inventory.aggregateSha256)"
    exit 0
}

Assert-True (Test-Path -LiteralPath $gatePath -PathType Leaf) 'P1 gate manifest is missing.'
Assert-True (Test-Path -LiteralPath $inventoryPath -PathType Leaf) 'P1 source inventory is missing.'
$gate = Get-Content -LiteralPath $gatePath -Raw | ConvertFrom-Json
$inventory = Get-Content -LiteralPath $inventoryPath -Raw | ConvertFrom-Json
Assert-True ($gate.schema -eq 'vlab.phase-gate.v2' -and $gate.planVersion -eq '1.4') 'Unexpected P1 gate schema/version.'
Assert-True ($gate.planSha256 -eq (Get-Sha $plan)) 'Plan changed after P1 gate closure.'
Assert-True (@($gate.blockers).Count -eq 0) 'P1 gate has blockers.'
Assert-True ($gate.status -eq 'PASS_WINDOWS_BUILD_VERIFIED_FOR_PHASE_1') 'P1 gate status is not Pass.'
Assert-Inventory $inventory $project
Assert-True ($gate.source.aggregateSha256 -eq $inventory.aggregateSha256) 'Gate/source inventory mismatch.'
foreach ($row in $gate.requiredEvidence) {
    $path = Join-Path $project ([string]$row.path)
    Assert-True ($row.status -eq 'PASS' -and (Get-Sha $path) -eq [string]$row.sha256) "Evidence changed: $($row.id)"
}
Write-Output "P1_GATE_VERIFY=PASS SOURCE_SHA256=$($inventory.aggregateSha256)"
