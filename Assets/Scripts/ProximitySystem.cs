using System.Collections.Generic;
using Mirror;
using UnityEngine;

/// <summary>
/// Runs on the server every tick alongside VisibilitySystem.
/// Syncs player positions when any of 4 rays toward an enlarged capsule
/// around the observed player goes unblocked — catching players near corners
/// before they are fully visible.
/// </summary>
public class ProximitySystem : MonoBehaviour
{
    [SerializeField] private float eyeHeight = 1.6f;
    [SerializeField] private LayerMask occlusionMask = Physics.DefaultRaycastLayers;

    [Header("Proximity Capsule")]
    [SerializeField] private float capsuleRadius = 1.2f;     // wider than the player capsule
    [SerializeField] private float capsuleHalfHeight = 1.1f; // taller than the player capsule

    private readonly List<NetworkIdentity> _players = new();
    private readonly List<PlayerMovement> _movements = new();

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

                // Host observer sees server state directly — no RPC needed
                if (observer.connectionToClient == null ||
                    observer.connectionToClient == NetworkServer.localConnection) continue;

                NetworkIdentity observed = _players[j];

                if (IsNearCapsule(observer.transform.position, observed.transform.position))
                    _movements[j].RpcSetRemotePosition(
                        observer.connectionToClient,
                        observed.transform.position,
                        observed.transform.rotation
                    );
            }
        }
    }

    /// <summary>
    /// Casts 4 rays from the observer toward the top, bottom, left, and right
    /// edges of an enlarged capsule around the observed player.
    /// Returns true if any ray reaches its target unblocked.
    /// </summary>
    private bool IsNearCapsule(Vector3 observerBase, Vector3 observedBase)
    {
        Vector3 origin = observerBase + Vector3.up * eyeHeight;
        Vector3 center = observedBase + Vector3.up * eyeHeight;

        // Perpendicular to the horizontal direction observer → observed
        Vector3 horizontal = observedBase - observerBase;
        horizontal.y = 0f;
        if (horizontal.sqrMagnitude < 0.001f) return false;
        Vector3 right = Vector3.Cross(Vector3.up, horizontal.normalized).normalized;

        Vector3[] targets = new Vector3[]
        {
            center + Vector3.up    * capsuleHalfHeight, // top
            center - Vector3.up    * capsuleHalfHeight, // bottom
            center + right         * capsuleRadius,     // left
            center - right         * capsuleRadius,     // right
        };

        foreach (var target in targets)
        {
            Vector3 direction = target - origin;
            RaycastCounter.Increment();
            if (!Physics.Raycast(origin, direction.normalized, direction.magnitude, occlusionMask))
                return true;
        }

        return false;
    }
}
