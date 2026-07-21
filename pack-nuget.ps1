param(
    [ValidateSet("Debug", "Release")]
    [string] $Configuration = "Release",

    [ValidateRange(1, 64)]
    [int] $ThrottleLimit = 4
)

$ErrorActionPreference = "Stop"


# ------------------------------------------------------------
# 基础路径
# ------------------------------------------------------------

$repositoryRoot = $PSScriptRoot

$packageOutput = Join-Path `
    $repositoryRoot `
    "artifacts\nuget"


New-Item `
    -ItemType Directory `
    -Path $packageOutput `
    -Force | Out-Null



# ------------------------------------------------------------
# 查找 Solution
# ------------------------------------------------------------

$solution = Get-ChildItem `
    -Path $repositoryRoot `
    -Filter "*.sln" `
    -File |
        Select-Object -First 1


if ($null -eq $solution)
{
    throw "No solution file was found."
}



# ------------------------------------------------------------
# 查找可打包项目
# ------------------------------------------------------------

$projects = Get-ChildItem `
    -Path $repositoryRoot `
    -Recurse `
    -Filter "*.csproj" |
        Where-Object {
            $_.FullName -notmatch "[\\/](test|tests)[\\/]"
        } |
        Where-Object {

            [xml]$xml = Get-Content -Raw $_.FullName

            $node = $xml.SelectSingleNode(
                    "//*[local-name()='IsPackable']"
            )

            $null -eq $node -or
                    $node.InnerText.Trim() -ne "false"
        }



if ($projects.Count -eq 0)
{
    throw "No packable projects found."
}



# ------------------------------------------------------------
# Restore
# ------------------------------------------------------------

Write-Host "Restoring..."

dotnet restore $solution.FullName

if ($LASTEXITCODE -ne 0)
{
    throw "Restore failed."
}



# ------------------------------------------------------------
# Build
# ------------------------------------------------------------

Write-Host "Building..."

dotnet build `
    $solution.FullName `
    --configuration $Configuration `
    --no-restore `
    -p:GeneratePackageOnBuild=false


if ($LASTEXITCODE -ne 0)
{
    throw "Build failed."
}



# ------------------------------------------------------------
# RunspacePool
# ------------------------------------------------------------

Add-Type `
    -AssemblyName System.Management.Automation



$pool = [runspacefactory]::CreateRunspacePool(
        1,
        $ThrottleLimit
)

$pool.Open()



# ------------------------------------------------------------
# 状态表
# ------------------------------------------------------------

$status = @{}

foreach($project in $projects)
{
    $status[$project.Name] = "Waiting"
}



function Show-Status
{
    Clear-Host

    Write-Host "NuGet Packing"
    Write-Host "=============="
    Write-Host ""

    foreach($key in $status.Keys)
    {
        $value = $status[$key]

        switch($value)
        {
            "Done" {
                Write-Host "[OK]   $key"
            }

            "Failed" {
                Write-Host "[FAIL] $key"
            }

            "Running" {
                Write-Host "[...]  $key"
            }

            default {
                Write-Host "[    ] $key"
            }
        }
    }

    Write-Host ""
}



# ------------------------------------------------------------
# 创建任务
# ------------------------------------------------------------

$tasks = New-Object System.Collections.ArrayList



foreach($project in $projects)
{
    $ps = [powershell]::Create()

    $ps.RunspacePool = $pool


    [void]$ps.AddScript({

        param(
            $projectPath,
            $configuration,
            $output
        )


        dotnet pack `
            $projectPath `
            --configuration $configuration `
            --output $output `
            --no-build `
            --no-restore


        if($LASTEXITCODE -ne 0)
        {
            throw "Pack failed"
        }


        return "OK"

    })


    [void]$ps.AddArgument(
            $project.FullName
    )

    [void]$ps.AddArgument(
            $configuration
    )

    [void]$ps.AddArgument(
            $packageOutput
    )



    $handle = $ps.BeginInvoke()



    $tasks.Add(
            @{
                Project = $project.Name
                PowerShell = $ps
                Handle = $handle
            }
    )


    $status[$project.Name] = "Running"


    Show-Status
}



# ------------------------------------------------------------
# 等待任务
# ------------------------------------------------------------

while($tasks.Count -gt 0)
{
    foreach($task in @($tasks))
    {
        if($task.Handle.IsCompleted)
        {
            try
            {
                $task.PowerShell.EndInvoke(
                        $task.Handle
                )

                $status[$task.Project] = "Done"
            }
            catch
            {
                $status[$task.Project] = "Failed"

                Show-Status

                throw
            }
            finally
            {
                $task.PowerShell.Dispose()

                $tasks.Remove($task)
            }


            Show-Status
        }
    }


    Start-Sleep -Milliseconds 200
}



# ------------------------------------------------------------
# 清理
# ------------------------------------------------------------

$pool.Close()
$pool.Dispose()



Write-Host ""
Write-Host "NuGet packages written to:"
Write-Host $packageOutput
