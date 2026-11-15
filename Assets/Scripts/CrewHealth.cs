using System;
using UnityEngine;

/// <summary>Health state for a crew member; damage sources and treatment call this component.</summary>
public sealed class CrewHealth : MonoBehaviour
{
    [SerializeField, Min(1)] private float maxHealth=100;
    public float Current {get;private set;}
    public float Maximum=>maxHealth;
    public float Fraction=>Current/maxHealth;
    public event Action<CrewHealth> Changed;
    private void Awake(){maxHealth=Mathf.Max(1,maxHealth);Current=maxHealth;}
    public void TakeDamage(float amount)
    {
        if(amount<=0 || float.IsNaN(amount) || float.IsInfinity(amount)) return;
        SetHealth(Current-amount);
    }
    public void Heal(float amount)
    {
        if(amount<=0 || float.IsNaN(amount) || float.IsInfinity(amount)) return;
        SetHealth(Current+amount);
    }
    private void SetHealth(float value)
    {
        float next=Mathf.Clamp(value,0,maxHealth);
        if(next==Current) return;
        Current=next;Changed?.Invoke(this);
    }
}
