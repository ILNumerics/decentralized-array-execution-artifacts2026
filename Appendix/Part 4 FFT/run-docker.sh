#!/usr/bin/env bash
set -euo pipefail

timestamp="$(date +"%Y-%m-%d_%H%M%S")"
imageName="artefact4:${timestamp}"
containerName="artefact4-${timestamp}"
outDir="$(pwd)/result"

mkdir -p "$outDir"

cleanup() {
    echo "Cleaning up container and image..."

    docker rm -f "$containerName" >/dev/null 2>&1 || true
    docker rmi -f "$imageName" >/dev/null 2>&1 || true

    echo "Done. Output files are in: $outDir"
}

trap cleanup EXIT

echo "Building Docker image: $imageName"
docker build -t "$imageName" .

echo "Running container: $containerName"
docker run --name "$containerName" \
    -v "${outDir}:/Artefact4/result" \
    "$imageName"