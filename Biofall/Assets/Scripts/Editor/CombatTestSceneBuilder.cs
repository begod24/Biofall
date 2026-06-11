using Biofall.Combat;
using Biofall.Core;
using Biofall.Enemies;
using Biofall.GameCamera;
using Biofall.Player;
using Biofall.Pooling;
using Biofall.UI;
using Biofall.Weapons;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Biofall.EditorTools
{
    /// <summary>
    /// One-click setup of the combat test rig in the CURRENT scene:
    /// managers, capsule player (move/aim/shoot), cube "pistol" with hitscan weapon,
    /// Sigma-Team-style top-down camera, ground (if missing), target dummies, and HUD.
    /// Re-runnable: skips anything that already exists.
    /// </summary>
    public static class CombatTestSceneBuilder
    {
        private const string PistolDataPath = "Assets/ScriptableObjects/Weapons/Pistol_Data.asset";

        [MenuItem("Biofall/Setup Combat Test Scene")]
        public static void Setup()
        {
            EnsureManager<GameManager>("GameManager");
            EnsureManager<PoolManager>("PoolManager");
            EnsureManager<EnemyManager>("EnemyManager");
            EnsureManager<HUDController>("HUD");

            EnsureGround();
            GameObject player = EnsurePlayer();
            EnsureAimLine(player);
            EnsureCamera(player.transform);
            EnsureTargetDummies();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("[Biofall] Combat test scene setup complete. Press Play: WASD move, mouse aim, LMB shoot, R reload.");
        }

        // ------------------------------------------------------------------ helpers

        private static T EnsureManager<T>(string name) where T : Component
        {
            var existing = Object.FindAnyObjectByType<T>();
            if (existing != null) return existing;

            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Biofall Setup");
            return go.AddComponent<T>();
        }

        private static void EnsureGround()
        {
            // If a raycast straight down finds any collider, the scene already has a floor.
            if (Physics.Raycast(new Vector3(0f, 50f, 0f), Vector3.down, 100f)) return;

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "TestGround";
            ground.transform.localScale = new Vector3(8f, 1f, 8f); // 80x80 m arena
            Undo.RegisterCreatedObjectUndo(ground, "Biofall Setup");
        }

        private static GameObject EnsurePlayer()
        {
            var existing = GameObject.FindWithTag("Player");
            if (existing != null) return existing;

            var player = new GameObject("Player") { tag = "Player" };
            player.transform.position = new Vector3(0f, 0.05f, 0f);
            Undo.RegisterCreatedObjectUndo(player, "Biofall Setup");

            var cc = player.AddComponent<CharacterController>();
            cc.height = 2f;
            cc.radius = 0.45f;
            cc.center = new Vector3(0f, 1f, 0f);

            player.AddComponent<PlayerInputReader>();
            player.AddComponent<PlayerMovement>();
            player.AddComponent<PlayerAim>();
            player.AddComponent<HealthComponent>();
            player.AddComponent<PlayerHealth>();

            // Visual capsule (CharacterController is the real collider).
            var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Visual";
            Object.DestroyImmediate(visual.GetComponent<Collider>());
            visual.transform.SetParent(player.transform, false);
            visual.transform.localPosition = new Vector3(0f, 1f, 0f);

            // Cube "pistol" held at the right hand, pointing forward.
            var pistol = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pistol.name = "Pistol";
            Object.DestroyImmediate(pistol.GetComponent<Collider>());
            pistol.transform.SetParent(player.transform, false);
            pistol.transform.localPosition = new Vector3(0.35f, 1.1f, 0.45f);
            pistol.transform.localScale = new Vector3(0.15f, 0.2f, 0.5f);

            // Muzzle parented to the player (not the scaled cube) so forward stays clean.
            var muzzle = new GameObject("Muzzle").transform;
            muzzle.SetParent(player.transform, false);
            muzzle.localPosition = new Vector3(0.35f, 1.1f, 0.8f);

            var weapon = pistol.AddComponent<HitscanWeapon>(); // RequireComponent adds AudioSource
            var weaponSo = new SerializedObject(weapon);
            weaponSo.FindProperty("_data").objectReferenceValue = EnsurePistolData();
            weaponSo.FindProperty("_muzzle").objectReferenceValue = muzzle;
            weaponSo.ApplyModifiedPropertiesWithoutUndo();

            var controller = player.AddComponent<WeaponController>();
            var controllerSo = new SerializedObject(controller);
            controllerSo.FindProperty("_equippedWeapon").objectReferenceValue = weapon;
            controllerSo.ApplyModifiedPropertiesWithoutUndo();

            return player;
        }

        private static void EnsureAimLine(GameObject player)
        {
            if (player.GetComponentInChildren<WeaponAimLine>() != null) return;

            var weapon = player.GetComponentInChildren<HitscanWeapon>();
            if (weapon == null) return;

            // Reuse the weapon's muzzle if it was wired; otherwise fall back to the weapon transform.
            var weaponSo = new SerializedObject(weapon);
            var muzzle = weaponSo.FindProperty("_muzzle").objectReferenceValue as Transform;
            if (muzzle == null) muzzle = weapon.transform;

            // Own un-scaled object (LineRenderer width is affected by transform scale).
            var aimLineGo = new GameObject("AimLine");
            aimLineGo.transform.SetParent(player.transform, false);
            Undo.RegisterCreatedObjectUndo(aimLineGo, "Biofall Setup");
            aimLineGo.AddComponent<LineRenderer>();

            var aimLine = aimLineGo.AddComponent<WeaponAimLine>();
            var aimSo = new SerializedObject(aimLine);
            aimSo.FindProperty("_muzzle").objectReferenceValue = muzzle;
            aimSo.FindProperty("_weapon").objectReferenceValue = weapon;
            aimSo.ApplyModifiedPropertiesWithoutUndo();
        }

        private static WeaponData EnsurePistolData()
        {
            var data = AssetDatabase.LoadAssetAtPath<WeaponData>(PistolDataPath);
            if (data != null) return data;

            if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects"))
                AssetDatabase.CreateFolder("Assets", "ScriptableObjects");
            if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects/Weapons"))
                AssetDatabase.CreateFolder("Assets/ScriptableObjects", "Weapons");

            data = ScriptableObject.CreateInstance<WeaponData>();
            data.WeaponName = "Pistol";
            data.FireType = WeaponFireType.Hitscan;
            data.Damage = 20f;
            data.FireRate = 360f;
            data.PelletsPerShot = 1;
            data.SpreadDegrees = 1f;
            data.Range = 50f;
            data.MagazineSize = 12;
            data.ReserveAmmo = 96;
            data.ReloadTime = 1.2f;

            AssetDatabase.CreateAsset(data, PistolDataPath);
            AssetDatabase.SaveAssets();
            return data;
        }

        private static void EnsureCamera(Transform player)
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
                Undo.RegisterCreatedObjectUndo(go, "Biofall Setup");
            }

            var rig = cam.GetComponent<TopDownCameraRig>();
            if (rig == null) rig = cam.gameObject.AddComponent<TopDownCameraRig>();

            var rigSo = new SerializedObject(rig);
            rigSo.FindProperty("_target").objectReferenceValue = player;
            // Sigma-Team Zombie Shooter feel: high, mostly top-down, slight tilt back.
            rigSo.FindProperty("_offset").vector3Value = new Vector3(0f, 14f, -6f);
            rigSo.ApplyModifiedPropertiesWithoutUndo();

            // Pose it immediately so the edit-mode view matches play mode.
            cam.transform.position = player.position + new Vector3(0f, 14f, -6f);
            cam.transform.LookAt(player.position);
        }

        private static void EnsureTargetDummies()
        {
            if (GameObject.Find("TargetDummy_1") != null) return;

            Vector3[] spots =
            {
                new Vector3(5f, 1f, 6f),
                new Vector3(-6f, 1f, 4f),
                new Vector3(0f, 1f, 9f),
            };

            for (int i = 0; i < spots.Length; i++)
            {
                var dummy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                dummy.name = $"TargetDummy_{i + 1}";
                dummy.transform.position = spots[i];
                dummy.AddComponent<HealthComponent>().Configure(60f);
                dummy.AddComponent<TargetDummy>();
                Undo.RegisterCreatedObjectUndo(dummy, "Biofall Setup");
            }
        }
    }
}
