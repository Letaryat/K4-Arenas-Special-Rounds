# made with chatgpt 
$destinationFolderRoot = "..\Compiled\WithoutPreferences"

if (!(Test-Path $destinationFolderRoot)) {
    New-Item -ItemType Directory -Path $destinationFolderRoot | Out-Null
}

Get-ChildItem -Recurse -Filter *.csproj | ForEach-Object {
    $csprojPath = $_.FullName
    $projectDir = Split-Path $csprojPath -Parent

    Write-Host "`n⏳ Building a project: $csprojPath"
    dotnet build $csprojPath -c Release | Out-Null

    $projectName = [System.IO.Path]::GetFileNameWithoutExtension($csprojPath)
    $dllPath = Join-Path $projectDir "bin\Release\net8.0\$projectName.dll"

    if (Test-Path $dllPath) {
        $destinationFolder = Join-Path $destinationFolderRoot $projectName
        if (!(Test-Path $destinationFolder)) {
            New-Item -ItemType Directory -Path $destinationFolder | Out-Null
        }

        $destinationPath = Join-Path $destinationFolder "$projectName.dll"
        Copy-Item -Path $dllPath -Destination $destinationPath -Force
        Write-Host "✅ Compiled and copied: $projectName.dll to folder called: $projectName"
    }
    else {
        Write-Host "⚠️ Could not find a DLL called: $projectName"
    }
}
