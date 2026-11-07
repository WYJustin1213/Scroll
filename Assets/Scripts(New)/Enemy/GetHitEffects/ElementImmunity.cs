using System;

// Flags enum: the inspector shows a multi-select dropdown (Nothing / Everything / individual elements).
[Flags]
public enum ElementImmunity
{
    None = 0,
    Fire = 1 << 0,
    Water = 1 << 1,
    Shock = 1 << 2,
    Ice = 1 << 3
}

public static class ElementImmunityExtensions
{
    public static bool IsImmuneTo(this ElementImmunity immunity, SpellElement element)
    {
        switch (element)
        {
            case SpellElement.Fire: return immunity.HasFlag(ElementImmunity.Fire);
            case SpellElement.Water: return immunity.HasFlag(ElementImmunity.Water);
            case SpellElement.Shock: return immunity.HasFlag(ElementImmunity.Shock);
            case SpellElement.Ice: return immunity.HasFlag(ElementImmunity.Ice);
            default: return false;
        }
    }
}
