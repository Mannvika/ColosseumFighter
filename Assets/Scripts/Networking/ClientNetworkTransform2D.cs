using Unity.Netcode.Components;
using UnityEngine;

[DisallowMultipleComponent]
public class ClientNetworkTransform2D : NetworkTransform
{
    protected override bool OnIsServerAuthoritative()
    {
        return false;
    }
}