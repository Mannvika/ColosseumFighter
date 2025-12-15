using System.Collections.Generic;
using UnityEngine;

public enum StatType
{
    MoveSpeed,
    Damage,
    RangedDamage,
    Defense
}

[System.Serializable]
public struct StatModifier
{
    public StatType Type;
    public float Value;
    public int TicksRemaining; // -1 for permanent
    public int Charges;
}

[System.Serializable]
public struct StatState
{
    public StatModifier Mod0;
    public StatModifier Mod1;
    public StatModifier Mod2;
    public StatModifier Mod3;
    public int ActiveCount;
}

public class StatSystem
{
    private List<StatModifier> _modifiers = new List<StatModifier>();
    private const int MAX_MODIFIERS = 4;

    public void AddModifier(StatType type, float value, int charges, int durationTicks)
    {
        if (_modifiers.Count >= MAX_MODIFIERS)
        {
            Debug.LogWarning("Too many modifiers! Ignored.");
            return;
        }

        _modifiers.Add(new StatModifier 
        { 
            Type = type, 
            Value = value, 
            TicksRemaining = durationTicks,
            Charges = charges
        });
    }

    public void ConsumeCharge(StatType type)
    {
        for (int i = _modifiers.Count - 1; i >= 0; i--)
        {
            var mod = _modifiers[i];
            
            if (mod.Type == type && mod.Charges > 0)
            {
                mod.Charges--;
                _modifiers[i] = mod;

                if (mod.Charges <= 0)
                {
                    _modifiers.RemoveAt(i);
                }
            }
        }
    }

    public float GetStat(StatType type, float baseValue)
    {
        float finalValue = baseValue;
        for (int i = 0; i < _modifiers.Count; i++)
        {
            if (_modifiers[i].Type == type)
            {
                finalValue *= _modifiers[i].Value; 
            }
        }
        return finalValue;
    }

    public void Tick()
    {
        for (int i = _modifiers.Count - 1; i >= 0; i--)
        {
            var mod = _modifiers[i];
            
            if (mod.TicksRemaining != -1)
            {
                mod.TicksRemaining--;
                _modifiers[i] = mod;

                if (mod.TicksRemaining <= 0)
                {
                    _modifiers.RemoveAt(i);
                }
            }
        }
    }

    public StatState GetState()
    {
        StatState state = new StatState();
        state.ActiveCount = _modifiers.Count;
        
        // Manually map to struct fields (ugly but safe for Netcode serialization)
        if (_modifiers.Count > 0) state.Mod0 = _modifiers[0];
        if (_modifiers.Count > 1) state.Mod1 = _modifiers[1];
        if (_modifiers.Count > 2) state.Mod2 = _modifiers[2];
        if (_modifiers.Count > 3) state.Mod3 = _modifiers[3];
        
        return state;
    }

    public void SetState(StatState state)
    {
        _modifiers.Clear();
        if (state.ActiveCount > 0) _modifiers.Add(state.Mod0);
        if (state.ActiveCount > 1) _modifiers.Add(state.Mod1);
        if (state.ActiveCount > 2) _modifiers.Add(state.Mod2);
        if (state.ActiveCount > 3) _modifiers.Add(state.Mod3);
    }
    
    public void Reset()
    {
        _modifiers.Clear();
    }
}