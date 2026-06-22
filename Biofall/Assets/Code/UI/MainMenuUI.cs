using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using Biofall.Core;
using Biofall.Net;

namespace Biofall.UI
{
    public sealed class MainMenuUI : MonoBehaviour
    {
        [Header("Root buttons")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button creditsButton;
        [SerializeField] private Button exitButton;

        [Header("Panels")]
        [SerializeField] private GameObject playPanel;
        [SerializeField] private GameObject campaignModePanel;
        [SerializeField] private GameObject coopConnectPanel;
        [SerializeField] private GameObject coopBrowserPanel;
        [SerializeField] private GameObject coopLobbyPanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject creditsPanel;

        [Header("Play panel")]
        [SerializeField] private Button campaignButton;
        [SerializeField] private Button waveModeButton;
        [SerializeField] private Button playBackButton;

        [Header("Campaign mode panel")]
        [SerializeField] private Button soloButton;
        [SerializeField] private Button coopButton;
        [SerializeField] private Button campaignModeBackButton;

        [Header("Co-op — networking")]
        [Tooltip("NetworkRoot prefab (NetworkManager + UnityTransport + NetworkBootstrap + LanDiscovery). " +
                 "Instantiated on demand the first time the player chooses Host/Join, so solo never creates it.")]
        [SerializeField] private GameObject networkRootPrefab;

        [Header("Co-op connect panel (Host / Find)")]
        [SerializeField] private Button hostButton;
        [SerializeField] private Button findButton;
        [SerializeField] private Button connectBackButton;

        [Header("Co-op browser panel (LAN hosts)")]
        [SerializeField] private Button browserRefreshButton;
        [SerializeField] private Button browserBackButton;
        [Tooltip("Fixed row buttons (HostRow1..N) populated from LAN discovery; extras hidden.")]
        [SerializeField] private Button[] hostRows;

        [Header("Co-op lobby panel")]
        [SerializeField] private Button lobbyReadyButton;
        [SerializeField] private Button lobbyStartButton;
        [SerializeField] private Button lobbyBackButton;
        [SerializeField] private Button[] operatorButtons;
        [SerializeField] private GameObject[] operatorSelectedFrames;
        [SerializeField] private TMP_Text[] operatorStatusLabels;
        [SerializeField] private TMP_Text[] squadStatusLabels;
        [SerializeField] private Image[] squadStatusDots;

        [Header("Settings panel")]
        [SerializeField] private TMP_Dropdown resolutionDropdown;
        [SerializeField] private TMP_Dropdown fullscreenDropdown;
        [SerializeField] private Button applyDisplayButton;
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider shakeSlider;
        [SerializeField] private Button settingsBackButton;

        [Header("Credits panel")]
        [SerializeField] private Button creditsBackButton;

        private static readonly Color ReadyColor = new Color(0.30f, 0.92f, 0.46f, 1f);
        private static readonly Color ConnectedColor = new Color(0.95f, 0.78f, 0.20f, 1f);
        private static readonly Color EmptyColor = new Color(0.35f, 0.30f, 0.32f, 1f);

        private List<Resolution> _resolutions;
        private int _selectedOperator;
        private readonly List<GameObject> _rootMenuObjects = new();
        private readonly List<DiscoveredHost> _shownHosts = new();

        private void Awake()
        {
            Time.timeScale = 1f;
            UiOverlay.Active = false;
            Cursor.visible = true;

            CollectRootMenuObjects();
            CloseAllPanels();

            Wire(playButton, () => Show(playPanel));
            Wire(continueButton, () => SceneManager.LoadScene(GameScenes.Gameplay));
            Wire(settingsButton, () => Show(settingsPanel));
            Wire(creditsButton, () => Show(creditsPanel));
            Wire(exitButton, Quit);

            Wire(campaignButton, () => Show(campaignModePanel));
            Wire(waveModeButton, () => SceneManager.LoadScene(GameScenes.WaveMode));
            Wire(playBackButton, CloseAllPanels);
            Wire(soloButton, () => SceneManager.LoadScene(GameScenes.Gameplay));
            Wire(coopButton, OpenCoopConnect);
            Wire(campaignModeBackButton, () => Show(playPanel));

            Wire(hostButton, HostGame);
            Wire(findButton, OpenBrowser);
            Wire(connectBackButton, CoopBackToCampaign);

            Wire(browserRefreshButton, RefreshHosts);
            Wire(browserBackButton, () => Show(coopConnectPanel));
            WireHostRows();

            Wire(lobbyReadyButton, ToggleReady);
            Wire(lobbyStartButton, StartGame);
            Wire(lobbyBackButton, LeaveLobby);

            Wire(settingsBackButton, CloseAllPanels);
            Wire(creditsBackButton, CloseAllPanels);

            WireOperatorButtons();
            SelectOperator(0);

            SetupDisplayDropdowns();
            Wire(applyDisplayButton, ApplyDisplay);

            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.SetValueWithoutNotify(GameSettings.MasterVolume);
                masterVolumeSlider.onValueChanged.AddListener(GameSettings.SetMasterVolume);
            }
            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.SetValueWithoutNotify(GameSettings.MusicVolume);
                musicVolumeSlider.onValueChanged.AddListener(GameSettings.SetMusicVolume);
            }
            if (shakeSlider != null)
            {
                shakeSlider.SetValueWithoutNotify(GameSettings.CameraShakeIntensity);
                shakeSlider.onValueChanged.AddListener(GameSettings.SetCameraShakeIntensity);
            }
        }

        private void Update()
        {
            if (coopBrowserPanel != null && coopBrowserPanel.activeSelf) RefreshHostRows();
            if (coopLobbyPanel != null && coopLobbyPanel.activeSelf) RefreshLobby();
        }

        private static void Wire(Button b, UnityEngine.Events.UnityAction action)
        {
            if (b != null) b.onClick.AddListener(action);
        }

        private void Show(GameObject panel)
        {
            CloseAllPanels();
            SetRootMenuVisible(false);
            if (panel != null) panel.SetActive(true);
        }

        private void CloseAllPanels()
        {
            if (playPanel != null) playPanel.SetActive(false);
            if (campaignModePanel != null) campaignModePanel.SetActive(false);
            if (coopConnectPanel != null) coopConnectPanel.SetActive(false);
            if (coopBrowserPanel != null) coopBrowserPanel.SetActive(false);
            if (coopLobbyPanel != null) coopLobbyPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (creditsPanel != null) creditsPanel.SetActive(false);
            SetRootMenuVisible(true);
        }

        private void OpenCoopConnect()
        {
            EnsureNetworkRoot();
            Show(coopConnectPanel);
        }

        private void EnsureNetworkRoot()
        {
            if (NetworkBootstrap.Instance != null) return;
            if (networkRootPrefab == null)
            {
                Debug.LogError("[MainMenuUI] networkRootPrefab not assigned — co-op cannot start.");
                return;
            }
            Instantiate(networkRootPrefab);
        }

        private void HostGame()
        {
            EnsureNetworkRoot();
            if (NetworkBootstrap.Instance != null && NetworkBootstrap.Instance.StartHost())
                Show(coopLobbyPanel);
            else
                Debug.LogWarning("[MainMenuUI] StartHost failed.");
        }

        private void OpenBrowser()
        {
            EnsureNetworkRoot();
            var d = NetworkBootstrap.Instance != null ? NetworkBootstrap.Instance.Discovery : null;
            if (d != null)
            {
                d.ClearHosts();
                d.StartListening();
                d.RefreshHosts();
            }
            _shownHosts.Clear();
            Show(coopBrowserPanel);
        }

        private void RefreshHosts()
        {
            var d = NetworkBootstrap.Instance != null ? NetworkBootstrap.Instance.Discovery : null;
            if (d == null) return;
            d.ClearHosts();
            d.RefreshHosts();
        }

        private void WireHostRows()
        {
            if (hostRows == null) return;
            for (int i = 0; i < hostRows.Length; i++)
            {
                int index = i;
                if (hostRows[i] != null) hostRows[i].gameObject.SetActive(false);
                Wire(hostRows[i], () => JoinHostRow(index));
            }
        }

        private void RefreshHostRows()
        {
            var d = NetworkBootstrap.Instance != null ? NetworkBootstrap.Instance.Discovery : null;
            _shownHosts.Clear();
            if (d != null)
            {
                var hosts = d.Hosts;
                int max = hostRows != null ? hostRows.Length : 0;
                for (int i = 0; i < hosts.Count && i < max; i++) _shownHosts.Add(hosts[i]);
            }

            if (hostRows == null) return;
            for (int i = 0; i < hostRows.Length; i++)
            {
                if (hostRows[i] == null) continue;
                bool has = i < _shownHosts.Count;
                hostRows[i].gameObject.SetActive(has);
                if (!has) continue;
                var label = hostRows[i].GetComponentInChildren<TMP_Text>();
                if (label != null) label.text = $"{_shownHosts[i].Name}    {_shownHosts[i].Address}";
            }
        }

        private void JoinHostRow(int index)
        {
            if (index < 0 || index >= _shownHosts.Count) return;
            EnsureNetworkRoot();
            if (NetworkBootstrap.Instance != null && NetworkBootstrap.Instance.StartClient(_shownHosts[index].Address))
                Show(coopLobbyPanel);
            else
                Debug.LogWarning("[MainMenuUI] StartClient failed.");
        }

        private void ToggleReady() => CoopSession.Instance?.ToggleReadyRpc();

        private void StartGame()
        {
            var s = CoopSession.Instance;
            if (s != null && NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost && s.AllReady())
                s.StartGame();
        }

        private void LeaveLobby()
        {
            ShutdownCoop();
            Show(coopConnectPanel);
        }

        private void CoopBackToCampaign()
        {
            ShutdownCoop();
            Show(campaignModePanel);
        }

        private void ShutdownCoop()
        {
            NetworkBootstrap.Instance?.Shutdown();
            _shownHosts.Clear();
        }

        private void RefreshLobby()
        {
            var nm = NetworkManager.Singleton;
            var s = CoopSession.Instance;
            bool isHost = nm != null && nm.IsHost;

            int count = (s != null && s.Slots != null) ? s.Slots.Count : 0;
            for (int i = 0; squadStatusLabels != null && i < squadStatusLabels.Length; i++)
            {
                if (squadStatusLabels[i] == null) continue;
                if (i < count)
                {
                    var slot = s.Slots[i];
                    bool me = nm != null && slot.ClientId == nm.LocalClientId;
                    squadStatusLabels[i].text = $"P{i + 1}{(me ? " (YOU)" : "")}  {(slot.Ready ? "READY" : "...")}";
                }
                else squadStatusLabels[i].text = $"P{i + 1}  EMPTY";
            }
            for (int i = 0; squadStatusDots != null && i < squadStatusDots.Length; i++)
            {
                if (squadStatusDots[i] == null) continue;
                if (i >= count) squadStatusDots[i].color = EmptyColor;
                else squadStatusDots[i].color = s.Slots[i].Ready ? ReadyColor : ConnectedColor;
            }

            if (lobbyReadyButton != null)
            {
                bool localReady = false;
                if (s != null && s.Slots != null && nm != null)
                    for (int i = 0; i < s.Slots.Count; i++)
                        if (s.Slots[i].ClientId == nm.LocalClientId) { localReady = s.Slots[i].Ready; break; }
                var t = lobbyReadyButton.GetComponentInChildren<TMP_Text>();
                if (t != null) t.text = localReady ? "READY ✓" : "READY UP";
            }

            if (lobbyStartButton != null)
            {
                lobbyStartButton.gameObject.SetActive(isHost);
                lobbyStartButton.interactable = isHost && s != null && s.AllReady();
            }
        }

        private void CollectRootMenuObjects()
        {
            _rootMenuObjects.Clear();

            AddRootMenuObject(transform.Find("MainMenuOuterFrame")?.gameObject);
            AddRootMenuObject(transform.Find("RootMenuFrame")?.gameObject);
            AddRootMenuObject(transform.Find("Title")?.gameObject);
            AddRootMenuObject(transform.Find("Subtitle")?.gameObject);
            AddRootMenuObject(playButton != null ? playButton.gameObject : null);
            AddRootMenuObject(continueButton != null ? continueButton.gameObject : null);
            AddRootMenuObject(settingsButton != null ? settingsButton.gameObject : null);
            AddRootMenuObject(creditsButton != null ? creditsButton.gameObject : null);
            AddRootMenuObject(exitButton != null ? exitButton.gameObject : null);
        }

        private void AddRootMenuObject(GameObject target)
        {
            if (target != null && !_rootMenuObjects.Contains(target))
                _rootMenuObjects.Add(target);
        }

        private void SetRootMenuVisible(bool visible)
        {
            for (int i = 0; i < _rootMenuObjects.Count; i++)
                if (_rootMenuObjects[i] != null)
                    _rootMenuObjects[i].SetActive(visible);
        }

        private void WireOperatorButtons()
        {
            if (operatorButtons == null) return;

            for (int i = 0; i < operatorButtons.Length; i++)
            {
                int index = i;
                Wire(operatorButtons[i], () => SelectOperator(index));
            }
        }

        private void SelectOperator(int index)
        {
            _selectedOperator = index;

            if (operatorSelectedFrames != null)
            {
                for (int i = 0; i < operatorSelectedFrames.Length; i++)
                    if (operatorSelectedFrames[i] != null)
                        operatorSelectedFrames[i].SetActive(i == _selectedOperator);
            }

            if (operatorStatusLabels != null)
            {
                for (int i = 0; i < operatorStatusLabels.Length; i++)
                    if (operatorStatusLabels[i] != null)
                        operatorStatusLabels[i].text = i == _selectedOperator ? "SELECTED" : "AVAILABLE";
            }
        }

        private void SetupDisplayDropdowns()
        {
            if (resolutionDropdown != null)
            {
                _resolutions = new List<Resolution>();
                var options = new List<string>();
                var seen = new HashSet<string>();
                foreach (var r in Screen.resolutions)
                {
                    string key = r.width + " x " + r.height;
                    if (!seen.Add(key)) continue;
                    _resolutions.Add(r);
                    options.Add(key);
                }
                int current = 0;
                for (int i = 0; i < _resolutions.Count; i++)
                    if (_resolutions[i].width == Screen.width && _resolutions[i].height == Screen.height) current = i;

                resolutionDropdown.ClearOptions();
                resolutionDropdown.AddOptions(options);
                resolutionDropdown.SetValueWithoutNotify(current);
                resolutionDropdown.RefreshShownValue();
            }

            if (fullscreenDropdown != null)
            {
                fullscreenDropdown.ClearOptions();
                fullscreenDropdown.AddOptions(new List<string> { "Fullscreen", "Windowed", "Borderless" });
                fullscreenDropdown.SetValueWithoutNotify(ModeToIndex(Screen.fullScreenMode));
                fullscreenDropdown.RefreshShownValue();
            }
        }

        private void ApplyDisplay()
        {
            if (_resolutions == null || _resolutions.Count == 0 ||
                resolutionDropdown == null || fullscreenDropdown == null) return;

            Resolution r = _resolutions[Mathf.Clamp(resolutionDropdown.value, 0, _resolutions.Count - 1)];
            GameSettings.ApplyDisplay(r.width, r.height, IndexToMode(fullscreenDropdown.value));
        }

        private static int ModeToIndex(FullScreenMode mode) => mode switch
        {
            FullScreenMode.ExclusiveFullScreen => 0,
            FullScreenMode.Windowed => 1,
            _ => 2,
        };

        private static FullScreenMode IndexToMode(int index) => index switch
        {
            0 => FullScreenMode.ExclusiveFullScreen,
            1 => FullScreenMode.Windowed,
            _ => FullScreenMode.FullScreenWindow,
        };

        private void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
