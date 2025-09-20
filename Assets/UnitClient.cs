using Mirror;
using System;
using UnityEngine;

public class UnitClient : NetworkBehaviour
{
    public int prefabID;

    [SyncVar]
    public int team;

    [SyncVar(hook = nameof(OnStatsChanged))]
    internal UnitStats data;

    [SyncVar(hook = nameof(OnPositionChanged))]
    public Vector3 targetHexPosition;

    public override void OnStartClient()
    {
        Debug.Log($"[Client] {gameObject.name} spawned!");
    }

    //[Command (requiresAuthority = false)]
    public void ApplyTurnUpdate(Vector3 newPos)
    {
        targetHexPosition = newPos;
        //ApplyServerMovement(newPos);
    }

    //[Command(requiresAuthority = false)]
    //public void ApplyServerMovement(Vector3 newPos)
    //{
    //    targetHexPosition = newPos;
    //}

    private void OnStatsChanged(UnitStats oldStats, UnitStats newStats)
    {
        gameObject.GetComponent<UnitClass>().data = newStats;
    }

    void OnPositionChanged(Vector3 oldPos, Vector3 newPos)
    {
        HexManager.instance.Hexes[GetComponent<UnitClass>().HexPosition].UnOccupy();

        transform.position = newPos; // Or smooth movement if desired

        HexManager.instance.Hexes[HexManager.instance.WorldToHex(newPos, 2f)].Occupy(gameObject);

    }

    //[Command]
    //internal void ApplyUnitMovement(uint netId, Vector3 newPos)
    //{
    //    if (NetworkServer.spawned.TryGetValue(netId, out NetworkIdentity identity))
    //    {
    //        UnitClient unit = identity.GetComponent<UnitClient>();
    //        if (unit != null)
    //        {
    //            unit.ApplyServerMovement(newPos);
    //        }
    //    }
    //}
}
