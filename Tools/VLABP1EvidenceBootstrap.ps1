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
    [string]$CheckpointId,

    [string]$PlanPath = 'plans/VLAB-PCVR-OPENXR-DESKTOP-BLUEPRINT.md'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$utf8 = [System.Text.UTF8Encoding]::new($false)
$sourceRoots = @('Assets', 'Packages', 'ProjectSettings')

function Resolve-Absolute([string]$Path, [string]$Base) {
    if ([System.IO.Path]::IsPathRooted($Path)) { return [System.IO.Path]::GetFullPath($Path) }
    return [System.IO.Path]::GetFullPath((Join-Path $Base $Path))
}

function Assert-True([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
}

function Get-Sha([string]$Path) {
    return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
}

function Get-TextSha([string]$Text) {
    $algorithm = [System.Security.Cryptography.SHA256]::Create()
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

function New-Inventory([string]$Root, [string]$Kind) {
    $entries = foreach ($relativeRoot in $sourceRoots) {
        $absoluteRoot = Join-Path $Root $relativeRoot
        Assert-True (Test-Path -LiteralPath $absoluteRoot -PathType Container) "Missing inventory root: $absoluteRoot"
        Get-ChildItem -LiteralPath $absoluteRoot -Recurse -Force -File | ForEach-Object {
            [ordered]@{
                relativePath = Get-Relative $Root $_.FullName
                sizeBytes = $_.Length
                sha256 = Get-Sha $_.FullName
            }
        }
    }
    $entries = @($entries | Sort-Object { $_.relativePath })
    $canonical = ($entries | ForEach-Object { '{0}|{1}|{2}' -f $_.relativePath, $_.sizeBytes, $_.sha256 }) -join "`n"
    if ($entries.Count -gt 0) { $canonical += "`n" }
    return [ordered]@{
        schema = 'vlab.sha256-inventory.v1'
        kind = $Kind
        fileCount = $entries.Count
        aggregateSha256 = Get-TextSha $canonical
        files = $entries
    }
}

function Write-Json([object]$Value, [string]$Path) {
    [IO.File]::WriteAllText($Path, (($Value | ConvertTo-Json -Depth 30) + "`n"), $utf8)
}

function Assert-Inventory([object]$Inventory, [string]$Root) {
    $canonical = [Collections.Generic.List[string]]::new()
    foreach ($entry in $Inventory.files) {
        $path = Join-Path $Root ([string]$entry.relativePath)
        Assert-True (Test-Path -LiteralPath $path -PathType Leaf) "Inventoried file missing: $($entry.relativePath)"
        $item = Get-Item -LiteralPath $path
        Assert-True ($item.Length -eq [long]$entry.sizeBytes) "Inventoried size changed: $($entry.relativePath)"
        $sha = Get-Sha $path
        Assert-True ($sha -eq [string]$entry.sha256) "Inventoried hash changed: $($entry.relativePath)"
        $canonical.Add(('{0}|{1}|{2}' -f $entry.relativePath, $entry.sizeBytes, $entry.sha256))
    }
    $text = $canonical -join "`n"
    if ($canonical.Count -gt 0) { $text += "`n" }
    Assert-True ((Get-TextSha $text) -eq [string]$Inventory.aggregateSha256) 'Inventory aggregate mismatch.'
}

$project = Resolve-Absolute $ProjectRoot (Get-Location).Path
$artifact = Resolve-Absolute $ArtifactRoot $project
$checkpoint = Resolve-Absolute $CheckpointRoot $project
$plan = Resolve-Absolute $PlanPath $project
$gatePath = Join-Path $artifact 'gate-manifest.json'

Assert-True (Test-Path -LiteralPath $project -PathType Container) 'Project root is missing.'
Assert-True (Test-Path -LiteralPath $checkpoint -PathType Container) 'Checkpoint root is missing.'
Assert-True ((Split-Path $checkpoint -Leaf) -ceq $CheckpointId) 'Checkpoint directory name does not match CheckpointId.'
Assert-True (Test-Path -LiteralPath $plan -PathType Leaf) 'Plan file is missing.'

if ($Action -eq 'Close') {
    Assert-True (-not (Test-Path -LiteralPath $gatePath)) 'Refusing to overwrite a closed evidence-bootstrap gate.'
    [IO.Directory]::CreateDirectory($artifact) | Out-Null

    $source = New-Inventory $project 'p1-evidence-bootstrap-source'
    $checkpointInventory = New-Inventory $checkpoint 'p1-evidence-bootstrap-checkpoint'
    Assert-True ($source.aggregateSha256 -eq $checkpointInventory.aggregateSha256) 'Checkpoint does not reproduce the current source roots.'

    $sceneRelative = 'Assets/ChemistryLab.unity'
    $projectScene = Join-Path $project $sceneRelative
    $checkpointScene = Join-Path $checkpoint $sceneRelative
    Assert-True ((Get-Sha $projectScene) -eq (Get-Sha $checkpointScene)) 'Checkpoint scene hash differs from the target scene.'

    $dependencyPaths = @(
        'Artifacts/VLABUpgrade/plan-1.3/P0/20260831T144944Z-p0audit/gate-manifest.json',
        'Artifacts/VLABUpgrade/plan-1.3/P1.a/20260831T150620Z-playmode-bootstrap-final/gate-manifest.json',
        'Artifacts/VLABUpgrade/plan-1.3/P1.b/20260831T155433Z-openxr-tests-retry/gate-manifest.json',
        'Artifacts/VLABUpgrade/plan-1.3/P1.c/20260831T160724Z-mode-router/gate-manifest.json',
        'Artifacts/VLABUpgrade/plan-1.3/P1.atomic-builder/20260901T103025Z-editmode-final/gate-manifest.json'
    )
    $dependencies = foreach ($relativePath in $dependencyPaths) {
        $path = Join-Path $project $relativePath
        Assert-True (Test-Path -LiteralPath $path -PathType Leaf) "Dependency gate is missing: $relativePath"
        $dependency = Get-Content -LiteralPath $path -Raw | ConvertFrom-Json
        Assert-True (([string]$dependency.status).StartsWith('PASS')) "Dependency gate is not PASS: $relativePath"
        [ordered]@{ path = $relativePath; sha256 = Get-Sha $path; status = $dependency.status }
    }

    Write-Json $source (Join-Path $artifact 'source-inventory.json')
    Write-Json $checkpointInventory (Join-Path $artifact 'checkpoint-inventory.json')
    $renderer = Get-Content -LiteralPath (Join-Path $project 'ProjectSettings/GraphicsSettings.asset') -Raw
    Assert-True ($renderer -match '(?m)^\s*m_CustomRenderPipeline:\s*\{fileID:\s*0\}\s*$') 'Built-in renderer sentinel failed.'

    $gate = [ordered]@{
        schema = 'vlab.gate.v2'
        planVersion = '1.4'
        planSha256 = Get-Sha $plan
        packageId = 'P1.evidence-bootstrap'
        checkpoint = [ordered]@{ id = $CheckpointId; root = $checkpoint.Replace('\', '/'); aggregateSha256 = $checkpointInventory.aggregateSha256 }
        source = [ordered]@{ aggregateSha256 = $source.aggregateSha256; fileCount = $source.fileCount; sceneSha256 = Get-Sha $projectScene }
        renderer = [ordered]@{ expected = 'Built-in Render Pipeline'; sentinel = 'PASS' }
        dependencies = @($dependencies)
        carryPolicy = 'Plan 1.3 prerequisite gates are immutable historical evidence. P1.atomic success-path blocker is scoped to P1.d2 and is not treated as a completed scene integration claim.'
        requiredEvidence = @(
            [ordered]@{ id = 'AC-001'; class = 'required'; status = 'PASS'; artifact = 'checkpoint-inventory.json' },
            [ordered]@{ id = 'P1-schema'; class = 'required'; status = 'PASS'; artifact = 'gate-manifest.json' }
        )
        blockers = @()
        status = 'PASS_STRUCTURALLY_VERIFIED'
    }
    Write-Json $gate $gatePath
    Write-Output "P1_EVIDENCE_BOOTSTRAP=PASS SOURCE_SHA256=$($source.aggregateSha256)"
    exit 0
}

Assert-True (Test-Path -LiteralPath $gatePath -PathType Leaf) 'Evidence-bootstrap gate is missing.'
$gate = Get-Content -LiteralPath $gatePath -Raw | ConvertFrom-Json
$sourceInventory = Get-Content -LiteralPath (Join-Path $artifact 'source-inventory.json') -Raw | ConvertFrom-Json
$checkpointInventory = Get-Content -LiteralPath (Join-Path $artifact 'checkpoint-inventory.json') -Raw | ConvertFrom-Json
Assert-Inventory $sourceInventory $project
Assert-Inventory $checkpointInventory $checkpoint
Assert-True ($gate.schema -eq 'vlab.gate.v2') 'Unexpected gate schema.'
Assert-True ($gate.planVersion -eq '1.4') 'Unexpected plan version.'
Assert-True ($gate.planSha256 -eq (Get-Sha $plan)) 'Plan changed after gate closure.'
Assert-True ($gate.source.aggregateSha256 -eq $sourceInventory.aggregateSha256) 'Gate/source mismatch.'
Assert-True ($gate.checkpoint.aggregateSha256 -eq $checkpointInventory.aggregateSha256) 'Gate/checkpoint mismatch.'
Assert-True (@($gate.blockers).Count -eq 0) 'Closed gate contains blockers.'
Assert-True (@($gate.requiredEvidence).Count -gt 0) 'Required evidence list is empty.'
foreach ($row in $gate.requiredEvidence) {
    Assert-True ($row.class -eq 'required' -and $row.status -eq 'PASS') "Required evidence row is not PASS: $($row.id)"
}
Assert-True ($gate.status -eq 'PASS_STRUCTURALLY_VERIFIED') 'Gate status is not structurally verified.'
Write-Output "P1_EVIDENCE_BOOTSTRAP_VERIFY=PASS SOURCE_SHA256=$($sourceInventory.aggregateSha256)"
