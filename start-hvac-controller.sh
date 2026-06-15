#!/usr/bin/env bash
set -e

REPO_DIR="/home/langdon/hvac-controller"
PROJECT_DIR="/home/langdon/hvac-controller/HvacController"
PUBLISH_DIR="/home/langdon/hvac-controller/publish"

cd "$REPO_DIR"

git pull --ff-only

cd "$PROJECT_DIR"

/home/langdon/.dotnet/dotnet publish -c Release -o "$PUBLISH_DIR"

exec /home/langdon/.dotnet/dotnet "$PUBLISH_DIR/HvacController.dll"
