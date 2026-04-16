using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UsefulScripts.MathRoguelike.Entities;
using UsefulScripts.MathRoguelike.Core;

namespace UsefulScripts.MathRoguelike.UI
{
    /// <summary>
    /// Heads-Up Display: shows player HP, MP, gold, score, floor and room.
    /// Subscribes to <see cref="RunData"/> changes via the game manager.
    /// </summary>
    public class PlayerHUD : MonoBehaviour
    {
        [Header("HP")]
        [SerializeField] private Slider    hpSlider;
        [SerializeField] private TMP_Text  hpLabel;

        [Header("MP")]
        [SerializeField] private Slider    mpSlider;
        [SerializeField] private TMP_Text  mpLabel;

        [Header("Economy / Progression")]
        [SerializeField] private TMP_Text  goldLabel;
        [SerializeField] private TMP_Text  scoreLabel;
        [SerializeField] private TMP_Text  floorLabel;
        [SerializeField] private TMP_Text  roomLabel;

        private RunData _run;

        private void Start()
        {
            _run = MathRoguelikeGameManager.Instance?.CurrentRun;
            Refresh();
        }

        /// <summary>Call this each frame or whenever stats change.</summary>
        public void Refresh()
        {
            if (_run == null) return;

            SetSlider(hpSlider, hpLabel, _run.currentHp, _run.maxHp, "HP");
            SetSlider(mpSlider, mpLabel, _run.currentMp, _run.maxMp, "MP");

            if (goldLabel)  goldLabel.text  = $"💰 {_run.gold}";
            if (scoreLabel) scoreLabel.text = $"⭐ {_run.score}";
            if (floorLabel) floorLabel.text = $"Floor {_run.currentFloor}";
            if (roomLabel)  roomLabel.text  = $"Room {_run.currentRoomIndex + 1}";
        }

        private void Update() => Refresh();

        private static void SetSlider(Slider s, TMP_Text label, int current, int max, string name)
        {
            if (s)
            {
                s.minValue = 0;
                s.maxValue = max;
                s.value    = current;
            }
            if (label) label.text = $"{name}: {current}/{max}";
        }
    }
}
