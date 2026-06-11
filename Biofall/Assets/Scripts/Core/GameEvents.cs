using System;
using UnityEngine;

namespace Biofall.Core
{
    /// <summary>
    /// Central, decoupled event hub (Observer pattern). Systems raise events here and
    /// UI/audio/other systems subscribe — nothing polls gameplay objects every frame.
    ///
    /// Subscribers MUST unsubscribe (typically in OnDisable/OnDestroy) to avoid leaks
    /// and calls into destroyed objects, since these are static events that outlive scenes.
    /// </summary>
    public static class GameEvents
    {
        // --- Game flow ---
        public static event Action<GameState> OnGameStateChanged;

        // --- Player ---
        public static event Action OnPlayerDied;
        public static event Action<float, float> OnPlayerHealthChanged; // current, max

        // --- Weapons ---
        public static event Action OnWeaponFired;
        public static event Action<int, int> OnAmmoChanged; // magazine, reserve

        // --- Enemies ---
        public static event Action<Vector3> OnEnemyKilled; // world position of the kill

        // --- Objectives / mission ---
        public static event Action OnObjectiveCompleted;
        public static event Action OnExtractionStarted;
        public static event Action<bool> OnMissionEnded; // success?

        public static void RaiseGameStateChanged(GameState state) => OnGameStateChanged?.Invoke(state);
        public static void RaisePlayerDied() => OnPlayerDied?.Invoke();
        public static void RaisePlayerHealthChanged(float current, float max) => OnPlayerHealthChanged?.Invoke(current, max);
        public static void RaiseWeaponFired() => OnWeaponFired?.Invoke();
        public static void RaiseAmmoChanged(int magazine, int reserve) => OnAmmoChanged?.Invoke(magazine, reserve);
        public static void RaiseEnemyKilled(Vector3 position) => OnEnemyKilled?.Invoke(position);
        public static void RaiseObjectiveCompleted() => OnObjectiveCompleted?.Invoke();
        public static void RaiseExtractionStarted() => OnExtractionStarted?.Invoke();
        public static void RaiseMissionEnded(bool success) => OnMissionEnded?.Invoke(success);
    }
}
