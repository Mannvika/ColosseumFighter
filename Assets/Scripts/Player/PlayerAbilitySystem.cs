using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAbilitySystem
{
    private PlayerController _controller;
    private Dictionary<AbilityBase, int> _cooldowns = new Dictionary<AbilityBase, int>();
    private Dictionary<StatType, AbilityBase> _statCooldownMap = new Dictionary<StatType, AbilityBase>();
    
    public event Action<AbilityBase, float> OnCooldownStarted;

    public PlayerAbilitySystem(PlayerController controller)
    {
        _controller = controller;
        _controller.Stats.OnStatDepleted += HandleStatDepletion;
    }

    public void RegisterStatWait(AbilityBase ability, StatType type)
    {
        _statCooldownMap[type] = ability;
    }

    private void HandleStatDepletion(StatType type)
    {
        if (_statCooldownMap.TryGetValue(type, out AbilityBase ability))
        {
            SetCooldown(ability);
            _statCooldownMap.Remove(type);
        }
    }

    public void OnDestroy()
    {
        if(_controller != null && _controller.Stats != null)
             _controller.Stats.OnStatDepleted -= HandleStatDepletion;
    }

    public void ProcessInput(PlayerNetworkInputData input)
    {
        var champion = _controller.championData;
        var state = _controller.currentState;

        if (state == PlayerState.Dashing)
        {
            if (_controller.Movement.IsDashFinished(_controller.CurrentTick, champion.dashAbility.dashDuration, Time.fixedDeltaTime))
            {
                EndAbility(champion.dashAbility);
            }
            return; 
        }

        if (state == PlayerState.Blocking)
        {
            if (input.IsDashPressed) TryActivateAbility(champion.dashAbility, PlayerState.Dashing);
            else if (!input.IsBlockPressed) EndAbility(champion.blockAbility);
            return;
        }

        if (state == PlayerState.Firing)
        {
            if (input.IsDashPressed) TryActivateAbility(champion.dashAbility, PlayerState.Dashing);
            else if (input.IsProjectilePressed) champion.projectileAbility.ProcessHold(_controller, _controller.IsServer);
            else EndAbility(champion.projectileAbility);
        }

        if (state == PlayerState.Normal || state == PlayerState.Firing)
        {
            if (input.IsDashPressed) TryActivateAbility(champion.dashAbility, PlayerState.Dashing);
            else if (input.IsBlockPressed) TryActivateAbility(champion.blockAbility, PlayerState.Blocking);
            else if (input.IsMeleePressed) TryActivateAbility(champion.meleeAttack, PlayerState.Attacking);
            else if (input.IsProjectilePressed && state != PlayerState.Firing) TryActivateAbility(champion.projectileAbility, PlayerState.Firing);
            else if (input.IsPrimaryAbilityPressed) TryActivateAbility(champion.primaryAbility, PlayerState.Normal);
            else if (input.IsSignatureAbilityPressed) TryActivateAbility(champion.signatureAbility, PlayerState.Normal);
        }
    }

    public void TryActivateAbility(AbilityBase ability, PlayerState targetState)
    {
        if (ability == null) return;
        if (!CanTransitionTo(targetState)) return;
        

        if (targetState != PlayerState.Firing && IsOnCooldown(ability)) return;

        if (_controller.currentState == PlayerState.Blocking && targetState != PlayerState.Blocking)
        {
            EndAbility(_controller.championData.blockAbility);
        }
        else if (_controller.currentState == PlayerState.Firing && targetState != PlayerState.Firing)
        {
            EndAbility(_controller.championData.projectileAbility);
        }

        _controller.currentActiveAbility = ability;
        ability.Activate(_controller, _controller.IsServer); 

        if (targetState != PlayerState.Firing && !ability.startCooldownOnEnd)
        {
            SetCooldown(ability);
        }
    }

    public void EndAbility(AbilityBase ability)
    {
        if (ability != null)
        {
            if(ability.startCooldownOnEnd)
            {
                SetCooldown(ability);
            }
            ability.OnEnd(_controller, _controller.IsServer);

        } 
    }

    public bool IsOnCooldown(AbilityBase ability)
    {
        if (_cooldowns.TryGetValue(ability, out int endTick))
        {
            int threshold = _controller.CurrentTick + (_controller.IsServer ? 1 : 0);
            return threshold < endTick;
        }
        return false;
    }

    public void SetCooldown(AbilityBase ability)
    {
        int durationTicks = Mathf.CeilToInt(ability.cooldown / Time.fixedDeltaTime);
        int endTick = _controller.CurrentTick + durationTicks;

        bool isHostPrediction = _controller.IsServer && _controller.IsOwner && _controller.isPredicting;

        if (!isHostPrediction)
        {
            _cooldowns[ability] = endTick;
        }

        if (_controller.IsOwner) OnCooldownStarted?.Invoke(ability, ability.cooldown);
    }

    private bool CanTransitionTo(PlayerState newState)
    {
        var current = _controller.currentState;
        if (newState == PlayerState.Normal) return true;

        switch (current)
        {
            case PlayerState.Normal:
                if (newState == PlayerState.Blocking && !_controller.Resources.CanBlock.Value) return false;
                return true;
            case PlayerState.Blocking:
                return (newState == PlayerState.Blocking && _controller.Resources.CanBlock.Value) || newState == PlayerState.Dashing;
            case PlayerState.Firing:
                return newState == PlayerState.Dashing || newState == PlayerState.Firing;
            case PlayerState.Dashing:
            case PlayerState.Stunned:
                return false;
        }
        return false;
    }
}