using Mirror;
using UnityEngine;

public class UnitClient : NetworkBehaviour
{
    public int prefabID;

    [SyncVar]
    public int team;

    [SyncVar(hook = nameof(OnPositionChanged))]
    public Vector3 targetHexPosition;
    public override void OnStartClient()
    {
        Debug.Log($"[Client] {gameObject.name} spawned!");
    }

    [Server]
    public void ApplyTurnUpdate(Vector3 newPos)
    {
        targetHexPosition = newPos;
    }

    void OnPositionChanged(Vector3 oldPos, Vector3 newPos)
    {
        transform.position = newPos; // Or smooth movement if desired
    }

}
