using System;
using System.Text;
using UnityEngine;
using TMPro;
using Biofall.Core;
using Biofall.Gameplay;

namespace Biofall.UI
{
    /// <summary>
    /// WaveMode-only arcade scoreboard. Tallies kills per enemy type and the waves
    /// survived while the run is live, then fills the summary text when the player
    /// dies (the existing GameOverUI shows the panel that contains it).
    ///
    /// WaveMode is pure arcade — there are NO Bio Samples here at all (enemies don't
    /// drop them and nothing is banked toward upgrades).
    ///
    /// Two output modes (use whichever is wired):
    ///   • summaryText  — one multi-line TMP_Text (two columns via &lt;pos&gt; tags).
    ///   • individual labels — countLabel per category + total/waves.
    /// </summary>
    public sealed class WaveModeSummary : MonoBehaviour
    {
        [Serializable]
        public sealed class KillCategory
        {
            [Tooltip("Display name, e.g. \"ZOMBIES\".")]
            public string label;
            [Tooltip("Enemy type this row counts — the EN_* EnemyData asset on that enemy's prefab.")]
            public EnemyData data;
            [Tooltip("Optional: text that shows this type's kill count (individual-labels mode).")]
            public TMP_Text countLabel;

            [NonSerialized] public int count;
        }

        [Header("Kill breakdown (one row per enemy type)")]
        [SerializeField] private KillCategory[] categories;

        [Header("Output A — single multi-line text (recommended)")]
        [Tooltip("If set, the whole summary is written here as one formatted block.")]
        [SerializeField] private TMP_Text summaryText;
        [Tooltip("Right-edge column position for the numbers (TMP <pos> percentage).")]
        [SerializeField] private float valueColumnPercent = 70f;
        [SerializeField] private string totalKillsRow = "TOTAL KILLS";
        [SerializeField] private string wavesRow = "WAVES SURVIVED";

        [Header("Output B — individual labels (optional)")]
        [SerializeField] private TMP_Text totalKillsLabel;
        [SerializeField] private TMP_Text wavesLabel;
        [SerializeField] private string totalKillsFormat = "{0}";
        [SerializeField] private string wavesFormat = "{0}";

        private int _totalKills;
        private int _maxWave = 1;
        private bool _filled;

        private void OnEnable()
        {
            EventBus.Subscribe<TargetDied>(OnEnemyKilled);
            EventBus.Subscribe<PlayerDied>(OnPlayerDied);
            WaveSpawner.WaveStarted += OnWaveStarted;
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<TargetDied>(OnEnemyKilled);
            EventBus.Unsubscribe<PlayerDied>(OnPlayerDied);
            WaveSpawner.WaveStarted -= OnWaveStarted;
        }

        private void OnWaveStarted(int wave) => _maxWave = Mathf.Max(_maxWave, wave);

        private void OnEnemyKilled(TargetDied e)
        {
            if (e.Target == null) return;

            var enemy = e.Target.GetComponent<Enemy>();
            if (enemy == null) return;

            _totalKills++;

            EnemyData data = enemy.Data;
            if (categories == null) return;

            for (int i = 0; i < categories.Length; i++)
            {
                var c = categories[i];
                if (c == null || c.data == null || c.data != data) continue;
                c.count++;
                break;
            }
        }

        private void OnPlayerDied(PlayerDied _) => FillSummary();

        private void FillSummary()
        {
            if (_filled) return;
            _filled = true;

            if (summaryText != null)
            {
                summaryText.text = BuildSummary();
                return;
            }

            // Individual-labels fallback.
            if (categories != null)
                foreach (var c in categories)
                    if (c != null && c.countLabel != null)
                        c.countLabel.text = c.count.ToString();

            if (totalKillsLabel != null) totalKillsLabel.text = string.Format(totalKillsFormat, _totalKills);
            if (wavesLabel != null) wavesLabel.text = string.Format(wavesFormat, _maxWave);
        }

        private string BuildSummary()
        {
            var sb = new StringBuilder(256);
            string col = $"<pos={valueColumnPercent.ToString(System.Globalization.CultureInfo.InvariantCulture)}%>";

            if (categories != null)
            {
                foreach (var c in categories)
                {
                    if (c == null) continue;
                    string name = string.IsNullOrEmpty(c.label) ? (c.data != null ? c.data.name : "?") : c.label;
                    sb.Append(name).Append(col).Append(c.count).Append('\n');
                }
            }

            sb.Append('\n');
            sb.Append(totalKillsRow).Append(col).Append(_totalKills).Append('\n');
            sb.Append(wavesRow).Append(col).Append(_maxWave);
            return sb.ToString();
        }
    }
}
