#!/bin/bash
set -euo pipefail

LOG_DIR="/home/langdon/hvac-controller/HvacController/logs"
ARCHIVE_DIR="$LOG_DIR/archive"

mkdir -p "$ARCHIVE_DIR"

PERIOD="$(date -d 'yesterday' +'%Y-%m')"

for file in "$LOG_DIR"/*.csv; do
    [ -f "$file" ] || continue

    base="$(basename "$file" .csv)"
    target="$ARCHIVE_DIR/${base}-${PERIOD}.csv"

    if [ -e "$target" ]; then
        target="$ARCHIVE_DIR/${base}-${PERIOD}-$(date +'%Y%m%d%H%M%S').csv"
    fi

    mv "$file" "$target"
done
