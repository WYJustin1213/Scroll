using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class SentencePopupLaneSpells : MonoBehaviour
{
    [Serializable]
    private class LaneSpell
    {
        public LaneSpellPoint lane;
        public SpellDefinition spell;
    }

    [SerializeField] private SentencePopup sentencePopup;
    [SerializeField] private LaneSpellController spellController;
    [Tooltip("Each entry casts its assigned spell into its lane when the sentence is completed.")]
    [SerializeField] private List<LaneSpell> laneSpells = new List<LaneSpell>();

    private void OnEnable()
    {
        if (sentencePopup == null || spellController == null)
        {
            Debug.LogError("SentencePopupLaneSpells needs a sentence popup and spell controller.", this);
            enabled = false;
            return;
        }

        sentencePopup.Completed += CastLaneSpells;
    }

    private void OnDisable()
    {
        if (sentencePopup != null)
            sentencePopup.Completed -= CastLaneSpells;
    }

    private void CastLaneSpells()
    {
        PlayerResources resources = spellController.Resources;
        if (resources == null)
        {
            Debug.LogError("SentencePopupLaneSpells needs the spell controller's player resources.", this);
            return;
        }

        foreach (LaneSpell entry in laneSpells)
        {
            if (entry == null || entry.lane == null || entry.spell == null)
            {
                Debug.LogError("SentencePopupLaneSpells entries need a lane and spell.", this);
                continue;
            }

            bool cast = spellController.CastSpellWithoutManaCost(entry.spell, entry.lane);
            if (!cast)
                Debug.LogWarning("Automatic spell \"" + entry.spell.name + "\" could not be cast in " + entry.lane.LaneName + ".", this);
        }
    }
}
