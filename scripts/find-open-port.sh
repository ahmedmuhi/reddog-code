#!/usr/bin/env bash
# Finds the first available TCP port from the arguments.
# Usage: ./scripts/find-open-port.sh 5200 15200 25200

set -euo pipefail

if [ $# -lt 1 ]; then
  echo "Usage: $0 <port> [additional ports...]" >&2
  exit 1
fi

is_port_free() {
  local PORT="$1"
  python3 - "$PORT" <<'PY'
import socket, sys
port = int(sys.argv[1])
with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as sock:
    sock.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    try:
        sock.bind(("127.0.0.1", port))
    except OSError:
        sys.exit(1)
sys.exit(0)
PY
}

for candidate in "$@"; do
  if is_port_free "$candidate"; then
    echo "$candidate"
    exit 0
  fi
done

echo "No available ports from: $*" >&2
exit 1
