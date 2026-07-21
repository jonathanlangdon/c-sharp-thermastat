#!/bin/bash
set -euo pipefail

STATE_FILE="/home/langdon/hvac-controller/HvacController/state/thermostat-state.json"
LOG_DIR="/home/langdon/hvac-controller/HvacController/logs"
LOG_FILE="$LOG_DIR/daily-runtime.csv"

mkdir -p "$LOG_DIR"

if [ ! -f "$LOG_FILE" ]; then
    echo "year,month,day,dayOfWeek,coolHoursToday,heatHoursToday" > "$LOG_FILE"
fi

YEAR="$(date +'%Y')"
MONTH="$(date +'%m')"
DAY="$(date +'%d')"
DAY_OF_WEEK="$(date +'%A')"

COOL_HOURS="$(jq -r '(.coolHoursToday // 0) | tonumber | .*100 | round / 100' "$STATE_FILE")"
HEAT_HOURS="$(jq -r '(.heatHoursToday // 0) | tonumber | .*100 | round / 100' "$STATE_FILE")"

echo "$YEAR,$MONTH,$DAY,$DAY_OF_WEEK,$COOL_HOURS,$HEAT_HOURS" >> "$LOG_FILE"
