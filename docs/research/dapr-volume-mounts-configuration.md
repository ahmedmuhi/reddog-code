# Dapr Volume Mounts Configuration Research

**Date:** 2025-11-11
**Context:** Troubleshooting read-only volume mount issues with Dapr localstorage binding
**Dapr Version:** 1.16.2
**Environment:** Kubernetes (kind cluster)

---

## Problem Statement

When using the Dapr localstorage binding component in Kubernetes, volumes mounted to the Dapr sidecar were read-only by default, causing errors:

```
unable to create directory specified by 'rootPath' /tmp/receipts: mkdir /tmp/receipts: read-only file system
```

Initial attempts to specify read-write mode with annotations like `dapr.io/volume-mounts: "receipts:/tmp/receipts:rw"` failed, as Dapr did not mount the volume at all.

---

## Solution Summary

**The correct approach requires THREE key components:**

1. **Use the `dapr.io/volume-mounts-rw` annotation** (not `dapr.io/volume-mounts` with `:rw` suffix)
2. **Set `fsGroup: 65532` in the Pod's securityContext** to match Dapr's user ID
3. **Ensure the volume exists in the Pod spec** (e.g., emptyDir, PVC, hostPath)

---

## Detailed Findings

### 1. Dapr Volume Mount Annotations

Dapr provides **two separate annotations** for mounting volumes to the sidecar:

| Annotation | Purpose | Mode |
|------------|---------|------|
| `dapr.io/volume-mounts` | Mount volumes in read-only mode | Read-only |
| `dapr.io/volume-mounts-rw` | Mount volumes in read-write mode | Read-write |

**Syntax:** Both annotations use comma-separated pairs of `volume-name:path/in/container`

**Example:**
```yaml
annotations:
  dapr.io/volume-mounts: "config-volume:/mnt/config,data-volume:/mnt/data"
  dapr.io/volume-mounts-rw: "receipts:/tmp/receipts,logs:/var/log"
```

**Important:** Do NOT use `:rw` or `:ro` suffixes with the volume paths. The annotation type determines the mount mode.

**Source:** [Dapr Documentation - Kubernetes Volume Mounts](https://docs.dapr.io/operations/hosting/kubernetes/kubernetes-volume-mounts/)

---

### 2. Dapr User ID and Permissions

The Dapr sidecar (daprd) runs as a non-root user with specific UID requirements:

- **User ID (UID):** 65532
- **User Name:** "nonroot" (in Mariner-based images)
- **Container Type:** Distroless or Mariner-based

**Critical Requirement:** Files and folders inside mounted volumes MUST be readable/writable by UID 65532.

**Source:** [Dapr Docs Issue #2964](https://github.com/dapr/docs/issues/2964)

---

### 3. Kubernetes fsGroup for Volume Permissions

To make emptyDir volumes (and other volume types) writable by the Dapr sidecar, you must set `fsGroup` in the Pod's `securityContext`:

```yaml
spec:
  securityContext:
    fsGroup: 65532  # Must match Dapr's UID
  volumes:
    - name: receipts
      emptyDir: {}
```

**What fsGroup does:**
- Associates a group ID (GID) with all containers in the Pod
- Sets ownership of the volume and all files created in it to the specified GID
- Allows containers running as UID 65532 to write to the volume

**Without fsGroup:** The emptyDir volume is owned by root (UID 0), causing permission denied errors when Dapr (UID 65532) tries to write.

**Sources:**
- [Kubernetes SecurityContext Documentation](https://kubernetes.io/docs/tasks/configure-pod-container/security-context/)
- [Stack Overflow - fsGroup and emptyDir permissions](https://stackoverflow.com/questions/48023652/kubernetes-security-context-fsgroup-field-and-default-users-group-id-running)

---

### 4. Recommended Mount Paths

Dapr documentation recommends specific mount paths for different use cases:

| Path | Purpose | Example Use Case |
|------|---------|------------------|
| `/mnt/*` | Persistent data that the sidecar can read/write | Configuration files, shared data stores |
| `/tmp/*` | Temporary data like scratch disks | Temporary files, receipts, logs |

**Note:** These are recommendations, not requirements. However, using standard paths improves clarity and security.

---

## Complete Working Example

Here's a full Deployment manifest showing the correct configuration for read-write volume mounts with Dapr:

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: receipt-generation-service
  namespace: reddog
spec:
  replicas: 1
  selector:
    matchLabels:
      app: receipt-generation-service
  template:
    metadata:
      labels:
        app: receipt-generation-service
      annotations:
        dapr.io/enabled: "true"
        dapr.io/app-id: "receipt-generation-service"
        dapr.io/app-port: "5300"
        dapr.io/volume-mounts-rw: "receipts:/tmp/receipts"
    spec:
      securityContext:
        fsGroup: 65532  # Critical for write permissions
      containers:
      - name: receipt-generation-service
        image: receipt-generation-service:latest
        ports:
        - containerPort: 5300
      volumes:
      - name: receipts
        emptyDir: {}
```

**Key elements:**
1. `dapr.io/volume-mounts-rw: "receipts:/tmp/receipts"` - Read-write mount annotation
2. `securityContext.fsGroup: 65532` - Sets volume ownership to Dapr's GID
3. `volumes: - name: receipts` - Volume definition matching the annotation

---

## Localstorage Binding Component Configuration

After setting up the volume mount correctly, configure the Dapr component:

```yaml
apiVersion: dapr.io/v1alpha1
kind: Component
metadata:
  name: reddog.binding.receipt
  namespace: reddog
spec:
  type: bindings.localstorage
  version: v1
  metadata:
  - name: rootPath
    value: "/tmp/receipts"  # Must match the volume mount path
```

**Important:** The `rootPath` must exactly match the path specified in the `dapr.io/volume-mounts-rw` annotation.

---

## Testing the Configuration

1. **Deploy the service** with the annotations and securityContext
2. **Check the Dapr sidecar logs:**
   ```bash
   kubectl logs -n reddog <pod-name> -c daprd
   ```
   Look for component initialization messages:
   ```
   INFO[0001] component loaded: reddog.binding.receipt (bindings.localstorage/v1)
   ```

3. **Verify volume permissions inside the sidecar:**
   ```bash
   kubectl exec -n reddog <pod-name> -c daprd -- ls -la /tmp/receipts
   ```
   Expected output:
   ```
   drwxrwsrwx 2 65532 65532 4096 Nov 11 12:00 .
   ```

4. **Test the binding by creating a file:**
   ```bash
   curl -X POST http://localhost:3500/v1.0/bindings/reddog.binding.receipt \
     -H "Content-Type: application/json" \
     -d '{
       "operation": "create",
       "metadata": {
         "fileName": "test-receipt.txt"
       },
       "data": "Test receipt content"
     }'
   ```

---

## Known Issues and Limitations

### 1. Localstorage Binding Not Recommended for Production

**From Stack Overflow ([Source](https://stackoverflow.com/questions/78401726/use-dapr-bindings-localstorage-inside-k8s)):**

> "I ended up switching to Azurite for local environments and Azure on AKS instead of using the localstorage binding."

**Reasons:**
- Read-only filesystem restrictions in secure Kubernetes environments
- Lack of persistence across Pod restarts (with emptyDir)
- Limited scalability (hostPath ties Pods to specific nodes)
- Not suitable for multi-replica deployments

### 2. Alternative Production Solutions

For production deployments, use cloud-native storage bindings:

| Cloud Provider | Binding Type | Component Type |
|----------------|--------------|----------------|
| Azure | Azure Blob Storage | `bindings.azure.blobstorage` |
| AWS | AWS S3 | `bindings.aws.s3` |
| Multi-cloud | MinIO (S3-compatible) | `bindings.aws.s3` |
| Local Dev | Azurite (Azure emulator) | `bindings.azure.blobstorage` |

**Migration Path:** Dapr bindings share common operations (create, get, delete, list), so you can switch storage backends by changing only the component configuration, not application code.

### 3. SecurityContext Injection Limitations

**From GitHub Issue ([Source](https://github.com/dapr/dapr/issues/5916)):**

Dapr's sidecar injector does NOT automatically configure Pod-level `securityContext` properties like `fsGroup`. You must manually add these to your Deployment manifests.

**Workarounds:**
- Manually configure `securityContext` in every Deployment
- Use Helm chart values to set cluster-wide defaults (limited properties)
- Use manual sidecar injection for full control (requires configuring CLI options and TLS)

---

## Best Practices

1. **Always use `dapr.io/volume-mounts-rw` for writable volumes**
   - Do NOT use `:rw` suffixes with the path
   - Do NOT use `dapr.io/volume-mounts` and expect write access

2. **Always set `fsGroup: 65532` for volumes requiring Dapr write access**
   - This is required for emptyDir, hostPath, and many PVC types
   - Without it, volumes are owned by root (UID 0)

3. **Use localstorage binding only for development/testing**
   - Switch to cloud storage bindings (Azure Blob, S3) for production
   - Use Azurite or MinIO for local development that mimics production

4. **Verify permissions after deployment**
   - Check Dapr sidecar logs for component initialization errors
   - Exec into the sidecar container to verify directory ownership
   - Test the binding with a simple create operation

5. **Use recommended mount paths**
   - `/tmp/*` for temporary/ephemeral data
   - `/mnt/*` for persistent shared data

---

## References

### Official Dapr Documentation
- [How-to: Mount Pod volumes to the Dapr sidecar](https://docs.dapr.io/operations/hosting/kubernetes/kubernetes-volume-mounts/)
- [Local Storage binding spec](https://docs.dapr.io/reference/components-reference/supported-bindings/localstorage/)
- [Azure Blob Storage binding spec](https://docs.dapr.io/reference/components-reference/supported-bindings/blobstorage/)
- [AWS S3 binding spec](https://docs.dapr.io/reference/components-reference/supported-bindings/s3/)

### GitHub Issues and Pull Requests
- [dapr/docs#2964 - Document UNIX permissions for mounted volumes](https://github.com/dapr/docs/issues/2964)
- [dapr/docs#3118 - PR: Add volume mount permission documentation](https://github.com/dapr/docs/pull/3118)
- [dapr/dapr#4662 - VolumeMounts support for Side Car Daprd](https://github.com/dapr/dapr/issues/4662)
- [dapr/dapr#5916 - How to have injector inject correct securityContext into daprd sidecar](https://github.com/dapr/dapr/issues/5916)

### Stack Overflow
- [Use Dapr bindings.localstorage inside k8s](https://stackoverflow.com/questions/78401726/use-dapr-bindings-localstorage-inside-k8s)
- [Kubernetes, security context, fsGroup field and default user's group ID](https://stackoverflow.com/questions/48023652/kubernetes-security-context-fsgroup-field-and-default-users-group-id-running)

### Kubernetes Documentation
- [Configure a Security Context for a Pod or Container](https://kubernetes.io/docs/tasks/configure-pod-container/security-context/)
- [Volumes](https://kubernetes.io/docs/concepts/storage/volumes/)

---

## Conclusion

The read-only volume mount issue with Dapr localstorage binding was caused by two factors:

1. **Incorrect annotation:** Using `dapr.io/volume-mounts` instead of `dapr.io/volume-mounts-rw`
2. **Missing fsGroup:** Not setting `securityContext.fsGroup: 65532` to match Dapr's UID

The solution requires BOTH the correct annotation AND the securityContext configuration. However, for production deployments, the localstorage binding should be replaced with cloud-native storage solutions (Azure Blob Storage, AWS S3, or MinIO) to avoid permission issues and enable scalability.

**Next Steps:**
1. Apply the corrected configuration with `dapr.io/volume-mounts-rw` and `fsGroup: 65532`
2. Verify the binding initializes successfully and can create files
3. Plan migration to Azure Blob Storage or MinIO for production deployments
