param(
    [ValidateSet('EditMode','PlayMode')][string]$Platform='EditMode',
    [string]$Filter='',
    [string]$Package='P2',
    [string]$Label='tests'
)
$ErrorActionPreference='Stop'
$project=Split-Path $PSScriptRoot -Parent
$stamp=[DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ')
$artifact=Join-Path $project "Artifacts/VLABUpgrade/plan-1.4/$Package/$stamp-$Label-$Platform"
New-Item -ItemType Directory -Path $artifact | Out-Null
$arguments=@('-projectPath',$project,'-runTests','-testPlatform',$Platform,'-testResults',"$artifact/results.xml",'-logFile',"$artifact/unity.log")
if($Filter){$arguments+=@('-testFilter',$Filter)}
$process=Start-Process -FilePath 'D:\unity download\6000.5.6f1\Editor\Unity.exe' -ArgumentList $arguments -WindowStyle Hidden -PassThru -Wait
if(Test-Path "$artifact/results.xml") {
    [xml]$results=Get-Content "$artifact/results.xml"
    $run=$results.'test-run'
    "ARTIFACT=$artifact EXIT=$($process.ExitCode) TOTAL=$($run.total) PASSED=$($run.passed) FAILED=$($run.failed)"
    $results.SelectNodes('//test-case[@result="Failed"]/failure') | ForEach-Object {$_.InnerText}
    if ([int]$run.total -le 0 -or $run.result -ne 'Passed') { exit 1 }
} else { "ARTIFACT=$artifact EXIT=$($process.ExitCode) NO_XML"; exit 2 }
exit $process.ExitCode
