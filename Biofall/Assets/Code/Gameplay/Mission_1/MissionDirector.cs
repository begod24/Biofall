using System.Collections;
using UnityEngine;
using Biofall.Core;
using Biofall.Net;

namespace Biofall.Gameplay.Mission1
{
    public sealed class MissionDirector : MonoBehaviour
    {
        [Header("Defense waves")]
        [Tooltip("SOLO spawner driven during the beacon defense (its SpawnNow batch = one wave).")]
        [SerializeField] private EnemySpawner defenseSpawner;
        [Tooltip("CO-OP server spawner driven during defense (networked horde). Used when in a co-op session.")]
        [SerializeField] private CoopEnemySpawner coopDefenseSpawner;
        [Tooltip("Seconds between defense waves while the beacon charges.")]
        [SerializeField] private float waveInterval = 8f;
        [Tooltip("First defense wave fires this long after the beacon switches on.")]
        [SerializeField] private float firstWaveDelay = 2f;

        private MissionPhase _phase = MissionPhase.FindGenerator;
        private Coroutine _waveLoop;
        private bool _ended;

        public MissionPhase Phase => _phase;

        private void OnEnable()
        {
            if (NetSession.InCoop && !NetSession.IsServer) { enabled = false; return; }

            EventBus.Subscribe<GeneratorActivated>(OnGeneratorActivated);
            EventBus.Subscribe<BeaconActivated>(OnBeaconActivated);
            EventBus.Subscribe<BeaconCharged>(OnBeaconCharged);
            EventBus.Subscribe<MissionCompleted>(OnMissionCompleted);
            EventBus.Subscribe<PlayerDied>(OnPlayerDied);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<GeneratorActivated>(OnGeneratorActivated);
            EventBus.Unsubscribe<BeaconActivated>(OnBeaconActivated);
            EventBus.Unsubscribe<BeaconCharged>(OnBeaconCharged);
            EventBus.Unsubscribe<MissionCompleted>(OnMissionCompleted);
            EventBus.Unsubscribe<PlayerDied>(OnPlayerDied);
        }

        private void Start()
        {
            SetPhase(MissionPhase.FindGenerator);
        }

        private void OnGeneratorActivated(GeneratorActivated _) => SetPhase(MissionPhase.ActivateBeacon);

        private void OnBeaconActivated(BeaconActivated _)
        {
            SetPhase(MissionPhase.DefendBeacon);
            _waveLoop = StartCoroutine(WaveLoop());
        }

        private void OnBeaconCharged(BeaconCharged _)
        {
            StopWaves();
            SetPhase(MissionPhase.Extract);
        }

        private void OnMissionCompleted(MissionCompleted _)
        {
            _ended = true;
            StopWaves();
            SetPhase(MissionPhase.Completed);
        }

        private void OnPlayerDied(PlayerDied _)
        {
            if (NetSession.InCoop) return;

            _ended = true;
            StopWaves();
        }

        private void SetPhase(MissionPhase phase)
        {
            _phase = phase;
            EventBus.Publish(new MissionPhaseChanged(phase));
        }

        private IEnumerator WaveLoop()
        {
            yield return new WaitForSeconds(firstWaveDelay);
            while (!_ended)
            {
                if (NetSession.InCoop)
                {
                    if (coopDefenseSpawner != null) coopDefenseSpawner.SpawnWaveNow();
                }
                else if (defenseSpawner != null)
                {
                    defenseSpawner.SpawnNow();
                }
                yield return new WaitForSeconds(waveInterval);
            }
        }

        private void StopWaves()
        {
            if (_waveLoop != null)
            {
                StopCoroutine(_waveLoop);
                _waveLoop = null;
            }
        }
    }
}
