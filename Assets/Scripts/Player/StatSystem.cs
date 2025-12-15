using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System;

public enum StatType
{
    MoveSpeed,
    Damage,
    RangedDamage,
    Defense
}

[System.Serializable]
public struct StatModifier: INetworkSerializable, IEquatable<StatModifier>
{
    public StatType Type;
    public float Value;
    public int TicksRemaining; // -1 for permanent
    public int Charges;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Type);
        serializer.SerializeValue(ref Value);
        serializer.SerializeValue(ref TicksRemaining);
        serializer.SerializeValue(ref Charges);
    }

    public bool Equals(StatModifier other)
    {
        return Type == other.Type && 
               Mathf.Approximately(Value, other.Value) && 
               TicksRemaining == other.TicksRemaining && 
               Charges == other.Charges;
    }
}

[System.Serializable]
public struct StatState: INetworkSerializable, IEquatable<StatState>
{
    public StatModifier Mod0;
    public StatModifier Mod1;
    public StatModifier Mod2;
    public StatModifier Mod3;
    public int ActiveCount;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Mod0);
        serializer.SerializeValue(ref Mod1);
        serializer.SerializeValue(ref Mod2);
        serializer.SerializeValue(ref Mod3);
        serializer.SerializeValue(ref ActiveCount);
    }

    public bool Equals(StatState other)
    {
        return Mod0.Equals(other.Mod0) && 
               Mod1.Equals(other.Mod1) && 
               Mod2.Equals(other.Mod2) && 
               Mod3.Equals(other.Mod3) && 
               ActiveCount == other.ActiveCount;
    }
}

public class StatSystem
{
    private List<StatModifier> _modifiers = new List<StatModifier>();
    private const int MAX_MODIFIERS = 4;

    public event Action<StatType> OnStatDepleted;

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

                    OnStatDepleted?.Invoke(type);
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
                    OnStatDepleted?.Invoke(mod.Type);
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