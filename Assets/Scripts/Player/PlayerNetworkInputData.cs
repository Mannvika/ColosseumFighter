using Unity.Netcode;
using UnityEngine;

public struct PlayerNetworkInputData : INetworkSerializable
{
    public int Tick;
    public Vector2 Movement;
    public Vector2 MousePosition;
    public byte ButtonFlags;
    public bool IsDashPressed => (ButtonFlags & (1 << 0)) != 0; // Default: Space Bar
    public bool IsMeleePressed => (ButtonFlags & (1 << 1)) != 0; // Default: V
    public bool IsPrimaryAbilityPressed => (ButtonFlags & (1 << 2)) != 0; // Default: E
    public bool IsSignatureAbilityPressed => (ButtonFlags & (1 << 3)) != 0; // Default Q
    public bool IsProjectilePressed => (ButtonFlags & (1 << 4)) != 0; // Default Left Click
    public bool IsBlockPressed => (ButtonFlags & (1 << 5)) != 0; // Default: Right Click
    
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Tick);
        serializer.SerializeValue(ref Movement);
        serializer.SerializeValue(ref MousePosition);
        serializer.SerializeValue(ref ButtonFlags);
    }
}