using System.Collections.Generic;
using Mirror;
using UnityEngine;

/// <summary>
/// Runs on the server every tick. Casts a ray from the observer to every padded
/// mesh vertex on the observed player. If any ray is unblocked the position is synced.
/// The padding pushes vertices outward along their normals, catching players
/// that are nearly visible before they fully round a corner.
/// </summary>
public class MeshVisibilitySystem : MonoBehaviour
{
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private float vertexPadding = 0.15f;
    [SerializeField] private LayerMask occlusionMask = Physics.DefaultRaycastLayers;

    private Vector3[] _localTargets;
    private float _eyeOffset;

    private readonly List<NetworkIdentity> _players = new();
    private readonly List<PlayerMovement> _movements = new();

    private void Start()
    {
        if (playerPrefab == null)
        {
            Debug.LogError("MeshVisibilitySystem: playerPrefab is not assigned.");
            enabled = false;
            return;
        }

        MeshFilter mf = playerPrefab.GetComponentInChildren<MeshFilter>();
        if (mf == null || mf.sharedMesh == null)
        {
            Debug.LogError("MeshVisibilitySystem: No MeshFilter or mesh found on playerPrefab.");
            enabled = false;
            return;
        }

        Camera cam = playerPrefab.GetComponentInChildren<Camera>(includeInactive: true);
        if (cam == null)
        {
            Debug.LogError("MeshVisibilitySystem: No Camera found on playerPrefab.");
            enabled = false;
            return;
        }
        _eyeOffset = cam.transform.localPosition.y;

        Vector3[] vertices = mf.sharedMesh.vertices;
        Vector3[] normals = mf.sharedMesh.normals;

        // Account for the MeshFilter's local offset within the prefab
        Transform meshTransform = mf.transform;

        _localTargets = new Vector3[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            // Transform vertex to prefab root local space, then push out along normal
            Vector3 worldVertex = meshTransform.TransformPoint(vertices[i]);
            Vector3 worldNormal = meshTransform.TransformDirection(normals[i]);
            _localTargets[i] = worldVertex + worldNormal * vertexPadding;
        }
    }

    private void FixedUpdate()
    {
        if (!NetworkServer.active) return;

        _players.Clear();
        _movements.Clear();
        foreach (var conn in NetworkServer.connections.Values)
        {
            if (conn.identity == null) continue;
            if (!conn.identity.TryGetComponent<PlayerMovement>(out var movement)) continue;
            _players.Add(conn.identity);
            _movements.Add(movement);
        }

        for (int i = 0; i < _players.Count; i++)
        {
            for (int j = 0; j < _players.Count; j++)
            {
                if (i == j) continue;

                NetworkIdentity observer = _players[i];

                if (observer.connectionToClient == null ||
                    observer.connectionToClient == NetworkServer.localConnection) continue;

                NetworkIdentity observed = _players[j];

                if (CanSee(observer.transform.position, observed.transform.position))
                    _movements[j].RpcSetRemotePosition(
                        observer.connectionToClient,
                        observed.transform.position,
                        observed.transform.rotation
                    );
            }
        }
    }

    private bool CanSee(Vector3 observerBase, Vector3 observedBase)
    {
        Vector3 observerEye = observerBase + Vector3.up * _eyeOffset;

        foreach (Vector3 localTarget in _localTargets)
        {
            // Offset the pre-computed target by the observed player's world position
            Vector3 worldTarget = observedBase + localTarget;
            Vector3 direction = worldTarget - observerEye;

            RaycastCounter.Increment();
            if (!Physics.Raycast(observerEye, direction.normalized, direction.magnitude, occlusionMask))
                return true;
        }

        return false;
    }

}
