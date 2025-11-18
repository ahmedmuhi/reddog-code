#!/usr/bin/env bash
set -euo pipefail
ROOT=$(git rev-parse --show-toplevel 2>/dev/null || echo ".")
cd "$ROOT"
export VIRTUALCUSTOMERS__DisableDaprCalls=true
export VIRTUALCUSTOMERS__NumOrders=1
export VIRTUALCUSTOMERS__MinSecondsBetweenOrders=0
export VIRTUALCUSTOMERS__MaxSecondsBetweenOrders=0
export VIRTUALCUSTOMERS__MinSecondsToPlaceOrder=0
export VIRTUALCUSTOMERS__MaxSecondsToPlaceOrder=0
export VIRTUALCUSTOMERS__MaxUniqueItemsPerOrder=1
export VIRTUALCUSTOMERS__MaxItemQuantity=1

dotnet run --project RedDog.VirtualCustomers/RedDog.VirtualCustomers.csproj --configuration Release --no-build >/tmp/virtualcustomers-smoke.log 2>&1
STATUS=$?
if [ $STATUS -eq 0 ]; then
  echo "VirtualCustomers smoke test completed. Log: /tmp/virtualcustomers-smoke.log"
else
  echo "VirtualCustomers smoke test failed. Inspect /tmp/virtualcustomers-smoke.log" >&2
fi
exit $STATUS
