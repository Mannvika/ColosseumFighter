using Unity.Netcode;
using UnityEngine;

public struct PlayerNetworkInputData : INetworkSerializable
{
    public Vector2 Movement;
    public Vector2 MousePosition;
    public bool IsMeleePressed;
    public bool IsBlockPressed;
    public bool IsDashPressed;
    
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Movement);
        serializer.SerializeValue(ref MousePosition);
        serializer.SerializeValue(ref IsMeleePressed);
        serializer.SerializeValue(ref IsBlockPressed);
        serializer.SerializeValue(ref IsDashPressed);
    }
}