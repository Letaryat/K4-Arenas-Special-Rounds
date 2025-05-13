# made with chatgpt 
# Ścieżka głównego folderu docelowego
$destinationFolderRoot = "..\Compiled\WithoutPreferences"

# Utwórz główny folder docelowy, jeśli nie istnieje
if (!(Test-Path $destinationFolderRoot)) {
    New-Item -ItemType Directory -Path $destinationFolderRoot | Out-Null
}

# Szukaj i buduj wszystkie projekty .csproj
Get-ChildItem -Recurse -Filter *.csproj | ForEach-Object {
    $csprojPath = $_.FullName
    $projectDir = Split-Path $csprojPath -Parent

    Write-Host "`n⏳ Buduję projekt: $csprojPath"
    dotnet build $csprojPath -c Release | Out-Null

    # Nazwa projektu i potencjalny plik DLL
    $projectName = [System.IO.Path]::GetFileNameWithoutExtension($csprojPath)
    $dllPath = Join-Path $projectDir "bin\Release\net8.0\$projectName.dll"

    if (Test-Path $dllPath) {
        # Stwórz osobny folder dla każdego DLL
        $destinationFolder = Join-Path $destinationFolderRoot $projectName
        if (!(Test-Path $destinationFolder)) {
            New-Item -ItemType Directory -Path $destinationFolder | Out-Null
        }

        $destinationPath = Join-Path $destinationFolder "$projectName.dll"
        Copy-Item -Path $dllPath -Destination $destinationPath -Force
        Write-Host "✅ Skompilowano i skopiowano: $projectName.dll do folderu $projectName"
    }
    else {
        Write-Host "⚠️ Nie znaleziono DLL dla: $projectName"
    }
}
