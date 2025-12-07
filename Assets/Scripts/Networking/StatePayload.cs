using UnityEngine;
using Unity.Netcode;

public class StatePayload : INetworkSerializable
{

    public int Tick;
    public Vector2 Position;
    public Vector2 Velocity;
    public PlayerState State;
    
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Tick);
        serializer.SerializeValue(ref Position);
        serializer.SerializeValue(ref Velocity);
        serializer.SerializeValue(ref State);
    }    
}
