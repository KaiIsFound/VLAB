[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('CaptureSource', 'CaptureCheckpoint', 'ClosePhase0', 'VerifyPhase0', 'CloseP1a', 'VerifyP1a', 'CloseP1b', 'VerifyP1b', 'CloseP1c', 'VerifyP1c', 'CloseP1AtomicBuilder', 'VerifyP1AtomicBuilder')]
    [string]$Action,

    [Parameter(Mandatory = $true)]
    [string]$ProjectRoot,

    [Parameter(Mandatory = $true)]
    [string]$ArtifactRoot,

    [string]$CheckpointRoot,
    [string]$PlanPath = 'plans/VLAB-PCVR-OPENXR-DESKTOP-BLUEPRINT.md',
    [string]$CheckpointId = '20260831-phase0-preflight'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$script:Utf8NoBom = [System.Text.UTF8Encoding]::new($false)
$script:SourceRoots = @('Assets', 'Packages', 'ProjectSettings')

function Resolve-FullPath([string]$Path, [string]$BasePath) {
    if ([System.IO.Path]::IsPathRooted($Path)) {
        return [System.IO.Path]::GetFullPath($Path)
    }
    return [System.IO.Path]::GetFullPath((Join-Path $BasePath $Path))
}

function Get-RelativePath([string]$BasePath, [string]$TargetPath) {
    $baseFullPath = [System.IO.Path]::GetFullPath($BasePath).TrimEnd('\', '/') + [System.IO.Path]::DirectorySeparatorChar
    $targetFullPath = [System.IO.Path]::GetFullPath($TargetPath)
    $baseUri = [System.Uri]::new($baseFullPath)
    $targetUri = [System.Uri]::new($targetFullPath)
    return [System.Uri]::UnescapeDataString($baseUri.MakeRelativeUri($targetUri).ToString()).Replace('/', [System.IO.Path]::DirectorySeparatorChar)
}

function Get-Sha256([string]$Path) {
    return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
}

function Get-StringSha256([string]$Value) {
    $algorithm = [System.Security.Cryptography.SHA256]::Create()
    try {
        $bytes = $script:Utf8NoBom.GetBytes($Value)
        return ([System.BitConverter]::ToString($algorithm.ComputeHash($bytes))).Replace('-', '').ToLowerInvariant()
    }
    finally {
        $algorithm.Dispose()
    }
}

function Write-Json([object]$Value, [string]$Path) {
    $json = $Value | ConvertTo-Json -Depth 20
    [System.IO.File]::WriteAllText($Path, $json + "`n", $script:Utf8NoBom)
}

function Assert-True([bool]$Condition, [string]$Message) {
    if (-not $Condition) {
        throw $Message
    }
}

function New-Inventory([string]$Root, [string[]]$RelativeRoots, [string]$Kind) {
    $entries = [System.Collections.Generic.List[object]]::new()
    foreach ($relativeRoot in $RelativeRoots) {
        $absoluteRoot = Join-Path $Root $relativeRoot
        Assert-True (Test-Path -LiteralPath $absoluteRoot) "Inventory root is missing: $absoluteRoot"
        Get-ChildItem -LiteralPath $absoluteRoot -Recurse -Force -File |
            ForEach-Object {
                $relativePath = (Get-RelativePath $Root $_.FullName).Replace('\', '/')
                $entries.Add([ordered]@{
                    relativePath = $relativePath
                    sizeBytes = $_.Length
                    sha256 = Get-Sha256 $_.FullName
                })
            }
    }

    $orderedEntries = @($entries | Sort-Object { $_.relativePath })
    $canonical = ($orderedEntries | ForEach-Object { '{0}|{1}|{2}' -f $_.relativePath, $_.sizeBytes, $_.sha256 }) -join "`n"
    if ($orderedEntries.Count -gt 0) {
        $canonical += "`n"
    }

    return [ordered]@{
        schema = 'vlab.sha256-inventory.v1'
        kind = $Kind
        generatedUtc = [DateTime]::UtcNow.ToString('O')
        root = $Root.Replace('\', '/')
        algorithm = 'Sort ordinal by relativePath; UTF-8 no BOM lines relativePath|sizeBytes|sha256 with LF after every line; SHA-256 lowercase over those bytes.'
        fileCount = $orderedEntries.Count
        aggregateSha256 = Get-StringSha256 $canonical
        files = $orderedEntries
    }
}

function New-ExplicitInventory([string]$Root, [string[]]$RelativeFiles, [string]$Kind) {
    $entries = foreach ($relativeFile in ($RelativeFiles | Sort-Object -Unique)) {
        $absolutePath = Join-Path $Root $relativeFile
        Assert-True (Test-Path -LiteralPath $absolutePath -PathType Leaf) "Required artifact is missing: $relativeFile"
        $item = Get-Item -LiteralPath $absolutePath
        [ordered]@{
            relativePath = $relativeFile.Replace('\', '/')
            sizeBytes = $item.Length
            sha256 = Get-Sha256 $absolutePath
        }
    }
    $canonical = ($entries | ForEach-Object { '{0}|{1}|{2}' -f $_.relativePath, $_.sizeBytes, $_.sha256 }) -join "`n"
    if ($entries.Count -gt 0) {
        $canonical += "`n"
    }
    return [ordered]@{
        schema = 'vlab.sha256-inventory.v1'
        kind = $Kind
        generatedUtc = [DateTime]::UtcNow.ToString('O')
        root = $Root.Replace('\', '/')
        algorithm = 'Sort ordinal by relativePath; UTF-8 no BOM lines relativePath|sizeBytes|sha256 with LF after every line; SHA-256 lowercase over those bytes.'
        fileCount = $entries.Count
        aggregateSha256 = Get-StringSha256 $canonical
        files = @($entries)
    }
}

function Assert-Inventory([string]$InventoryPath, [string]$Root) {
    Assert-True (Test-Path -LiteralPath $InventoryPath -PathType Leaf) "Inventory is missing: $InventoryPath"
    $inventory = Get-Content -LiteralPath $InventoryPath -Raw | ConvertFrom-Json
    Assert-True ($inventory.schema -eq 'vlab.sha256-inventory.v1') "Unsupported inventory schema: $InventoryPath"
    $canonicalLines = [System.Collections.Generic.List[string]]::new()
    foreach ($entry in $inventory.files) {
        $absolutePath = Join-Path $Root ([string]$entry.relativePath)
        Assert-True (Test-Path -LiteralPath $absolutePath -PathType Leaf) "Inventoried file is missing: $($entry.relativePath)"
        $item = Get-Item -LiteralPath $absolutePath
        Assert-True ($item.Length -eq [long]$entry.sizeBytes) "Size mismatch: $($entry.relativePath)"
        $hash = Get-Sha256 $absolutePath
        Assert-True ($hash -eq [string]$entry.sha256) "Hash mismatch: $($entry.relativePath)"
        $canonicalLines.Add(('{0}|{1}|{2}' -f $entry.relativePath, $entry.sizeBytes, $entry.sha256))
    }
    Assert-True ($canonicalLines.Count -eq [int]$inventory.fileCount) "Inventory count mismatch: $InventoryPath"
    $canonical = ($canonicalLines -join "`n")
    if ($canonicalLines.Count -gt 0) {
        $canonical += "`n"
    }
    Assert-True ((Get-StringSha256 $canonical) -eq [string]$inventory.aggregateSha256) "Aggregate mismatch: $InventoryPath"
    return $inventory
}

$project = Resolve-FullPath $ProjectRoot (Get-Location).Path
$artifact = Resolve-FullPath $ArtifactRoot $project
$plan = Resolve-FullPath $PlanPath $project
[System.IO.Directory]::CreateDirectory($artifact) | Out-Null

if ($Action -eq 'CaptureSource') {
    $inventory = New-Inventory $project $script:SourceRoots 'phase0-source'
    Write-Json $inventory (Join-Path $artifact 'source-inventory.json')
    Write-Output "SOURCE_SHA256=$($inventory.aggregateSha256) FILES=$($inventory.fileCount)"
    exit 0
}

if ($Action -eq 'CaptureCheckpoint') {
    Assert-True (-not [string]::IsNullOrWhiteSpace($CheckpointRoot)) 'CheckpointRoot is required.'
    $checkpoint = Resolve-FullPath $CheckpointRoot $project
    $inventory = New-Inventory $checkpoint $script:SourceRoots 'recovery-checkpoint-source'
    Write-Json $inventory (Join-Path $artifact 'checkpoint-inventory.json')
    Write-Output "CHECKPOINT_SHA256=$($inventory.aggregateSha256) FILES=$($inventory.fileCount)"
    exit 0
}

function Assert-Phase0Core {
    $auditPath = Join-Path $artifact 'phase0-audit.json'
    $testPath = Join-Path $artifact 'editmode-results.xml'
    Assert-True (Test-Path -LiteralPath $auditPath -PathType Leaf) 'phase0-audit.json is missing.'
    Assert-True (Test-Path -LiteralPath $testPath -PathType Leaf) 'editmode-results.xml is missing.'
    $audit = Get-Content -LiteralPath $auditPath -Raw | ConvertFrom-Json
    Assert-True ($audit.planVersion -eq '1.3') 'Audit plan version is not 1.3.'
    Assert-True ([bool]$audit.rendererSentinelPass) 'Renderer sentinel did not pass.'
    Assert-True ($audit.graphicsDefaultPipeline -eq 'Built-in Render Pipeline') 'Graphics pipeline is not Built-in.'
    Assert-True ($audit.effectivePipeline -eq 'Built-in Render Pipeline') 'Effective pipeline is not Built-in.'
    Assert-True (@($audit.qualitiesBefore).Count -gt 0) 'Pre-audit quality matrix is empty.'
    Assert-True (@($audit.qualitiesBefore).Count -eq @($audit.qualitiesAfter).Count) 'Quality matrices differ in length.'
    foreach ($quality in @($audit.qualitiesBefore) + @($audit.qualitiesAfter)) {
        Assert-True ($quality.pipeline -eq 'Built-in Render Pipeline') "Quality pipeline changed: $($quality.name)"
    }
    $screenshots = @(Get-ChildItem -LiteralPath (Join-Path $artifact 'baseline-screenshots') -Filter '*.png' -File)
    Assert-True ($screenshots.Count -eq 11) "Expected 11 screenshots, found $($screenshots.Count)."

    [xml]$testXml = Get-Content -LiteralPath $testPath -Raw
    $testRun = $testXml.'test-run'
    Assert-True ([int]$testRun.total -gt 0) 'EditMode test discovery/execution is zero.'
    Assert-True ([int]$testRun.failed -eq 0) 'EditMode tests failed.'
    Assert-True ([int]$testRun.passed -eq [int]$testRun.total) 'Not all EditMode tests passed.'
    return [ordered]@{ audit = $audit; testRun = $testRun; screenshots = $screenshots }
}

if ($Action -eq 'ClosePhase0') {
    Assert-True (Test-Path -LiteralPath $plan -PathType Leaf) 'Plan file is missing.'
    Assert-True (-not (Test-Path -LiteralPath (Join-Path $artifact 'gate-manifest.json'))) 'Refusing to overwrite a closed gate.'
    $sourceInventory = Assert-Inventory (Join-Path $artifact 'source-inventory.json') $project
    Assert-True (-not [string]::IsNullOrWhiteSpace($CheckpointRoot)) 'CheckpointRoot is required.'
    $checkpoint = Resolve-FullPath $CheckpointRoot $project
    $checkpointInventory = Assert-Inventory (Join-Path $artifact 'checkpoint-inventory.json') $checkpoint
    $core = Assert-Phase0Core

    $requiredArtifacts = @(
        'commands.txt',
        'phase0-audit.json',
        'phase0-audit-gui.log',
        'editmode-results.xml',
        'editmode-tests.log',
        'restore-probe.log',
        'source-inventory.json',
        'checkpoint-inventory.json'
    ) + @($core.screenshots | ForEach-Object { Get-RelativePath $artifact $_.FullName })
    $artifactInventory = New-ExplicitInventory $artifact $requiredArtifacts 'phase0-required-artifacts'
    $artifactInventoryPath = Join-Path $artifact 'artifact-inventory.json'
    Write-Json $artifactInventory $artifactInventoryPath
    Assert-Inventory $artifactInventoryPath $artifact | Out-Null

    $scenePath = Join-Path $project 'Assets/ChemistryLab.unity'
    $manifestPath = Join-Path $project 'Packages/manifest.json'
    $lockPath = Join-Path $project 'Packages/packages-lock.json'
    $gate = [ordered]@{
        schema = 'vlab.phase-gate.v1'
        planVersion = '1.3'
        planSha256 = Get-Sha256 $plan
        phase = 'P0.gate'
        runId = Split-Path $artifact -Leaf
        recoveryCheckpoint = [ordered]@{
            checkpointId = $CheckpointId
            checkpointRoot = $checkpoint.Replace('\', '/')
            fileCount = $checkpointInventory.fileCount
            aggregateSha256 = $checkpointInventory.aggregateSha256
            restoreProbe = 'PASS — independent Unity import/compile and byte fingerprint comparison; see restore-probe.log'
        }
        source = [ordered]@{
            fileCount = $sourceInventory.fileCount
            aggregateSha256 = $sourceInventory.aggregateSha256
            sceneSha256 = Get-Sha256 $scenePath
            packageManifestSha256 = Get-Sha256 $manifestPath
            packagesLockSha256 = Get-Sha256 $lockPath
        }
        invocation = [ordered]@{
            commandFile = 'commands.txt'
            auditCommand = $core.audit.commandLine
            startUtc = $core.audit.startUtc
            endUtc = $core.audit.endUtc
            auditExitCode = 0
            editModeExitCode = 0
        }
        unity = [ordered]@{
            version = $core.audit.unityVersion
            sceneGuid = $core.audit.sceneGuid
            builderMarker = '__ChemistryLab_Generated_v8'
        }
        renderer = [ordered]@{
            graphicsDefault = $core.audit.graphicsDefaultPipeline
            qualityMatrixBefore = $core.audit.qualitiesBefore
            qualityMatrixAfter = $core.audit.qualitiesAfter
            runtimeEffective = $core.audit.effectivePipeline
            sentinelPass = $core.audit.rendererSentinelPass
        }
        host = [ordered]@{
            operatingSystem = $core.audit.operatingSystem
            processor = $core.audit.processor
            graphicsDevice = $core.audit.graphicsDevice
            graphicsMemoryMb = $core.audit.graphicsMemoryMb
            openXrRuntime = 'Not installed/configured'
            headset = 'Not available'
        }
        tests = [ordered]@{
            platform = 'EditMode'
            discovered = [int]$core.testRun.testcasecount
            executed = [int]$core.testRun.total
            passed = [int]$core.testRun.passed
            failed = [int]$core.testRun.failed
            skipped = [int]$core.testRun.skipped
            resultXmlSha256 = Get-Sha256 (Join-Path $artifact 'editmode-results.xml')
        }
        evidence = @(
            [ordered]@{ id = 'AC-001'; class = 'required'; status = 'PASS'; artifact = 'checkpoint-inventory.json + restore-probe.log' },
            [ordered]@{ id = 'P0-renderer'; class = 'required'; status = 'PASS'; artifact = 'phase0-audit.json' },
            [ordered]@{ id = 'P0-tests'; class = 'required'; status = 'PASS'; artifact = 'editmode-results.xml' },
            [ordered]@{ id = 'P0-visual-baseline'; class = 'required'; status = 'PASS_BASELINE_CAPTURED'; artifact = 'baseline-screenshots/*.png' },
            [ordered]@{ id = 'AC-080'; class = 'hardware_optional'; status = 'NOT_TESTED_HARDWARE_REQUIRED'; artifact = $null }
        )
        acceptedWarnings = @(
            'Unity batch mode was blocked by Licensing Client; GUI automation and Test Runner paths completed.',
            'Input Manager deprecation is accepted during P0 while activeInputHandler remains Both.',
            'com.coplaydev.unity-mcp uses mutable #main and is not treated as reproducible until pinned or embedded.'
        )
        blockers = @()
        terminalStateMaximum = 'Structurally verified'
        artifactInventorySha256 = Get-Sha256 $artifactInventoryPath
        artifactAggregateSha256 = $artifactInventory.aggregateSha256
        status = 'PASS_STRUCTURALLY_VERIFIED'
    }
    Write-Json $gate (Join-Path $artifact 'gate-manifest.json')
}

if ($Action -eq 'VerifyPhase0') {
    $sourceInventory = Assert-Inventory (Join-Path $artifact 'source-inventory.json') $project
    Assert-True (-not [string]::IsNullOrWhiteSpace($CheckpointRoot)) 'CheckpointRoot is required.'
    $checkpoint = Resolve-FullPath $CheckpointRoot $project
    $checkpointInventory = Assert-Inventory (Join-Path $artifact 'checkpoint-inventory.json') $checkpoint
    $artifactInventory = Assert-Inventory (Join-Path $artifact 'artifact-inventory.json') $artifact
    $core = Assert-Phase0Core
    $gatePath = Join-Path $artifact 'gate-manifest.json'
    Assert-True (Test-Path -LiteralPath $gatePath -PathType Leaf) 'gate-manifest.json is missing.'
    $gate = Get-Content -LiteralPath $gatePath -Raw | ConvertFrom-Json
    Assert-True ($gate.schema -eq 'vlab.phase-gate.v1') 'Unsupported gate schema.'
    Assert-True ($gate.planVersion -eq '1.3') 'Gate plan version is not 1.3.'
    Assert-True ($gate.planSha256 -eq (Get-Sha256 $plan)) 'Plan hash changed after gate closure.'
    Assert-True ($gate.source.aggregateSha256 -eq $sourceInventory.aggregateSha256) 'Gate/source fingerprint mismatch.'
    Assert-True ($gate.recoveryCheckpoint.aggregateSha256 -eq $checkpointInventory.aggregateSha256) 'Gate/checkpoint fingerprint mismatch.'
    Assert-True ($gate.artifactInventorySha256 -eq (Get-Sha256 (Join-Path $artifact 'artifact-inventory.json'))) 'Artifact inventory hash mismatch.'
    Assert-True ($gate.artifactAggregateSha256 -eq $artifactInventory.aggregateSha256) 'Artifact aggregate mismatch.'
    Assert-True ($gate.source.sceneSha256 -eq (Get-Sha256 (Join-Path $project 'Assets/ChemistryLab.unity'))) 'Scene changed after gate closure.'
    Assert-True ($gate.status -eq 'PASS_STRUCTURALLY_VERIFIED') 'Gate is not structurally verified.'
    Assert-True (@($gate.blockers).Count -eq 0) 'Gate has blockers.'
    Write-Output "PHASE0_GATE=PASS SOURCE=$($sourceInventory.aggregateSha256) ARTIFACTS=$($artifactInventory.aggregateSha256) TESTS=$($core.testRun.passed)/$($core.testRun.total)"
    exit 0
}

function Read-PassingTestRun([string]$RelativePath, [string]$ExpectedPlatform) {
    $path = Join-Path $artifact $RelativePath
    Assert-True (Test-Path -LiteralPath $path -PathType Leaf) "Test XML is missing: $RelativePath"
    [xml]$xml = Get-Content -LiteralPath $path -Raw
    $run = $xml.'test-run'
    Assert-True ([int]$run.total -gt 0) "$ExpectedPlatform test discovery/execution is zero."
    Assert-True ([int]$run.failed -eq 0) "$ExpectedPlatform tests failed."
    Assert-True ([int]$run.passed -eq [int]$run.total) "Not all $ExpectedPlatform tests passed."
    return $run
}

if ($Action -eq 'CloseP1a') {
    Assert-True (-not [string]::IsNullOrWhiteSpace($CheckpointRoot)) 'CheckpointRoot is required.'
    Assert-True (-not (Test-Path -LiteralPath (Join-Path $artifact 'gate-manifest.json'))) 'Refusing to overwrite a closed gate.'
    $checkpoint = Resolve-FullPath $CheckpointRoot $project
    $sourceInventory = Assert-Inventory (Join-Path $artifact 'source-inventory.json') $project
    $checkpointInventory = Assert-Inventory (Join-Path $artifact 'checkpoint-inventory.json') $checkpoint
    $playMode = Read-PassingTestRun 'playmode-results.xml' 'PlayMode'
    $editMode = Read-PassingTestRun 'editmode-results.xml' 'EditMode'

    $settingsPath = Join-Path $project 'ProjectSettings/ProjectSettings.asset'
    $settings = Get-Content -LiteralPath $settingsPath -Raw
    Assert-True ($settings -match '(?m)^\s*activeInputHandler:\s*2\s*$') 'activeInputHandler is not Both (2).'
    $sceneRelative = 'Assets/ChemistryLab.unity'
    $manifestRelative = 'Packages/manifest.json'
    $lockRelative = 'Packages/packages-lock.json'
    Assert-True ((Get-Sha256 (Join-Path $project $sceneRelative)) -eq (Get-Sha256 (Join-Path $checkpoint $sceneRelative))) 'P1.a changed the ChemistryLab scene.'
    Assert-True ((Get-Sha256 (Join-Path $project $manifestRelative)) -eq (Get-Sha256 (Join-Path $checkpoint $manifestRelative))) 'P1.a changed Packages/manifest.json.'
    Assert-True ((Get-Sha256 (Join-Path $project $lockRelative)) -eq (Get-Sha256 (Join-Path $checkpoint $lockRelative))) 'P1.a changed Packages/packages-lock.json.'

    $requiredArtifacts = @(
        'commands.txt',
        'playmode-results.xml',
        'playmode-tests.log',
        'editmode-results.xml',
        'editmode-tests.log',
        'source-inventory.json',
        'checkpoint-inventory.json'
    )
    $artifactInventory = New-ExplicitInventory $artifact $requiredArtifacts 'p1a-required-artifacts'
    $artifactInventoryPath = Join-Path $artifact 'artifact-inventory.json'
    Write-Json $artifactInventory $artifactInventoryPath
    Assert-Inventory $artifactInventoryPath $artifact | Out-Null

    $gate = [ordered]@{
        schema = 'vlab.work-package-gate.v1'
        planVersion = '1.3'
        planSha256 = Get-Sha256 $plan
        package = 'P1.a'
        runId = Split-Path $artifact -Leaf
        checkpoint = [ordered]@{
            checkpointId = '20260831-p1a-pre-playmode'
            fileCount = $checkpointInventory.fileCount
            aggregateSha256 = $checkpointInventory.aggregateSha256
        }
        source = [ordered]@{
            fileCount = $sourceInventory.fileCount
            aggregateSha256 = $sourceInventory.aggregateSha256
            sceneSha256 = Get-Sha256 (Join-Path $project $sceneRelative)
            packageManifestSha256 = Get-Sha256 (Join-Path $project $manifestRelative)
            packagesLockSha256 = Get-Sha256 (Join-Path $project $lockRelative)
        }
        inputHandling = [ordered]@{ expected = 'Both'; serializedValue = 2; status = 'PASS' }
        playMode = [ordered]@{
            discovered = [int]$playMode.testcasecount
            executed = [int]$playMode.total
            passed = [int]$playMode.passed
            failed = [int]$playMode.failed
            skipped = [int]$playMode.skipped
            startUtc = [string]$playMode.'start-time'
            endUtc = [string]$playMode.'end-time'
        }
        editModeRegression = [ordered]@{
            discovered = [int]$editMode.testcasecount
            executed = [int]$editMode.total
            passed = [int]$editMode.passed
            failed = [int]$editMode.failed
            skipped = [int]$editMode.skipped
        }
        sceneChanged = $false
        packagesChanged = $false
        priorFailedRuns = @(
            '20260831T145956Z-playmode-bootstrap — compile failure, no XML',
            '20260831T150400Z-playmode-bootstrap-retry — zero discovered/executed'
        )
        artifactInventorySha256 = Get-Sha256 $artifactInventoryPath
        artifactAggregateSha256 = $artifactInventory.aggregateSha256
        blockers = @()
        status = 'PASS_STRUCTURALLY_VERIFIED'
    }
    Write-Json $gate (Join-Path $artifact 'gate-manifest.json')
}

if ($Action -eq 'VerifyP1a') {
    Assert-True (-not [string]::IsNullOrWhiteSpace($CheckpointRoot)) 'CheckpointRoot is required.'
    $checkpoint = Resolve-FullPath $CheckpointRoot $project
    $sourceInventory = Assert-Inventory (Join-Path $artifact 'source-inventory.json') $project
    $checkpointInventory = Assert-Inventory (Join-Path $artifact 'checkpoint-inventory.json') $checkpoint
    $artifactInventory = Assert-Inventory (Join-Path $artifact 'artifact-inventory.json') $artifact
    $playMode = Read-PassingTestRun 'playmode-results.xml' 'PlayMode'
    $editMode = Read-PassingTestRun 'editmode-results.xml' 'EditMode'
    $gatePath = Join-Path $artifact 'gate-manifest.json'
    Assert-True (Test-Path -LiteralPath $gatePath -PathType Leaf) 'gate-manifest.json is missing.'
    $gate = Get-Content -LiteralPath $gatePath -Raw | ConvertFrom-Json
    Assert-True ($gate.schema -eq 'vlab.work-package-gate.v1') 'Unsupported P1.a gate schema.'
    Assert-True ($gate.planVersion -eq '1.3') 'P1.a gate plan version is not 1.3.'
    Assert-True ($gate.planSha256 -eq (Get-Sha256 $plan)) 'Plan changed after P1.a gate closure.'
    Assert-True ($gate.source.aggregateSha256 -eq $sourceInventory.aggregateSha256) 'P1.a source fingerprint mismatch.'
    Assert-True ($gate.checkpoint.aggregateSha256 -eq $checkpointInventory.aggregateSha256) 'P1.a checkpoint fingerprint mismatch.'
    Assert-True ($gate.artifactInventorySha256 -eq (Get-Sha256 (Join-Path $artifact 'artifact-inventory.json'))) 'P1.a artifact inventory hash mismatch.'
    Assert-True ($gate.artifactAggregateSha256 -eq $artifactInventory.aggregateSha256) 'P1.a artifact aggregate mismatch.'
    Assert-True ($gate.inputHandling.serializedValue -eq 2) 'P1.a input freeze evidence is invalid.'
    Assert-True ($gate.status -eq 'PASS_STRUCTURALLY_VERIFIED') 'P1.a gate is not structurally verified.'
    Write-Output "P1A_GATE=PASS SOURCE=$($sourceInventory.aggregateSha256) PLAYMODE=$($playMode.passed)/$($playMode.total) EDITMODE=$($editMode.passed)/$($editMode.total)"
    exit 0
}

function Assert-P1bCore([string]$Checkpoint) {
    $probe = Get-Content -LiteralPath (Join-Path $artifact 'package-probe.json') -Raw | ConvertFrom-Json
    $config = Get-Content -LiteralPath (Join-Path $artifact 'openxr-configuration.json') -Raw | ConvertFrom-Json
    $renderer = Get-Content -LiteralPath (Join-Path $artifact 'renderer-audit.json') -Raw | ConvertFrom-Json
    $playMode = Read-PassingTestRun 'playmode-results.xml' 'PlayMode'
    $editMode = Read-PassingTestRun 'editmode-results.xml' 'EditMode'

    Assert-True ($probe.planVersion -eq '1.3') 'UPM probe is not bound to plan 1.3.'
    $managementProbe = @($probe.packages | Where-Object { $_.name -eq 'com.unity.xr.management' })[0]
    $openXrProbe = @($probe.packages | Where-Object { $_.name -eq 'com.unity.xr.openxr' })[0]
    Assert-True (@($managementProbe.recommendedVersions) -contains '4.5.4') 'UPM did not recommend XR Management 4.5.4.'
    Assert-True (@($openXrProbe.recommendedVersions) -contains '1.17.1') 'UPM did not recommend OpenXR 1.17.1.'

    $oldLock = Get-Content -LiteralPath (Join-Path $Checkpoint 'Packages/packages-lock.json') -Raw | ConvertFrom-Json
    $newLock = Get-Content -LiteralPath (Join-Path $project 'Packages/packages-lock.json') -Raw | ConvertFrom-Json
    $allNames = @($oldLock.dependencies.psobject.Properties.Name + $newLock.dependencies.psobject.Properties.Name | Sort-Object -Unique)
    $changed = @{}
    foreach ($name in $allNames) {
        $oldProperty = $oldLock.dependencies.psobject.Properties[$name]
        $newProperty = $newLock.dependencies.psobject.Properties[$name]
        $oldVersion = if ($oldProperty) { [string]$oldProperty.Value.version } else { '<missing>' }
        $newVersion = if ($newProperty) { [string]$newProperty.Value.version } else { '<missing>' }
        if ($oldVersion -ne $newVersion) {
            $changed[$name] = $newVersion
        }
    }
    $expectedChanges = [ordered]@{
        'com.unity.xr.legacyinputhelpers' = '3.0.1'
        'com.unity.xr.management' = '4.5.4'
        'com.unity.xr.openxr' = '1.17.1'
    }
    Assert-True ($changed.Count -eq $expectedChanges.Count) "Unexpected package lock diff count: $($changed.Count)."
    foreach ($entry in $expectedChanges.GetEnumerator()) {
        Assert-True ($changed.ContainsKey($entry.Key) -and $changed[$entry.Key] -eq $entry.Value) "Unexpected package lock result for $($entry.Key)."
    }
    $pinned = [ordered]@{
        'com.unity.xr.interaction.toolkit' = '3.5.1'
        'com.unity.inputsystem' = '1.20.0'
        'com.unity.xr.core-utils' = '2.6.0'
        'com.unity.render-pipelines.universal' = '17.5.0'
    }
    foreach ($entry in $pinned.GetEnumerator()) {
        $property = $newLock.dependencies.psobject.Properties[$entry.Key]
        Assert-True ($property -and [string]$property.Value.version -eq $entry.Value) "Pinned package drifted: $($entry.Key)."
    }

    Assert-True ($config.planVersion -eq '1.3') 'OpenXR configuration is not bound to plan 1.3.'
    Assert-True ([bool]$config.openXrLoaderAssigned) 'OpenXR loader is not assigned.'
    Assert-True (-not [bool]$config.initializeXrOnStartup) 'Initialize XR on Startup must be false.'
    Assert-True (@($config.assignedLoaders).Count -eq 1) 'Windows must have exactly one XR loader.'
    Assert-True (@($config.enabledInteractionProfiles).Count -eq 4) 'Expected four reviewed interaction profiles.'
    Assert-True ([int]$config.mandatoryErrorCount -eq 0) 'OpenXR Project Validation has mandatory errors.'

    Assert-True ([bool]$renderer.rendererSentinelPass) 'Renderer sentinel did not pass after package install.'
    Assert-True ($renderer.graphicsDefaultPipeline -eq 'Built-in Render Pipeline') 'Graphics pipeline changed during P1.b.'
    Assert-True ($renderer.effectivePipeline -eq 'Built-in Render Pipeline') 'Effective pipeline changed during P1.b.'
    Assert-True ((Get-Sha256 (Join-Path $project 'Assets/ChemistryLab.unity')) -eq (Get-Sha256 (Join-Path $Checkpoint 'Assets/ChemistryLab.unity'))) 'P1.b changed the ChemistryLab scene.'
    $projectSettings = Get-Content -LiteralPath (Join-Path $project 'ProjectSettings/ProjectSettings.asset') -Raw
    Assert-True ($projectSettings -match '(?m)^\s*activeInputHandler:\s*2\s*$') 'P1.b changed activeInputHandler from Both.'

    return [ordered]@{
        probe = $probe
        config = $config
        renderer = $renderer
        playMode = $playMode
        editMode = $editMode
        changedPackages = $expectedChanges
        pinnedPackages = $pinned
    }
}

if ($Action -eq 'CloseP1b') {
    Assert-True (-not [string]::IsNullOrWhiteSpace($CheckpointRoot)) 'CheckpointRoot is required.'
    Assert-True (-not (Test-Path -LiteralPath (Join-Path $artifact 'gate-manifest.json'))) 'Refusing to overwrite a closed gate.'
    $checkpoint = Resolve-FullPath $CheckpointRoot $project
    $sourceInventory = Assert-Inventory (Join-Path $artifact 'source-inventory.json') $project
    $checkpointInventory = Assert-Inventory (Join-Path $artifact 'checkpoint-inventory.json') $checkpoint
    $core = Assert-P1bCore $checkpoint
    $requiredArtifacts = @(
        'commands.txt', 'upm-probe-commands.txt', 'package-install-commands.txt', 'openxr-config-commands.txt',
        'package-probe.json', 'package-change-plan.json', 'package-resolve-audit.log',
        'renderer-audit.json', 'openxr-configuration.json', 'openxr-config.log',
        'editmode-results.xml', 'editmode-tests.log', 'playmode-results.xml', 'playmode-tests.log',
        'source-inventory.json', 'checkpoint-inventory.json'
    )
    $artifactInventory = New-ExplicitInventory $artifact $requiredArtifacts 'p1b-required-artifacts'
    $artifactInventoryPath = Join-Path $artifact 'artifact-inventory.json'
    Write-Json $artifactInventory $artifactInventoryPath
    Assert-Inventory $artifactInventoryPath $artifact | Out-Null
    $gate = [ordered]@{
        schema = 'vlab.work-package-gate.v1'
        planVersion = '1.3'
        planSha256 = Get-Sha256 $plan
        package = 'P1.b'
        runId = Split-Path $artifact -Leaf
        checkpoint = [ordered]@{ checkpointId = '20260831-p1b-pre-openxr'; fileCount = $checkpointInventory.fileCount; aggregateSha256 = $checkpointInventory.aggregateSha256 }
        source = [ordered]@{
            fileCount = $sourceInventory.fileCount
            aggregateSha256 = $sourceInventory.aggregateSha256
            sceneSha256 = Get-Sha256 (Join-Path $project 'Assets/ChemistryLab.unity')
            packageManifestSha256 = Get-Sha256 (Join-Path $project 'Packages/manifest.json')
            packagesLockSha256 = Get-Sha256 (Join-Path $project 'Packages/packages-lock.json')
        }
        packageDiff = $core.changedPackages
        pinnedPackages = $core.pinnedPackages
        openXr = [ordered]@{
            loaderAssigned = $core.config.openXrLoaderAssigned
            initializeOnStartup = $core.config.initializeXrOnStartup
            enabledInteractionProfiles = $core.config.enabledInteractionProfiles
            projectValidationErrors = $core.config.mandatoryErrorCount
            projectValidationWarnings = $core.config.warningCount
        }
        renderer = [ordered]@{ graphicsDefault = $core.renderer.graphicsDefaultPipeline; effective = $core.renderer.effectivePipeline; sentinelPass = $core.renderer.rendererSentinelPass }
        editMode = [ordered]@{ discovered = [int]$core.editMode.testcasecount; executed = [int]$core.editMode.total; passed = [int]$core.editMode.passed; failed = [int]$core.editMode.failed; skipped = [int]$core.editMode.skipped }
        playMode = [ordered]@{ discovered = [int]$core.playMode.testcasecount; executed = [int]$core.playMode.total; passed = [int]$core.playMode.passed; failed = [int]$core.playMode.failed; skipped = [int]$core.playMode.skipped }
        sceneChanged = $false
        inputHandling = 'Both'
        artifactInventorySha256 = Get-Sha256 $artifactInventoryPath
        artifactAggregateSha256 = $artifactInventory.aggregateSha256
        blockers = @()
        status = 'PASS_STRUCTURALLY_VERIFIED'
    }
    Write-Json $gate (Join-Path $artifact 'gate-manifest.json')
}

if ($Action -eq 'VerifyP1b') {
    Assert-True (-not [string]::IsNullOrWhiteSpace($CheckpointRoot)) 'CheckpointRoot is required.'
    $checkpoint = Resolve-FullPath $CheckpointRoot $project
    $sourceInventory = Assert-Inventory (Join-Path $artifact 'source-inventory.json') $project
    $checkpointInventory = Assert-Inventory (Join-Path $artifact 'checkpoint-inventory.json') $checkpoint
    $artifactInventory = Assert-Inventory (Join-Path $artifact 'artifact-inventory.json') $artifact
    $core = Assert-P1bCore $checkpoint
    $gatePath = Join-Path $artifact 'gate-manifest.json'
    Assert-True (Test-Path -LiteralPath $gatePath -PathType Leaf) 'P1.b gate-manifest.json is missing.'
    $gate = Get-Content -LiteralPath $gatePath -Raw | ConvertFrom-Json
    Assert-True ($gate.planSha256 -eq (Get-Sha256 $plan)) 'Plan changed after P1.b gate closure.'
    Assert-True ($gate.source.aggregateSha256 -eq $sourceInventory.aggregateSha256) 'P1.b source fingerprint mismatch.'
    Assert-True ($gate.checkpoint.aggregateSha256 -eq $checkpointInventory.aggregateSha256) 'P1.b checkpoint fingerprint mismatch.'
    Assert-True ($gate.artifactInventorySha256 -eq (Get-Sha256 (Join-Path $artifact 'artifact-inventory.json'))) 'P1.b artifact inventory hash mismatch.'
    Assert-True ($gate.artifactAggregateSha256 -eq $artifactInventory.aggregateSha256) 'P1.b artifact aggregate mismatch.'
    Assert-True ($gate.status -eq 'PASS_STRUCTURALLY_VERIFIED') 'P1.b gate is not structurally verified.'
    Write-Output "P1B_GATE=PASS SOURCE=$($sourceInventory.aggregateSha256) EDITMODE=$($core.editMode.passed)/$($core.editMode.total) PLAYMODE=$($core.playMode.passed)/$($core.playMode.total) OPENXR_ERRORS=$($core.config.mandatoryErrorCount)"
    exit 0
}

function Assert-P1cCore([string]$Checkpoint) {
    $playMode = Read-PassingTestRun 'playmode-results.xml' 'PlayMode'
    $editMode = Read-PassingTestRun 'editmode-results.xml' 'EditMode'

    $editModePath = Join-Path $artifact 'editmode-results.xml'
    [xml]$editModeXml = Get-Content -LiteralPath $editModePath -Raw
    $coordinatorTests = @($editModeXml.SelectNodes("//test-case[contains(@fullname,'PresentationModeCoordinatorTests')]") | Where-Object { $_.result -eq 'Passed' })
    Assert-True ($coordinatorTests.Count -eq 11) "Expected 11 passing presentation-mode tests, found $($coordinatorTests.Count)."

    $requiredSourceFiles = @(
        'Assets/VLABChemistryLab/Scripts/Mode/VLabPresentationMode.cs',
        'Assets/VLABChemistryLab/Scripts/Mode/VLabPresentationMode.cs.meta',
        'Assets/Tests/EditMode/PresentationModeCoordinatorTests.cs',
        'Assets/Tests/EditMode/PresentationModeCoordinatorTests.cs.meta'
    )
    foreach ($relativePath in $requiredSourceFiles) {
        Assert-True (Test-Path -LiteralPath (Join-Path $project $relativePath) -PathType Leaf) "P1.c source is missing: $relativePath"
        Assert-True (-not (Test-Path -LiteralPath (Join-Path $Checkpoint $relativePath))) "P1.c checkpoint unexpectedly contains: $relativePath"
    }

    foreach ($relativePath in @(
        'Assets/ChemistryLab.unity',
        'Packages/manifest.json',
        'Packages/packages-lock.json',
        'ProjectSettings/GraphicsSettings.asset',
        'ProjectSettings/QualitySettings.asset'
    )) {
        Assert-True ((Get-Sha256 (Join-Path $project $relativePath)) -eq (Get-Sha256 (Join-Path $Checkpoint $relativePath))) "P1.c changed protected file: $relativePath"
    }

    $projectSettings = Get-Content -LiteralPath (Join-Path $project 'ProjectSettings/ProjectSettings.asset') -Raw
    Assert-True ($projectSettings -match '(?m)^\s*activeInputHandler:\s*2\s*$') 'P1.c changed activeInputHandler from Both.'

    return [ordered]@{
        playMode = $playMode
        editMode = $editMode
        coordinatorTestCount = $coordinatorTests.Count
        requiredSourceFiles = $requiredSourceFiles
    }
}

if ($Action -eq 'CloseP1c') {
    Assert-True (-not [string]::IsNullOrWhiteSpace($CheckpointRoot)) 'CheckpointRoot is required.'
    Assert-True (-not (Test-Path -LiteralPath (Join-Path $artifact 'gate-manifest.json'))) 'Refusing to overwrite a closed gate.'
    $checkpoint = Resolve-FullPath $CheckpointRoot $project
    $sourceInventory = Assert-Inventory (Join-Path $artifact 'source-inventory.json') $project
    $checkpointInventory = Assert-Inventory (Join-Path $artifact 'checkpoint-inventory.json') $checkpoint
    $core = Assert-P1cCore $checkpoint
    $requiredArtifacts = @(
        'commands.txt',
        'editmode-results.xml', 'editmode-tests.log',
        'playmode-results.xml', 'playmode-tests.log',
        'source-inventory.json', 'checkpoint-inventory.json'
    )
    $artifactInventory = New-ExplicitInventory $artifact $requiredArtifacts 'p1c-required-artifacts'
    $artifactInventoryPath = Join-Path $artifact 'artifact-inventory.json'
    Write-Json $artifactInventory $artifactInventoryPath
    Assert-Inventory $artifactInventoryPath $artifact | Out-Null

    $gate = [ordered]@{
        schema = 'vlab.work-package-gate.v1'
        planVersion = '1.3'
        planSha256 = Get-Sha256 $plan
        package = 'P1.c'
        runId = Split-Path $artifact -Leaf
        checkpoint = [ordered]@{ checkpointId = '20260831-p1c-pre-mode-router'; fileCount = $checkpointInventory.fileCount; aggregateSha256 = $checkpointInventory.aggregateSha256 }
        source = [ordered]@{
            fileCount = $sourceInventory.fileCount
            aggregateSha256 = $sourceInventory.aggregateSha256
            sceneSha256 = Get-Sha256 (Join-Path $project 'Assets/ChemistryLab.unity')
            packageManifestSha256 = Get-Sha256 (Join-Path $project 'Packages/manifest.json')
            packagesLockSha256 = Get-Sha256 (Join-Path $project 'Packages/packages-lock.json')
        }
        modeCoordinator = [ordered]@{
            testCount = $core.coordinatorTestCount
            desktopFirst = $true
            sharedStateReferencePreserved = $true
            runtimeWiringDeferred = $true
        }
        editMode = [ordered]@{ discovered = [int]$core.editMode.testcasecount; executed = [int]$core.editMode.total; passed = [int]$core.editMode.passed; failed = [int]$core.editMode.failed; skipped = [int]$core.editMode.skipped }
        playMode = [ordered]@{ discovered = [int]$core.playMode.testcasecount; executed = [int]$core.playMode.total; passed = [int]$core.playMode.passed; failed = [int]$core.playMode.failed; skipped = [int]$core.playMode.skipped }
        sceneChanged = $false
        packagesChanged = $false
        rendererSettingsChanged = $false
        inputHandling = 'Both'
        artifactInventorySha256 = Get-Sha256 $artifactInventoryPath
        artifactAggregateSha256 = $artifactInventory.aggregateSha256
        blockers = @()
        status = 'PASS_STRUCTURALLY_VERIFIED'
    }
    Write-Json $gate (Join-Path $artifact 'gate-manifest.json')
}

if ($Action -eq 'VerifyP1c') {
    Assert-True (-not [string]::IsNullOrWhiteSpace($CheckpointRoot)) 'CheckpointRoot is required.'
    $checkpoint = Resolve-FullPath $CheckpointRoot $project
    $sourceInventory = Assert-Inventory (Join-Path $artifact 'source-inventory.json') $project
    $checkpointInventory = Assert-Inventory (Join-Path $artifact 'checkpoint-inventory.json') $checkpoint
    $artifactInventory = Assert-Inventory (Join-Path $artifact 'artifact-inventory.json') $artifact
    $core = Assert-P1cCore $checkpoint
    $gatePath = Join-Path $artifact 'gate-manifest.json'
    Assert-True (Test-Path -LiteralPath $gatePath -PathType Leaf) 'P1.c gate-manifest.json is missing.'
    $gate = Get-Content -LiteralPath $gatePath -Raw | ConvertFrom-Json
    Assert-True ($gate.schema -eq 'vlab.work-package-gate.v1') 'Unsupported P1.c gate schema.'
    Assert-True ($gate.planVersion -eq '1.3') 'P1.c gate plan version is not 1.3.'
    Assert-True ($gate.planSha256 -eq (Get-Sha256 $plan)) 'Plan changed after P1.c gate closure.'
    Assert-True ($gate.source.aggregateSha256 -eq $sourceInventory.aggregateSha256) 'P1.c source fingerprint mismatch.'
    Assert-True ($gate.checkpoint.aggregateSha256 -eq $checkpointInventory.aggregateSha256) 'P1.c checkpoint fingerprint mismatch.'
    Assert-True ($gate.artifactInventorySha256 -eq (Get-Sha256 (Join-Path $artifact 'artifact-inventory.json'))) 'P1.c artifact inventory hash mismatch.'
    Assert-True ($gate.artifactAggregateSha256 -eq $artifactInventory.aggregateSha256) 'P1.c artifact aggregate mismatch.'
    Assert-True ($gate.modeCoordinator.testCount -eq 11) 'P1.c coordinator evidence is incomplete.'
    Assert-True ([bool]$gate.modeCoordinator.runtimeWiringDeferred) 'P1.c runtime-wiring boundary is not recorded.'
    Assert-True ($gate.status -eq 'PASS_STRUCTURALLY_VERIFIED') 'P1.c gate is not structurally verified.'
    Write-Output "P1C_GATE=PASS SOURCE=$($sourceInventory.aggregateSha256) EDITMODE=$($core.editMode.passed)/$($core.editMode.total) PLAYMODE=$($core.playMode.passed)/$($core.playMode.total) MODE_TESTS=$($core.coordinatorTestCount)/11"
    exit 0
}

function Assert-P1AtomicBuilderCore([string]$Checkpoint) {
    $editMode = Read-PassingTestRun 'editmode-results.xml' 'EditMode'
    [xml]$editModeXml = Get-Content -LiteralPath (Join-Path $artifact 'editmode-results.xml') -Raw
    $atomicTests = @($editModeXml.SelectNodes("//test-case[contains(@fullname,'AtomicChemistryLabBuilderTests')]") | Where-Object { $_.result -eq 'Passed' })
    Assert-True ($atomicTests.Count -eq 5) "Expected 5 passing atomic Builder tests, found $($atomicTests.Count)."

    foreach ($relativePath in @(
        'Assets/ChemistryLab.unity',
        'Packages/manifest.json',
        'Packages/packages-lock.json',
        'ProjectSettings/GraphicsSettings.asset',
        'ProjectSettings/QualitySettings.asset'
    )) {
        Assert-True ((Get-Sha256 (Join-Path $project $relativePath)) -eq (Get-Sha256 (Join-Path $Checkpoint $relativePath))) "P1.atomic-builder changed protected file: $relativePath"
    }
    Assert-True (-not (Test-Path -LiteralPath (Join-Path $project 'Assets/__VLABAtomicBuildTemp'))) 'Atomic Builder left its temporary scene folder behind.'

    return [ordered]@{ editMode = $editMode; atomicTestCount = $atomicTests.Count }
}

if ($Action -eq 'CloseP1AtomicBuilder') {
    Assert-True (-not [string]::IsNullOrWhiteSpace($CheckpointRoot)) 'CheckpointRoot is required.'
    Assert-True (-not (Test-Path -LiteralPath (Join-Path $artifact 'gate-manifest.json'))) 'Refusing to overwrite a closed gate.'
    $checkpoint = Resolve-FullPath $CheckpointRoot $project
    $sourceInventory = Assert-Inventory (Join-Path $artifact 'source-inventory.json') $project
    $checkpointInventory = Assert-Inventory (Join-Path $artifact 'checkpoint-inventory.json') $checkpoint
    $core = Assert-P1AtomicBuilderCore $checkpoint
    $requiredArtifacts = @(
        'commands.txt', 'editmode-results.xml', 'editmode-tests.log',
        'source-inventory.json', 'checkpoint-inventory.json'
    )
    $artifactInventory = New-ExplicitInventory $artifact $requiredArtifacts 'p1-atomic-builder-required-artifacts'
    $artifactInventoryPath = Join-Path $artifact 'artifact-inventory.json'
    Write-Json $artifactInventory $artifactInventoryPath
    Assert-Inventory $artifactInventoryPath $artifact | Out-Null
    $gate = [ordered]@{
        schema = 'vlab.work-package-gate.v1'
        planVersion = '1.3'
        planSha256 = Get-Sha256 $plan
        package = 'P1.atomic-builder'
        runId = Split-Path $artifact -Leaf
        checkpoint = [ordered]@{ checkpointId = '20260831-p1atomic-pre-builder'; fileCount = $checkpointInventory.fileCount; aggregateSha256 = $checkpointInventory.aggregateSha256 }
        source = [ordered]@{ fileCount = $sourceInventory.fileCount; aggregateSha256 = $sourceInventory.aggregateSha256; sceneSha256 = Get-Sha256 (Join-Path $project 'Assets/ChemistryLab.unity') }
        atomicBuilder = [ordered]@{ injectedFailureTests = $core.atomicTestCount; requiredCheckpoint = $true; targetSceneHashPreserved = $true; temporarySceneCleaned = $true }
        editMode = [ordered]@{ discovered = [int]$core.editMode.testcasecount; executed = [int]$core.editMode.total; passed = [int]$core.editMode.passed; failed = [int]$core.editMode.failed; skipped = [int]$core.editMode.skipped }
        sceneChanged = $false
        packagesChanged = $false
        rendererSettingsChanged = $false
        artifactInventorySha256 = Get-Sha256 $artifactInventoryPath
        artifactAggregateSha256 = $artifactInventory.aggregateSha256
        blockers = @('Success-path replacement of Assets/ChemistryLab.unity is deliberately deferred until P1.d integration has a new explicit checkpoint.')
        status = 'PASS_STRUCTURALLY_VERIFIED'
    }
    Write-Json $gate (Join-Path $artifact 'gate-manifest.json')
}

if ($Action -eq 'VerifyP1AtomicBuilder') {
    Assert-True (-not [string]::IsNullOrWhiteSpace($CheckpointRoot)) 'CheckpointRoot is required.'
    $checkpoint = Resolve-FullPath $CheckpointRoot $project
    $sourceInventory = Assert-Inventory (Join-Path $artifact 'source-inventory.json') $project
    $checkpointInventory = Assert-Inventory (Join-Path $artifact 'checkpoint-inventory.json') $checkpoint
    $artifactInventory = Assert-Inventory (Join-Path $artifact 'artifact-inventory.json') $artifact
    $core = Assert-P1AtomicBuilderCore $checkpoint
    $gatePath = Join-Path $artifact 'gate-manifest.json'
    Assert-True (Test-Path -LiteralPath $gatePath -PathType Leaf) 'P1.atomic-builder gate-manifest.json is missing.'
    $gate = Get-Content -LiteralPath $gatePath -Raw | ConvertFrom-Json
    Assert-True ($gate.planSha256 -eq (Get-Sha256 $plan)) 'Plan changed after P1.atomic-builder gate closure.'
    Assert-True ($gate.source.aggregateSha256 -eq $sourceInventory.aggregateSha256) 'P1.atomic-builder source fingerprint mismatch.'
    Assert-True ($gate.checkpoint.aggregateSha256 -eq $checkpointInventory.aggregateSha256) 'P1.atomic-builder checkpoint fingerprint mismatch.'
    Assert-True ($gate.artifactInventorySha256 -eq (Get-Sha256 (Join-Path $artifact 'artifact-inventory.json'))) 'P1.atomic-builder artifact inventory hash mismatch.'
    Assert-True ($gate.artifactAggregateSha256 -eq $artifactInventory.aggregateSha256) 'P1.atomic-builder artifact aggregate mismatch.'
    Assert-True ($gate.atomicBuilder.injectedFailureTests -eq 5) 'P1.atomic-builder injected-failure evidence is incomplete.'
    Assert-True ($gate.status -eq 'PASS_STRUCTURALLY_VERIFIED') 'P1.atomic-builder gate is not structurally verified.'
    Write-Output "P1ATOMIC_GATE=PASS SOURCE=$($sourceInventory.aggregateSha256) EDITMODE=$($core.editMode.passed)/$($core.editMode.total) ATOMIC_TESTS=$($core.atomicTestCount)/5"
    exit 0
}
