# Known Issues

## Dapr 1.16.x Sidecar Readiness Probe Port Mismatch

**Issue:** Dapr sidecar injector (versions 1.16.0 - 1.16.2) configures sidecar readiness probes to check port `3501` instead of the correct port `3500`.

**Impact:**
- Dapr sidecars show as `not ready` (pods show `1/2` Ready instead of `2/2`)
- Application containers are functional and ready
- Dapr functionality works correctly (the HTTP API is listening on port 3500)
- This is purely a probe configuration bug

**Evidence:**
```bash
# Dapr HTTP server is running on port 3500
$ kubectl logs <pod> -c daprd | grep "HTTP server is running"
time="..." level=info msg="HTTP server is running on port 3500"

# But the readiness probe is checking port 3501
$ kubectl get pod <pod> -o json | jq '.spec.containers[] | select(.name=="daprd") | .readinessProbe'
{
  "httpGet": {
    "path": "/v1.0/healthz",
    "port": 3501  # <-- WRONG PORT
  }
}
```

**Status:**
- Reported: 2025-11-11
- Affects: Dapr 1.16.0, 1.16.1, 1.16.2
- Upstream: No Helm value exists to override this port
- Workaround: Accept that sidecars show as `1/2` Ready; functionality is unaffected

**Resolution Plan:**
- Monitor Dapr releases for fix (likely 1.16.3 or 1.17.0)
- When fixed, update `scripts/setup-local-dev.sh` to use the fixed version
- Re-run pod deployments to get corrected sidecars

**Workaround for Production:**
If this affects production deployments:
1. The applications work fine despite the probe issue
2. Services can still communicate through Dapr
3. Consider implementing custom pod disruption budgets if relying on readiness

**References:**
- Dapr HTTP health endpoint docs: https://docs.dapr.io/operations/resiliency/health-checks/sidecar-health/
- This affects local kind cluster deployments in this repository
