#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Biofall.UI;

namespace Biofall.EditorTools
{
    public static class BiofallSettingsUI
    {
        const string ScenePath = "Assets/Scenes/MainMenu.unity";

        static readonly Color Maroon     = new Color(0.42f, 0.06f, 0.12f, 1f);
        static readonly Color MaroonDark = new Color(0.16f, 0.02f, 0.05f, 0.95f);
        static readonly Color Red        = new Color(0.66f, 0.10f, 0.15f, 1f);
        static readonly Color Light      = new Color(0.96f, 0.90f, 0.90f, 1f);

        static DefaultControls.Resources _ui;
        static TMP_DefaultControls.Resources _tmp;

        [MenuItem("Tools/Biofall/Setup Settings UI")]
        public static void Setup()
        {
            BuildResources();

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                MainMenuUI menu = null;
                foreach (var root in scene.GetRootGameObjects())
                {
                    menu = root.GetComponentInChildren<MainMenuUI>(true);
                    if (menu != null) break;
                }
                if (menu == null) { Debug.LogError("[SettingsUI] MainMenuUI not found"); return; }

                var so = new SerializedObject(menu);
                var settingsPanel = so.FindProperty("settingsPanel").objectReferenceValue as GameObject;
                var creditsPanel = so.FindProperty("creditsPanel").objectReferenceValue as GameObject;
                if (settingsPanel == null) { Debug.LogError("[SettingsUI] settingsPanel not assigned"); return; }

                BuildSettings(settingsPanel.transform, so);
                so.ApplyModifiedPropertiesWithoutUndo();

                if (creditsPanel != null) AddSigmaTeam(creditsPanel.transform);

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log("[SettingsUI] done.");
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        static void BuildSettings(Transform panel, SerializedObject menuSo)
        {
            for (int i = panel.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(panel.GetChild(i).gameObject);

            var list = NewRect("OptionsList", panel);
            Stretch(list, 24, 24, 24, 24);
            var v = list.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = 12;
            v.padding = new RectOffset(16, 16, 16, 16);
            v.childAlignment = TextAnchor.UpperCenter;
            v.childControlWidth = true; v.childForceExpandWidth = true;
            v.childControlHeight = true; v.childForceExpandHeight = false;

            var header = MakeLabel(list, "SETTINGS", 48, TextAlignmentOptions.Center);
            header.fontStyle = FontStyles.Bold;
            Le(header.gameObject, -1, 64);

            var resDd  = AddDropdownRow(list, "RESOLUTION");
            var fsDd   = AddDropdownRow(list, "DISPLAY MODE");
            var apply  = AddButtonRow(list, "APPLY");
            var master = AddSliderRow(list, "MASTER VOLUME");
            var music  = AddSliderRow(list, "MUSIC VOLUME");
            var shake  = AddSliderRow(list, "CAMERA SHAKE");
            var back   = AddButtonRow(list, "BACK");

            Set(menuSo, "resolutionDropdown", resDd);
            Set(menuSo, "fullscreenDropdown", fsDd);
            Set(menuSo, "applyDisplayButton", apply);
            Set(menuSo, "masterVolumeSlider", master);
            Set(menuSo, "musicVolumeSlider", music);
            Set(menuSo, "shakeSlider", shake);
            Set(menuSo, "settingsBackButton", back);
        }

        static RectTransform MakeRow(Transform parent, float height, TextAnchor align)
        {
            var row = NewRect("Row", parent);
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 16;
            h.childAlignment = align;
            h.childControlWidth = true; h.childForceExpandWidth = false;
            h.childControlHeight = true; h.childForceExpandHeight = false;
            Le(row.gameObject, -1, height);
            return row;
        }

        static TMP_Dropdown AddDropdownRow(Transform list, string label)
        {
            var row = MakeRow(list, 44, TextAnchor.MiddleLeft);
            var lbl = MakeLabel(row, label, 26, TextAlignmentOptions.MidlineLeft);
            Le(lbl.gameObject, 180, 40, 1f);

            var ddGo = TMP_DefaultControls.CreateDropdown(_tmp);
            ddGo.name = "Dropdown";
            ddGo.transform.SetParent(row, false);
            Le(ddGo, 320, 40, 0f);
            Img(ddGo, Maroon);
            var dd = ddGo.GetComponent<TMP_Dropdown>();
            if (dd.captionText != null) dd.captionText.color = Light;
            var itemLbl = ddGo.transform.Find("Template/Viewport/Content/Item/Item Label");
            if (itemLbl != null) itemLbl.GetComponent<TMP_Text>().color = Light;
            var tmplImg = ddGo.transform.Find("Template");
            if (tmplImg != null) { var im = tmplImg.GetComponent<Image>(); if (im != null) im.color = MaroonDark; }
            return dd;
        }

        static Slider AddSliderRow(Transform list, string label)
        {
            var row = MakeRow(list, 40, TextAnchor.MiddleLeft);
            var lbl = MakeLabel(row, label, 26, TextAlignmentOptions.MidlineLeft);
            Le(lbl.gameObject, 180, 36, 1f);

            var sGo = DefaultControls.CreateSlider(_ui);
            sGo.name = "Slider";
            sGo.transform.SetParent(row, false);
            Le(sGo, 320, 24, 0f);
            var slider = sGo.GetComponent<Slider>();
            slider.minValue = 0f; slider.maxValue = 1f; slider.wholeNumbers = false; slider.value = 1f;
            Tint(sGo, "Background", MaroonDark);
            Tint(sGo, "Fill Area/Fill", Red);
            Tint(sGo, "Handle Slide Area/Handle", Light);
            return slider;
        }

        static Button AddButtonRow(Transform list, string label)
        {
            var row = MakeRow(list, 46, TextAnchor.MiddleCenter);
            var bGo = TMP_DefaultControls.CreateButton(_tmp);
            bGo.name = label + "Button";
            bGo.transform.SetParent(row, false);
            Le(bGo, 240, 44, 0f);
            Img(bGo, Maroon);
            var txt = bGo.GetComponentInChildren<TMP_Text>();
            if (txt != null) { txt.text = label; txt.color = Light; txt.fontSize = 26; }
            return bGo.GetComponent<Button>();
        }

        static void AddSigmaTeam(Transform creditsPanel)
        {
            var texts = creditsPanel.GetComponentsInChildren<TMP_Text>(true);
            foreach (var t in texts)
            {
                if (t.text != null && t.text.Contains("Bekbolat"))
                {
                    if (!t.text.Contains("SIGMA TEAM")) t.text = t.text.TrimEnd() + "\nSIGMA TEAM";
                    return;
                }
            }
            var lbl = MakeLabel(creditsPanel, "SIGMA TEAM", 32, TextAlignmentOptions.Center);
            Stretch((RectTransform)lbl.transform, 0, 0, 0, 0);
        }

        static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        static void Stretch(RectTransform rt, float l, float r, float t, float b)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(l, b); rt.offsetMax = new Vector2(-r, -t);
        }

        static TMP_Text MakeLabel(Transform parent, string text, float size, TextAlignmentOptions align)
        {
            var go = new GameObject("Label", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = text; t.fontSize = size; t.color = Light; t.alignment = align;
            t.raycastTarget = false;
            return t;
        }

        static void Le(GameObject go, float preferredWidth, float minHeight, float flexibleWidth = -1f)
        {
            var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
            if (preferredWidth > 0) { le.preferredWidth = preferredWidth; le.minWidth = preferredWidth; }
            if (minHeight > 0) { le.minHeight = minHeight; le.preferredHeight = minHeight; }
            if (flexibleWidth >= 0) le.flexibleWidth = flexibleWidth;
        }

        static void Img(GameObject go, Color c) { var i = go.GetComponent<Image>(); if (i != null) i.color = c; }

        static void Tint(GameObject root, string path, Color c)
        {
            var t = root.transform.Find(path);
            if (t != null) { var i = t.GetComponent<Image>(); if (i != null) i.color = c; }
        }

        static void Set(SerializedObject so, string prop, Object value)
        {
            var p = so.FindProperty(prop);
            if (p != null) p.objectReferenceValue = value;
            else Debug.LogWarning("[SettingsUI] property not found: " + prop);
        }

        static void BuildResources()
        {
            Sprite Std    = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            Sprite Bg     = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            Sprite Input  = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/InputFieldBackground.psd");
            Sprite Knob   = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            Sprite Check   = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd");
            Sprite Arrow  = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/DropdownArrow.psd");
            Sprite Mask   = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UIMask.psd");

            _ui = new DefaultControls.Resources
            {
                standard = Std, background = Bg, inputField = Input,
                knob = Knob, checkmark = Check, dropdown = Arrow, mask = Mask
            };
            _tmp = new TMP_DefaultControls.Resources
            {
                standard = Std, background = Bg, inputField = Input,
                knob = Knob, checkmark = Check, dropdown = Arrow, mask = Mask
            };
        }
    }
}
#endif
