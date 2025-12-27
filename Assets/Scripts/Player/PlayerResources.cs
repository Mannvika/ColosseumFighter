using UnityEngine;
using Unity.Netcode;

public class PlayerResources : NetworkBehaviour
{
    private PlayerController _controller;

    public NetworkVariable<float> SignatureCharge = new NetworkVariable<float>(0f);
    public NetworkVariable<float> BlockCharge = new NetworkVariable<float>(0f);
    public NetworkVariable<bool> CanBlock = new NetworkVariable<bool>(true);

    public void Initialize(PlayerController controller)
    {
        _controller = controller;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer && _controller.championData != null)
        {
            BlockCharge.Value = _controller.championData.blockAbility.maxCharge;
        }
    }

    private void Update()
    {
        if (!IsServer || _controller == null || _controller.championData == null) return;
        
        HandleBlockCharge();
        HandleSignatureCharge();
    }

    private void HandleBlockCharge()
    {
        var data = _controller.championData.blockAbility;
        float dt = Time.deltaTime;

        if (_controller.currentState != PlayerState.Blocking)
        {
            BlockCharge.Value = Mathf.Min(BlockCharge.Value + (data.chargePerSecond * dt), data.maxCharge);
            
            if (!CanBlock.Value && BlockCharge.Value >= data.maxCharge)
                CanBlock.Value = true;
        }
        else
        {
            BlockCharge.Value = Mathf.Max(0, BlockCharge.Value - (data.dischargePerSecond * dt));
            
            if (BlockCharge.Value <= 0)
            {
                BlockCharge.Value = 0f;
                CanBlock.Value = false;
                _controller.ForceEndState(PlayerState.Blocking); 
            }
        }
    }

    private void HandleSignatureCharge()
    {
        var data = _controller.championData.signatureAbility;
        if (data == null) return;
        
        SignatureCharge.Value = Mathf.Min(SignatureCharge.Value + (data.chargePerSecond * Time.deltaTime), data.maxCharge);
    }

    public void AddSignatureCharge(float damageDealt)
    {
        if (!IsServer) return;
        var data = _controller.championData.signatureAbility;
        SignatureCharge.Value = Mathf.Min(SignatureCharge.Value + (damageDealt * data.chargePerDamageDealt), data.maxCharge);
    }

    public void ResetSignatureCharge()
    {
        if(IsServer) SignatureCharge.Value = 0f;
    }
}