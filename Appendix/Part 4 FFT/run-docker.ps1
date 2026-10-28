param(
    # rebuild the image even if it exists already (after changes to sources, Dockerfile or VIPROC_REF)
    [switch]$Rebuild
)

$ErrorActionPreference = "Stop"

$imageName = "artefact4:latest"
$outDir = Join-Path (Get-Location).Path "result"

New-Item -ItemType Directory -Force -Path $outDir | Out-Null

# the image is kept between runs: build only on request or if it does not exist yet
if ($Rebuild -or -not (docker images -q $imageName)) {
    Write-Host "Building Docker image: $imageName"
    docker build -t $imageName .
    if ($LASTEXITCODE -ne 0) { throw "docker build failed" }
}
else {
    Write-Host "Using existing Docker image: $imageName (use -Rebuild to build it again)"
}

Write-Host "Running container from: $imageName"
docker run --rm `
    -v "${outDir}:/Artefact4/result" `
    $imageName
if ($LASTEXITCODE -ne 0) { throw "docker run failed" }

Write-Host "Done. Output files are in: $outDir"
