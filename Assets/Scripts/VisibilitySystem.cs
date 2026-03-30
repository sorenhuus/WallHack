using System.Collections.Generic;
using Mirror;
using UnityEngine;

/// <summary>
/// Runs on the server every tick. Raycasts between all player pairs and only
/// sends position updates to clients that have line-of-sight to the observed player.
/// </summary>
public class VisibilitySystem : MonoBehaviour
{
    [SerializeField] private float eyeHeight = 1.6f;
    [SerializeField] private LayerMask occlusionMask = Physics.DefaultRaycastLayers;

    private readonly List<NetworkIdentity> _players = new();
    private readonly List<PlayerMovement> _movements = new();

    private void FixedUpdate()
    {
        if (!NetworkServer.active) return;

        // Collect all connected players and their cached PlayerMovement
        _players.Clear();
        _movements.Clear();
        foreach (var conn in NetworkServer.connections.Values)
        {
            if (conn.identity == null) continue;
            if (!conn.identity.TryGetComponent<PlayerMovement>(out var movement)) continue;
            _players.Add(conn.identity);
            _movements.Add(movement);
        }

        // For each pair, check LoS and send position if visible
        for (int i = 0; i < _players.Count; i++)
        {
            for (int j = 0; j < _players.Count; j++)
            {
                if (i == j) continue;

                NetworkIdentity observer = _players[i]; // receives the update

                // Host observer sees server state directly — no RPC needed
                if (observer.connectionToClient == null ||
                    observer.connectionToClient == NetworkServer.localConnection) continue;

                NetworkIdentity observed = _players[j]; // whose position is sent

                if (HasLineOfSight(observer.transform.position, observed.transform.position))
                    _movements[j].RpcSetRemotePosition(
                        observer.connectionToClient,
                        observed.transform.position,
                        observed.transform.rotation
                    );
            }
        }
    }

    private bool HasLineOfSight(Vector3 fromBase, Vector3 toBase)
    {
        Vector3 from = fromBase + Vector3.up * eyeHeight;
        Vector3 to   = toBase   + Vector3.up * eyeHeight;
        Vector3 direction = to - from;
        RaycastCounter.Increment();
        return !Physics.Raycast(from, direction.normalized, direction.magnitude, occlusionMask);
    }
}
