using System;

// Flags enum: being hit by any selected element kills the enemy instantly.
[Flags]
public enum ElementWeakness
{
    None = 0,
    Fire = 1 << 0,
    Water = 1 << 1,
    Shock = 1 << 2,
    Ice = 1 << 3
}

public static class ElementWeaknessExtensions
{
    public static bool IsWeakTo(this ElementWeakness weakness, SpellElement element)
    {
        switch (element)
        {
            case SpellElement.Fire: return weakness.HasFlag(ElementWeakness.Fire);
            case SpellElement.Water: return weakness.HasFlag(ElementWeakness.Water);
            case SpellElement.Shock: return weakness.HasFlag(ElementWeakness.Shock);
            case SpellElement.Ice: return weakness.HasFlag(ElementWeakness.Ice);
            default: return false;
        }
    }
}
