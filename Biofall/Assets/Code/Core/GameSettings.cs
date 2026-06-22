using System;
using UnityEngine;

namespace Biofall.Core
{
    public static class GameSettings
    {
        private const string VolumeKey = "bf_master_volume";
        private const string MusicKey = "bf_music_volume";
        private const string ShakeKey = "bf_camera_shake";
        private const string ShakeIntensityKey = "bf_shake_intensity";
        private const string ResWKey = "bf_res_w";
        private const string ResHKey = "bf_res_h";
        private const string FsModeKey = "bf_fs_mode";

        private static float _volume = 1f;
        private static float _music = 1f;
        private static float _shake = 1f;
        private static bool _loaded;

        public static event Action MusicVolumeChanged;

        public static float MasterVolume { get { EnsureLoaded(); return _volume; } }
        public static float MusicVolume { get { EnsureLoaded(); return _music; } }
        public static float CameraShakeIntensity { get { EnsureLoaded(); return _shake; } }
        public static bool CameraShakeEnabled { get { EnsureLoaded(); return _shake > 0f; } }

        private static void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;
            _volume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, 1f));
            _music = Mathf.Clamp01(PlayerPrefs.GetFloat(MusicKey, 1f));
            float defaultShake = PlayerPrefs.GetInt(ShakeKey, 1) == 1 ? 1f : 0f;
            _shake = Mathf.Clamp01(PlayerPrefs.GetFloat(ShakeIntensityKey, defaultShake));
            AudioListener.volume = _volume;
            ApplySavedDisplay();
        }

        public static void SetMasterVolume(float value)
        {
            EnsureLoaded();
            _volume = Mathf.Clamp01(value);
            AudioListener.volume = _volume;
            PlayerPrefs.SetFloat(VolumeKey, _volume);
            PlayerPrefs.Save();
        }

        public static void SetMusicVolume(float value)
        {
            EnsureLoaded();
            _music = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(MusicKey, _music);
            PlayerPrefs.Save();
            MusicVolumeChanged?.Invoke();
        }

        public static void SetCameraShakeIntensity(float value)
        {
            EnsureLoaded();
            _shake = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(ShakeIntensityKey, _shake);
            PlayerPrefs.SetInt(ShakeKey, _shake > 0f ? 1 : 0);
            PlayerPrefs.Save();
        }

        public static void ApplyDisplay(int width, int height, FullScreenMode mode)
        {
            EnsureLoaded();
            width = Mathf.Max(640, width);
            height = Mathf.Max(480, height);
            Screen.SetResolution(width, height, mode);
            PlayerPrefs.SetInt(ResWKey, width);
            PlayerPrefs.SetInt(ResHKey, height);
            PlayerPrefs.SetInt(FsModeKey, (int)mode);
            PlayerPrefs.Save();
        }

        private static void ApplySavedDisplay()
        {
            if (!PlayerPrefs.HasKey(ResWKey)) return;
            int w = PlayerPrefs.GetInt(ResWKey, Screen.width);
            int h = PlayerPrefs.GetInt(ResHKey, Screen.height);
            var mode = (FullScreenMode)PlayerPrefs.GetInt(FsModeKey, (int)Screen.fullScreenMode);
            Screen.SetResolution(w, h, mode);
        }
    }
}
