#!/usr/bin/env bash
# Migrate Dapr state store keys after app ID rename (no-separator → kebab-case).
# Dapr prefixes state keys with "<appId>||", so renaming an app ID makes existing
# keys unreachable unless the prefix is updated.
#
# Usage: ./scripts/migrate-state-keys.sh [--dry-run] [REDIS_HOST] [REDIS_PORT]
#   --dry-run   Show what would be renamed without making changes
#   REDIS_HOST  defaults to localhost
#   REDIS_PORT  defaults to 6379
#
# The script is idempotent: re-running after a successful migration finds no
# old-prefix keys and exits cleanly with zero renames.

set -euo pipefail

DRY_RUN=false
if [[ "${1:-}" == "--dry-run" ]]; then
  DRY_RUN=true
  shift
fi

REDIS_HOST="${1:-localhost}"
REDIS_PORT="${2:-6379}"

source "$(dirname "${BASH_SOURCE[0]}")/_helpers.sh"

# App ID mappings for stateful services only (MakeLine and Loyalty use Dapr state stores).
declare -A MIGRATIONS=(
  ["makelineservice"]="make-line-service"
  ["loyaltyservice"]="loyalty-service"
)

echo "========================================="
echo "Dapr State Key Migration"
if [[ "$DRY_RUN" == true ]]; then
  echo "  *** DRY RUN — no changes will be made ***"
fi
echo "========================================="
echo "Redis: ${REDIS_HOST}:${REDIS_PORT}"
echo ""

if ! command -v redis-cli >/dev/null 2>&1; then
  print_error "redis-cli is not installed"
  exit 1
fi

if ! redis-cli -h "$REDIS_HOST" -p "$REDIS_PORT" PING >/dev/null 2>&1; then
  print_error "Cannot connect to Redis at ${REDIS_HOST}:${REDIS_PORT}"
  exit 1
fi

print_status "Connected to Redis"
echo ""

TOTAL_MIGRATED=0

for OLD_APP_ID in "${!MIGRATIONS[@]}"; do
  NEW_APP_ID="${MIGRATIONS[$OLD_APP_ID]}"
  PATTERN="${OLD_APP_ID}||*"

  echo "-----------------------------------------"
  echo "Migrating: ${OLD_APP_ID} → ${NEW_APP_ID}"
  echo "Scanning for keys matching: ${PATTERN}"

  KEYS=()
  CURSOR=0
  while true; do
    RESULT=$(redis-cli -h "$REDIS_HOST" -p "$REDIS_PORT" SCAN "$CURSOR" MATCH "$PATTERN" COUNT 100)
    CURSOR=$(echo "$RESULT" | head -1)
    BATCH=$(echo "$RESULT" | tail -n +2)

    while IFS= read -r KEY; do
      [[ -z "$KEY" ]] && continue
      KEYS+=("$KEY")
    done <<< "$BATCH"

    [[ "$CURSOR" == "0" ]] && break
  done

  COUNT=${#KEYS[@]}
  if [[ "$COUNT" -eq 0 ]]; then
    print_warning "No keys found for ${OLD_APP_ID}"
    continue
  fi

  echo "Found ${COUNT} key(s) to migrate"
  MIGRATED=0

  for KEY in "${KEYS[@]}"; do
    # Replace the old app ID prefix with the new one
    NEW_KEY="${NEW_APP_ID}${KEY#"$OLD_APP_ID"}"
    if [[ "$DRY_RUN" == true ]]; then
      echo "  Would rename: ${KEY} → ${NEW_KEY}"
      MIGRATED=$((MIGRATED + 1))
    elif redis-cli -h "$REDIS_HOST" -p "$REDIS_PORT" RENAME "$KEY" "$NEW_KEY" >/dev/null 2>&1; then
      MIGRATED=$((MIGRATED + 1))
    else
      print_error "Failed to rename: ${KEY} → ${NEW_KEY}"
    fi
  done

  if [[ "$DRY_RUN" == true ]]; then
    print_status "Would migrate ${MIGRATED}/${COUNT} keys for ${OLD_APP_ID} → ${NEW_APP_ID}"
  else
    print_status "Migrated ${MIGRATED}/${COUNT} keys for ${OLD_APP_ID} → ${NEW_APP_ID}"
  fi
  TOTAL_MIGRATED=$((TOTAL_MIGRATED + MIGRATED))
done

echo ""
echo "========================================="
print_status "Migration complete. Total keys migrated: ${TOTAL_MIGRATED}"
echo "========================================="
