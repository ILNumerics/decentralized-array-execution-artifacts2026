#!/usr/bin/env bash
set -euo pipefail

# usage: ./run-docker.sh [--rebuild]
# --rebuild: build the image even if it exists already (after changes to sources, Dockerfile or VIPROC_REF)
rebuild=false
for arg in "$@"; do
    case "$arg" in
        --rebuild) rebuild=true ;;
        *) echo "Unknown argument: $arg (usage: $0 [--rebuild])" >&2; exit 1 ;;
    esac
done

imageName="artefact4:latest"
outDir="$(pwd)/result"

mkdir -p "$outDir"

# the image is kept between runs: build only on request or if it does not exist yet
if [ "$rebuild" = true ] || [ -z "$(docker images -q "$imageName")" ]; then
    echo "Building Docker image: $imageName"
    docker build -t "$imageName" .
else
    echo "Using existing Docker image: $imageName (use --rebuild to build it again)"
fi

echo "Running container from: $imageName"
docker run --rm \
    -v "${outDir}:/Artefact4/result" \
    "$imageName"

echo "Done. Output files are in: $outDir"
