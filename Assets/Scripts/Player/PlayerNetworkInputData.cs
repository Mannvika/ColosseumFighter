using Unity.Netcode;
using UnityEngine;

public struct PlayerNetworkInputData : INetworkSerializable
{
    public int Tick;
    public Vector2 Movement;
    public Vector2 MousePosition;
    public byte ButtonFlags;
    public bool IsDashPressed; // Default: Space Bar
    public bool IsMeleePressed; // Default: V
    public bool IsPrimaryAbilityPressed; // Default: E
    public bool IsSignatureAbilityPressed; // Default Q
    public bool IsProjectilePressed; // Default Left Click
    public bool IsBlockPressed; // Default: Right Click
    
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Movement);
        serializer.SerializeValue(ref MousePosition);
        serializer.SerializeValue(ref IsMeleePressed);
        serializer.SerializeValue(ref IsBlockPressed);
        serializer.SerializeValue(ref IsDashPressed);
        serializer.SerializeValue(ref IsProjectilePressed);
        serializer.SerializeValue(ref IsPrimaryAbilityPressed);
        serializer.SerializeValue(ref IsSignatureAbilityPressed);
    }
}