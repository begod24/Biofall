#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Biofall.UI;

namespace Biofall.EditorTools
{
    public static class BiofallMenuFX
    {
        const string ScenePath  = "Assets/Scenes/MainMenu.unity";
        const string MatDir     = "Assets/VFX/Materials";
        const string MatPath    = MatDir + "/MAT_CRTOverlay.mat";
        const string ShaderName = "Biofall/CRTOverlay";

        [MenuItem("Tools/Biofall/Setup Menu FX")]
        public static void Setup()
        {
            var shader = Shader.Find(ShaderName);
            if (shader == null) { Debug.LogError("[MenuFX] shader not found: " + ShaderName); return; }

            var mat = AssetDatabase.LoadAssetAtPath<Material>(MatPath);
            if (mat == null)
            {
                if (!AssetDatabase.IsValidFolder(MatDir)) AssetDatabase.CreateFolder("Assets/VFX", "Materials");
                mat = new Material(shader) { name = "MAT_CRTOverlay" };
                AssetDatabase.CreateAsset(mat, MatPath);
                AssetDatabase.SaveAssets();
            }
            else { mat.shader = shader; }

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                GameObject title = null;
                Canvas canvas = null;
                foreach (var root in scene.GetRootGameObjects())
                {
                    var t = FindByName(root.transform, "Title");
                    if (t != null) { title = t.gameObject; canvas = t.GetComponentInParent<Canvas>(); break; }
                }

                if (title != null)
                {
                    if (title.GetComponent<TitleGlowPulse>() == null) title.AddComponent<TitleGlowPulse>();
                    var shadow = title.GetComponent<Shadow>();
                    if (shadow != null) Object.DestroyImmediate(shadow);
                }

                if (canvas != null && canvas.transform.Find("CRT_Overlay") == null)
                {
                    var go = new GameObject("CRT_Overlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
                    go.transform.SetParent(canvas.transform, false);
                    var rt = (RectTransform)go.transform;
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;
                    var ri = go.GetComponent<RawImage>();
                    ri.material = mat;
                    ri.color = Color.white;
                    ri.raycastTarget = false;
                    go.transform.SetAsLastSibling();
                }

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[MenuFX] done. title={title != null} canvas={canvas != null}");
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        static Transform FindByName(Transform root, string name)
        {
            if (root.name == name) return root;
            foreach (Transform c in root)
            {
                var r = FindByName(c, name);
                if (r != null) return r;
            }
            return null;
        }
    }
}
#endif
