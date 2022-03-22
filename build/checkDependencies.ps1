# Copyright (c) 2022 Matthias Wolf, Mawosoft.

<#
.SYNOPSIS
    Check solution/project dependencies for outdated/vulnerable/deprecated

.DESCRIPTION
    Runs all 'dotnet list package' reports and discovers new dependency problems by comparing
    the results to artifacts from previous workflow runs.The script will download the previous
    artifact, but the caller is responsible for uploading the new one.
    Creates an issue, if new breaking changes have been found.

.OUTPUTS
    None. Sets workflow step outputs 'ArtifactName' and 'ArtifactPath' via workflow commands
    issued by Write-Host.
#>

#Requires -Version 7

using namespace System
using namespace System.IO
using namespace System.Collections.Generic
using module ./ListPackageHelper.psm1
using namespace ListPackageHelper

[CmdletBinding()]
param (
    # Project or solution files to process. Defaults to the solution or project
    # in the current directory
    [Parameter(Position = 0)]
    [Alias('p')]
    [string[]]$Projects,

    # 'list package' options to run for each project/solution
    [ValidateNotNullOrEmpty()]
    [Alias('o')]
    [string[][]]$OptionsMatrix = (
        ('--outdated', '--include-transitive'),
        ('--vulnerable', '--include-transitive'),
        ('--deprecated', '--include-transitive')
    ),

    [Parameter(Mandatory)]
    [Alias('Token', 't')]
    [securestring]$GitHubToken,

    [ValidateNotNullOrEmpty()]
    [Alias('Artifact', 'a')]
    [string]$ArtifactName = 'DependencyCheck',

    [Alias('Labels', 'l')]
    [string[]]$IssueLabels = @('dependencies')
)

Set-StrictMode -Version 3.0
$ErrorActionPreference = 'Stop'

Import-Module "$PSScriptRoot/GitHubHelper.psm1" -Force

# GitHub data about current workflow run and repository
[int]$runId = $env:GITHUB_RUN_ID
[int]$runNumber = $env:GITHUB_RUN_NUMBER
[string]$ownerRepo = $env:GITHUB_REPOSITORY
if (-not $runId -or -not $runNumber -or -not $ownerRepo) {
    throw "GitHub environment variables are not defined."
}

[ListPackageResult]$previousResult = $null
[ListPackageResult]$result = $null

# Download artifact from previous workflow run
[string]$artifactDirectory = Join-Path ([Path]::GetTempPath()) ([Path]::GetRandomFileName())
[string]$artifactFile = Join-Path $artifactDirectory 'LastResult.json'
$null = [Directory]::CreateDirectory($artifactDirectory)
if ($runNumber -gt 1) {
    [int]$workflowId = Get-WorkflowId $ownerRepo $runId -Token $GitHubToken
    $artifacts = Find-ArtifactsFromPreviousRun $ownerRepo $ArtifactName -WorkflowId $workflowId `
        -MaxRunNumber ($runNumber - 1) -Token $GitHubToken
    if ($artifacts) {
        Write-Host "Found artifact '$ArtifactName' from workflow run #$(
            $artifacts.workflow_run.run_number) on $($artifacts.workflow_run.created_at) UTC."
        Expand-Artifact $artifacts.artifacts[0].archive_download_url $artifactDirectory -Token $GitHubToken
        $previousResult = [ListPackageResult]::CreateFromJson((Get-Content $artifactFile -Raw))
    }
}

Write-Host '::group::Raw tool output'
try {
    $result = Invoke-ListPackage -p $Projects -o $OptionsMatrix -InformationAction 'Continue'
}
finally {
    Write-Host '::endgroup::'
}

[MergedPackageRef[]]$toplevel = [MergedPackageRef]::Create(($result.Packages.Values | Where-Object RefType -EQ TopLevel))
[MergedPackageRef[]]$transitive = [MergedPackageRef]::Create(($result.Packages.Values | Where-Object RefType -EQ Transitive))
if ($toplevel -or $transitive) {
    Write-Host '::group::All results'
    if ($toplevel) { [MergedPackageRef]::FormatTable($toplevel, 'Top-level Packages', $true) }
    if ($transitive) { [MergedPackageRef]::FormatTable($transitive, 'Transitive Packages', $true) }
    Write-Host '::endgroup::'
}
else {
    Write-Host "All results: No packages matching the given criteria have been found."
}

[MergedPackageRef[]]$diffToplevel = $null
[MergedPackageRef[]]$diffTransitive = $null
if ($previousResult) {
    [ListPackageComparison]$diff = [ListPackageComparison]::new($previousResult, $result)
    [HashSet[string]]$diffKeys = $diff.RightOnly
    $diffKeys.UnionWith($diff.Changed)
    [List[ParsedPackageRef]]$diffPackages = [List[ParsedPackageRef]]::new($diffKeys.Count)
    foreach($key in $diffKeys) {
        $diffPackages.Add($result.Packages[$key])
    }
    [MergedPackageRef[]]$diffToplevel = [MergedPackageRef]::Create(($diffPackages | Where-Object RefType -EQ TopLevel))
    [MergedPackageRef[]]$diffTransitive = [MergedPackageRef]::Create(($diffPackages | Where-Object RefType -EQ Transitive))
    if ($diffToplevel -or $diffTransitive) {
        Write-Host '::group::New results'
        if ($diffToplevel) { [MergedPackageRef]::FormatTable($diffToplevel, 'Top-level Packages', $true) }
        if ($diffTransitive) { [MergedPackageRef]::FormatTable($diffTransitive, 'Transitive Packages', $true) }
        Write-Host '::endgroup::'
    }
    else {
        Write-Host "New results: No new packages matching the given criteria have been found."
    }
}

if ($diffToplevel -or $diffTransitive -or (-not $previousResult -and ($toplevel -or $transitive))) {
    $title = "New dependency problems"
    [System.Text.StringBuilder]$body = [System.Text.StringBuilder]::new()
    if ($diffToplevel -or $diffTransitive) {
        $null = $body.AppendLine('### New Dependency Problems')
        if ($diffToplevel) {
            $null = $body.AppendLine('<details><summary>Top-level Packages</summary>').AppendLine()
            $null = $body.AppendLine([MergedPackageRef]::FormatMarkdownHtmlTable($diffToplevel, 'Package', 1, $true))
            $null = $body.AppendLine('</details>')
        }
        if ($diffTransitive) {
            $null = $body.AppendLine('<details><summary>Transitive Packages</summary>').AppendLine()
            $null = $body.AppendLine([MergedPackageRef]::FormatMarkdownHtmlTable($diffTransitive, 'Package', 1, $true))
            $null = $body.AppendLine('</details>')
        }
    }
    if ($toplevel -or $transitive) {
        $null = $body.AppendLine('### All Dependency Problems')
        if ($toplevel) {
            $null = $body.AppendLine('<details><summary>Top-level Packages</summary>').AppendLine()
            $null = $body.AppendLine([MergedPackageRef]::FormatMarkdownHtmlTable($toplevel, 'Package', 1, $true))
            $null = $body.AppendLine('</details>')
        }
        if ($transitive) {
            $null = $body.AppendLine('<details><summary>Transitive Packages</summary>').AppendLine()
            $null = $body.AppendLine([MergedPackageRef]::FormatMarkdownHtmlTable($transitive, 'Package', 1, $true))
            $null = $body.AppendLine('</details>')
        }
    }

    [hashtable]$params = @{
        title = $title
        body  = $body.ToString()
    }
    if ($IssueLabels) { $params.labels = $IssueLabels }
    $issue = $params | ConvertTo-Json | Invoke-RestMethod -Uri "https://api.github.com/repos/$ownerRepo/issues" `
        -Method Post -Authentication Bearer -Token $token
    Write-Host "Created issue #$($issue.number)"
}

$result.ToJson($true) | Set-Content $artifactFile

Write-Host "::set-output name=ArtifactName::$ArtifactName"
Write-Host "::set-output name=ArtifactPath::$artifactFile"
