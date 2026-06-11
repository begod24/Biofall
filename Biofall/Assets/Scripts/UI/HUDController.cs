using Biofall.Core;
using Biofall.Enemies;
using UnityEngine;

namespace Biofall.UI
{
    /// <summary>
    /// Minimal first-sprint HUD: health, ammo, and live enemy count. Listens to
    /// <see cref="GameEvents"/> (never polls gameplay) and draws via IMGUI so it has no
    /// uGUI/TMP package dependency yet. Replace with a uGUI canvas in the UI polish pass.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        private float _health, _maxHealth = 1f;
        private int _magazine, _reserve;
        private bool _playerDead;

        private GUIStyle _style;

        private void OnEnable()
        {
            GameEvents.OnPlayerHealthChanged += HandleHealth;
            GameEvents.OnAmmoChanged += HandleAmmo;
            GameEvents.OnPlayerDied += HandlePlayerDied;
        }

        private void OnDisable()
        {
            GameEvents.OnPlayerHealthChanged -= HandleHealth;
            GameEvents.OnAmmoChanged -= HandleAmmo;
            GameEvents.OnPlayerDied -= HandlePlayerDied;
        }

        private void HandleHealth(float current, float max) { _health = current; _maxHealth = Mathf.Max(1f, max); }
        private void HandleAmmo(int magazine, int reserve) { _magazine = magazine; _reserve = reserve; }
        private void HandlePlayerDied() => _playerDead = true;

        private void OnGUI()
        {
            _style ??= new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold };

            int enemies = EnemyManager.Exists ? EnemyManager.Instance.ActiveCount : 0;
            GUI.Label(new Rect(16, 12, 400, 28), $"HP: {_health:0}/{_maxHealth:0}", _style);
            GUI.Label(new Rect(16, 40, 400, 28), $"Ammo: {_magazine} / {_reserve}", _style);
            GUI.Label(new Rect(16, 68, 400, 28), $"Enemies: {enemies}   FPS: {1f / Mathf.Max(0.0001f, Time.unscaledDeltaTime):0}", _style);

            if (_playerDead)
                GUI.Label(new Rect(Screen.width / 2 - 80, Screen.height / 2 - 20, 200, 40), "YOU DIED", _style);
        }
    }
}
