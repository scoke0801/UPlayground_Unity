using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Data.Actor;
using UPlayGround.Data.Actor.NPC;
using UPlayGround.EditorTools;

namespace UPlayGround.UI.Editor
{
    /// <summary>일상 대사와 외형 연결을 검증하고 고정 말풍선 프리팹·기본 설정을 저작한다.</summary>
    public static class NpcAmbientSpeechAuthoring
    {
        public const string SettingsPath = "Assets/10.Datas/NpcAmbientSpeech/Resources/Config/NpcAmbientSpeech.asset";
        public const string PrefabPath = "Assets/03.Prefabs/UI/WorldSpace/UINpcSpeechBubble.prefab";
        private const string LinesPath = "Assets/10.Datas/NpcAmbientSpeech/NpcAmbientLines.json";
        private const string BindingsPath = "Assets/10.Datas/NpcAmbientSpeech/NpcAmbientBindings.json";
        private const string ImagePath = "Assets/04.Images/UI/NpcAmbientSpeech/NpcSpeechFrame.png";
        private const string FontPath = "Assets/ExternalAssets/Font/Maplestory Light SDF.asset";

        [Serializable] private sealed class LinesSource { public NpcAmbientSpeechSO.Voice[] voices; }
        [Serializable] private sealed class BindingsSource { public BindingSource[] bindings; }
        [Serializable] private sealed class BindingSource { public string npcPath; public string voiceId; public string requiredFlag; public string speakerName; }

        /// <summary>검증을 통과한 저작 원본만 SO로 동기화한다. 새 에셋에는 기존 GUID를 덮지 않는다.</summary>
        [UPlaygroundTool("npc.ambient-speech.apply")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode에서 실행하세요.");
            var source = JsonUtility.FromJson<LinesSource>(File.ReadAllText(LinesPath));
            ValidateLines(source.voices);
            var bindings = LoadBindings(source.voices);
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (font == null || !File.Exists(ImagePath)) throw new InvalidOperationException("말풍선 폰트 또는 ImageGen 이미지가 없습니다.");
            var sprite = ImportFrame();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null) prefab = CreatePrefab(sprite, font);
            if (prefab.GetComponent<UINpcSpeechBubble>() == null) throw new InvalidOperationException("다른 프리팹을 덮어쓸 수 없습니다.");
            var settings = AssetDatabase.LoadAssetAtPath<NpcAmbientSpeechSO>(SettingsPath);
            if (settings == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath));
                AssetDatabase.Refresh();
                settings = ScriptableObject.CreateInstance<NpcAmbientSpeechSO>();
                AssetDatabase.CreateAsset(settings, SettingsPath);
            }
            Undo.RecordObject(settings, "NPC 일상 대사 동기화");
            settings.voices = source.voices;
            settings.bindings = bindings;
            settings.InvalidateBindings();
            settings.bubblePrefab = prefab;
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssetIfDirty(settings);
            Validate();
        }

        /// <summary>원본과 Unity 로드 결과의 대사·이름·프리팹·연결을 검사한다.</summary>
        [UPlaygroundTool("npc.ambient-speech.validate")]
        public static void Validate()
        {
            var settings = AssetDatabase.LoadAssetAtPath<NpcAmbientSpeechSO>(SettingsPath);
            if (settings == null || Resources.Load<NpcAmbientSpeechSO>(NpcAmbientSpeechSO.ResourcePath) != settings)
                throw new InvalidOperationException("기본 설정의 Resources 연결이 없습니다.");
            ValidateLines(settings.voices);
            if (settings.bubblePrefab == null || settings.bubblePrefab.GetComponent<UINpcSpeechBubble>() == null)
                throw new InvalidOperationException("말풍선 프리팹 연결이 없습니다.");
            var image = settings.bubblePrefab.GetComponent<Image>();
            if (image == null || image.sprite == null || image.raycastTarget)
                throw new InvalidOperationException("말풍선 이미지 또는 입력 통과 설정 오류");
            foreach (var text in settings.bubblePrefab.GetComponentsInChildren<TMP_Text>(true))
                if (text.font == null || text.raycastTarget) throw new InvalidOperationException("말풍선 폰트 또는 입력 통과 설정 오류");
            foreach (var binding in settings.bindings)
                if (binding.npc == null || !settings.HasVoice(binding.voice) || string.IsNullOrWhiteSpace(binding.npc.actorName))
                    throw new InvalidOperationException("NPC 데이터·말투·이름 연결 누락");
            foreach (Transform child in settings.bubblePrefab.GetComponentsInChildren<Transform>(true))
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) != 0)
                    throw new InvalidOperationException("말풍선 Missing Script");
            Directory.CreateDirectory("Logs/NpcAmbientSpeech");
            File.WriteAllText("Logs/NpcAmbientSpeech/AssetValidation.txt",
                $"Unity {Application.unityVersion}\n독립 대사 1024개 / 말투 {settings.voices.Length}개\nNPC 연결 {settings.bindings.Length}개\nID·정확 중복·길이·행동/시간 조건·Resources·프리팹·Missing Script 검사 통과\n시각·어투 최종 품질과 Play Mode는 사용자 테스트 대기\n");
        }

        private static void ValidateLines(NpcAmbientSpeechSO.Voice[] voices)
        {
            var ids = new HashSet<string>();
            var texts = new HashSet<string>();
            var voiceIds = new HashSet<string>();
            if (voices == null || voices.Length != 8) throw new InvalidOperationException("말투는 8개여야 합니다.");
            foreach (var voice in voices)
            {
                if (voice == null || !voiceIds.Add(voice.id) || voice.lines == null || voice.lines.Length != 128)
                    throw new InvalidOperationException("말투 ID 중복 또는 대사 개수 오류");
                foreach (var line in voice.lines)
                {
                    if (line == null || string.IsNullOrWhiteSpace(line.id) || !ids.Add(line.id)
                        || string.IsNullOrWhiteSpace(line.text) || !texts.Add(line.text) || line.text.Length > 36
                        || string.IsNullOrWhiteSpace(line.topic) || line.activities == 0 || line.periods == 0
                        || (line.activities & ~NpcSpeechActivity.Any) != 0 || (line.periods & ~NpcSpeechPeriod.Any) != 0)
                        throw new InvalidOperationException("일상 대사 ID·중복·길이·조건 오류: " + line?.id);
                }
            }
            if (texts.Count != 1024) throw new InvalidOperationException("독립 대사는 1024개여야 합니다.");
        }

        private static NpcAmbientSpeechSO.Binding[] LoadBindings(NpcAmbientSpeechSO.Voice[] voices)
        {
            var source = JsonUtility.FromJson<BindingsSource>(File.ReadAllText(BindingsPath));
            var result = new List<NpcAmbientSpeechSO.Binding>();
            var seen = new HashSet<NpcActorSO>();
            foreach (var item in source.bindings)
            {
                var npc = AssetDatabase.LoadAssetAtPath<NpcActorSO>(item.npcPath);
                int voice = Array.FindIndex(voices, v => v.id == item.voiceId);
                if (npc == null || !seen.Add(npc) || voice < 0)
                    throw new InvalidOperationException("NPC 경로·중복·말투 오류: " + item.npcPath);
                result.Add(new NpcAmbientSpeechSO.Binding { npc = npc, voice = voice,
                    requiredFlag = item.requiredFlag, speakerName = item.speakerName });
            }
            return result.ToArray();
        }

        private static Sprite ImportFrame()
        {
            var existing = AssetDatabase.LoadAllAssetsAtPath(ImagePath).OfType<Sprite>().FirstOrDefault();
            if (existing != null) return existing;
            var texture = new Texture2D(2, 2);
            Rect bounds;
            try
            {
                texture.LoadImage(File.ReadAllBytes(ImagePath));
                var pixels = texture.GetPixels32();
                int minX = texture.width, minY = texture.height, maxX = -1, maxY = -1;
                for (int y = 0; y < texture.height; y++)
                    for (int x = 0; x < texture.width; x++)
                        if (pixels[y * texture.width + x].a > 8)
                        { minX = Math.Min(minX, x); minY = Math.Min(minY, y); maxX = Math.Max(maxX, x); maxY = Math.Max(maxY, y); }
                if (maxX < minX || maxY < minY) throw new InvalidOperationException("말풍선 이미지가 비어 있습니다.");
                bounds = new Rect(minX, minY, maxX - minX + 1, maxY - minY + 1);
            }
            finally { UnityEngine.Object.DestroyImmediate(texture); }
            var importer = (TextureImporter)AssetImporter.GetAtPath(ImagePath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
            // 원본 alpha는 보존하고 Unity의 스프라이트 영역만 투명 여백 안으로 좁힌다.
#pragma warning disable CS0618
            importer.spritesheet = new[] { new SpriteMetaData { name = "NpcSpeechFrame", rect = bounds, pivot = new Vector2(0.5f, 0.5f), alignment = (int)SpriteAlignment.Center } };
#pragma warning restore CS0618
            importer.SaveAndReimport();
            return AssetDatabase.LoadAllAssetsAtPath(ImagePath).OfType<Sprite>().Single();
        }

        private static GameObject CreatePrefab(Sprite sprite, TMP_FontAsset font)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
            AssetDatabase.Refresh();
            var root = new GameObject("UINpcSpeechBubble", typeof(RectTransform), typeof(CanvasGroup), typeof(Image), typeof(UINpcSpeechBubble));
            try
            {
                var rect = root.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0f);
                rect.sizeDelta = new Vector2(480f, 150f);
                var image = root.GetComponent<Image>();
                image.sprite = sprite;
                image.raycastTarget = false;
                var group = root.GetComponent<CanvasGroup>();
                group.blocksRaycasts = group.interactable = false;
                var name = CreateText(root.transform, "Name", font, 22f, new Color32(224, 201, 150, 255), new Vector2(22f, -14f), new Vector2(436f, 27f));
                var line = CreateText(root.transform, "Line", font, 28f, new Color32(252, 245, 227, 255), new Vector2(22f, -45f), new Vector2(436f, 77f));
                var view = new SerializedObject(root.GetComponent<UINpcSpeechBubble>());
                view.FindProperty("_name").objectReferenceValue = name;
                view.FindProperty("_line").objectReferenceValue = line;
                view.FindProperty("_group").objectReferenceValue = group;
                view.FindProperty("_panel").objectReferenceValue = rect;
                view.ApplyModifiedPropertiesWithoutUndo();
                return PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        private static TextMeshProUGUI CreateText(Transform parent, string name, TMP_FontAsset font, float size, Color color, Vector2 position, Vector2 dimensions)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position; rect.sizeDelta = dimensions;
            var text = go.GetComponent<TextMeshProUGUI>();
            text.font = font; text.fontSize = size; text.color = color;
            text.alignment = TextAlignmentOptions.TopLeft;
            text.raycastTarget = false; text.richText = false;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Overflow;
            return text;
        }
    }
}
