using UnityEngine;

[CreateAssetMenu(
    fileName = "GameRules",
    menuName = "Command FPS/Game Rules")]
public class GameRulesConfig : ScriptableObject
{
    [Header("Grid")]
    [Min(0.1f)]
    public float cellSize = 2f;

    [Header("Movement")]
    [Min(0f)]
    public float moveDuration = 0.22f;

    [Min(0f)]
    public float turnDuration = 0.12f;

    [Header("Spells")]
    [Min(0f)]
    public float fireballCastDuration = 0.15f;

    [Min(0.1f)]
    public float debugFireballLength = 6f;

    [Header("Planning / Future Phase 3")]
    public bool failedActionStopsSequence = true;
}
