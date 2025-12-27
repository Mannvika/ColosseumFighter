using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public struct LobbyPlayerData : INetworkSerializable, IEquatable<LobbyPlayerData>
{
    public ulong clientID;
    public bool isReady;
    public FixedString32Bytes playerName;
    public int championId; // -1 = None, 0 = Champ A, 1 = Champ B
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref clientID);
        serializer.SerializeValue(ref isReady);
        serializer.SerializeValue(ref championId);
        serializer.SerializeValue(ref playerName);
    }

    public bool Equals(LobbyPlayerData other)
    {
        return clientID == other.clientID && isReady == other.isReady && playerName == other.playerName && championId == other.championId;
    }
}
