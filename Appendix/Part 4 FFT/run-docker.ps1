$ErrorActionPreference = "Stop"

$timestamp = Get-Date -Format "yyyy-MM-dd_HHmmss"
$imageName = "artefact4:$timestamp"
$containerName = "artefact4-$timestamp"
$outDir = Join-Path (Get-Location).Path "result"

New-Item -ItemType Directory -Force -Path $outDir | Out-Null

try {
    Write-Host "Building Docker image: $imageName"
    docker build -t $imageName .

    Write-Host "Running container: $containerName"
    docker run --name $containerName `
        -v "${outDir}:/Artefact4/result" `
        $imageName
}
finally {
    Write-Host "Cleaning up container and image..."

    docker rm -f $containerName 2>$null
    docker rmi -f $imageName 2>$null

    Write-Host "Done. Output files are in: $outDir"
}