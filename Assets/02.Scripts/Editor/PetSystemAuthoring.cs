using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Animancer;
using AYellowpaper.SerializedCollections;
using KinematicCharacterController;
using TMPro;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Animation;
using UPlayGround.Components.Pet;
using UPlayGround.Data.Actor;
using UPlayGround.Data.Actor.Animation;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Item;
using UPlayGround.Data.Path;
using UPlayGround.Data.Pet;
using UPlayGround.Manager;
using UPlayGround.MovementController;
using UPlayGround.UI;

namespace UPlayGround.EditorTools
{
    /// <summary>첫 펫 수직 구현에 필요한 추적 에셋을 생성하고 기존 로더에 등록한다.</summary>
    public static class PetSystemAuthoring
    {
        private const string DataRoot = "Assets/10.Datas/Pet/";
        private const string PrefabRoot = "Assets/03.Prefabs/Actor/Pet/";
        private const string MonkeyRoot = "Assets/ExternalAssets/Character/Suriyun/Pspsps/";
        private const string ClipRoot = "Assets/ExternalAssets/Character/Suriyun/Animations/Pspsps Animations/Character Animations/";
        private static TMP_FontAsset s_font;
        private static Sprite s_panel;
        private static GameObject s_buttonTemplate;

        [UPlaygroundTool("PetSystem.CreateFirstCompanion")]
        public static void CreateFirstCompanion()
        {
            if (EditorApplication.isPlaying || EditorApplication.isCompiling || EditorUtility.scriptCompilationFailed)
                throw new InvalidOperationException("컴파일이 끝난 Edit Mode에서 실행해야 합니다.");
            Directory.CreateDirectory(DataRoot); Directory.CreateDirectory(PrefabRoot);
            Directory.CreateDirectory("Assets/03.Prefabs/UI/Scene/Pet");
            var party = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03.Prefabs/UI/Scene/Party/UI_Scene_PartyMenu.prefab");
            if (party == null) throw new InvalidOperationException("파티 화면 원본이 없습니다.");
            s_font = party.GetComponentsInChildren<TextMeshProUGUI>(true).First(text => text.font != null).font;
            s_buttonTemplate = party.GetComponentsInChildren<Button>(true).First().gameObject;
            s_panel = party.GetComponentsInChildren<Image>(true).First(image => image.sprite != null).sprite;
            int petLayer = EnsurePetLayer();
            var follow = LoadOrCreate<PetFollowProfileSO>(DataRoot + "MonkeyFollow.asset");
            follow.groundMask = LayerMask.GetMask("Ground");
            follow.obstructionMask = LayerMask.GetMask("Ground");
            follow.navAgentTypeId = UnityEngine.AI.NavMesh.GetSettingsCount() > 0
                ? UnityEngine.AI.NavMesh.GetSettingsByIndex(0).agentTypeID : 0;
            var loot = LoadOrCreate<PetLootProfileSO>(DataRoot + "MonkeyLoot.asset");
            var animation = LoadOrCreate<ActorAnimationMotionSet>(DataRoot + "MonkeyMotions.asset");
            animation.motionSlots ??= new();
            animation.motionSlots[MotionTags.Idle] = CreateMotion("IdleA");
            animation.motionSlots[MotionTags.Walk] = CreateMotion("Walk");
            animation.motionSlots[MotionTags.Run] = CreateMotion("Run");
            var actorDefinition = LoadOrCreate<ActorDefinitionSO>(DataRoot + "MonkeyActor.asset");
            actorDefinition.actorId = "Pet.Monkey";
            actorDefinition.displayName = "원숭이";
            actorDefinition.actorType = ActorType.Pet;
            actorDefinition.prefab = CreateMonkey(actorDefinition, animation, follow, petLayer);
            var definition = LoadOrCreate<PetDefinitionSO>(DataRoot + "Monkey.asset");
            definition.petId = "Pet.Monkey"; definition.actor = actorDefinition;
            definition.followProfile = follow; definition.lootProfile = loot;
            if (definition.appearances.Count == 0)
            {
                for (int i = 0; i < 10; i++)
                {
                    var original = AssetDatabase.LoadAssetAtPath<GameObject>(MonkeyRoot + $"Prefab/Characters/Monkey/Chibi_Monkey_{i:00}.prefab");
                    if (original == null) throw new InvalidOperationException($"원숭이 외형 누락: {i:00}");
                    var materials = original.GetComponentInChildren<SkinnedMeshRenderer>().sharedMaterials;
                    definition.appearances.Add(new PetAppearance { appearanceId = $"Monkey.{i:00}", body = materials[0], face = materials[1] });
                }
            }
            var catalog = LoadOrCreate<PetCatalogSO>(DataRoot + "PetCatalog.asset");
            if (!catalog.definitions.Contains(definition)) catalog.definitions.Add(definition);
            if (!catalog.initiallyUnlocked.Contains(definition.petId)) catalog.initiallyUnlocked.Add(definition.petId);
            catalog.defaultPetId = definition.petId;
            var drops = LoadOrCreate<GroundDropSettingsSO>(DataRoot + "GroundDropSettings.asset");
            drops.groundMask = LayerMask.GetMask("Ground"); drops.visibilityMask = drops.groundMask;
            drops.dropPrefab = CreateDropPrefab(); drops.labelPrefab = CreateLabelPrefab();
            GameObject menu = CreatePetMenu();
            var actorDatabase = FindSingle<ActorDatabase>();
            actorDatabase.AddDefinition(actorDefinition);
            var uiDatabase = FindSingle<UIPrefabDatabase>();
            if (!uiDatabase.HasKey(UI_Scene_PetMenu.UIKey))
                uiDatabase.AddPrefab(UI_Scene_PetMenu.UIKey, menu, CanvasLayer.Scene, "동행 펫과 외형 선택");
            AddMenuButton();
            RegisterAddress(catalog, PetManager.CatalogKey);
            RegisterAddress(drops, GroundDropManager.SettingsKey);
            foreach (var asset in new ScriptableObject[] { follow, loot, animation, actorDefinition, definition, catalog, drops, actorDatabase, uiDatabase })
            { EditorUtility.SetDirty(asset); AssetDatabase.SaveAssetIfDirty(asset); }
            AssetDatabase.Refresh();
        }

        private static MotionSetAsset CreateMotion(string clipName)
        {
            var motion = LoadOrCreate<MotionSetAsset>(DataRoot + "Monkey_" + clipName + ".asset");
            if (motion.motionSet?.motions?.Count > 0) return motion;
            var clip = AssetDatabase.LoadAllAssetsAtPath(ClipRoot + "Anim_Chibi@" + clipName + ".fbx")
                .OfType<AnimationClip>().FirstOrDefault(value => !value.name.StartsWith("__"));
            if (clip == null) throw new InvalidOperationException("클립 누락: " + clipName);
            motion.motionSet = MotionSet.CreateAuthored("Monkey." + clipName, MotionSectionEndPolicy.LoopSelf);
            motion.motionSet.motions.Add(new UPlayGround.Animation.Motion { id = Guid.NewGuid().ToString("N"), motionName = clipName, motionClip = clip });
            EditorUtility.SetDirty(motion); AssetDatabase.SaveAssetIfDirty(motion);
            return motion;
        }

        private static GameObject CreateMonkey(ActorDefinitionSO definition, ActorAnimationMotionSet motions, PetFollowProfileSO follow, int layer)
        {
            string path = PrefabRoot + "Pet_Monkey.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;
            var root = new GameObject("Pet_Monkey");
            try
            {
                var original = AssetDatabase.LoadAssetAtPath<GameObject>(MonkeyRoot + "Prefab/Characters/Monkey/Chibi_Monkey_00.prefab");
                var visual = (GameObject)PrefabUtility.InstantiatePrefab(original, root.transform);
                visual.name = "Model";
                var unityAnimator = visual.GetComponentInChildren<Animator>();
                unityAnimator.applyRootMotion = false; unityAnimator.runtimeAnimatorController = null;
                var animancer = root.AddComponent<AnimancerComponent>(); animancer.Animator = unityAnimator;
                var animator = root.AddComponent<ActorAnimator>(); SetReference(animator, "_motionSet", motions);
                var motor = root.AddComponent<KinematicCharacterMotor>();
                motor.SetCapsuleDimensions(follow.capsuleRadius, follow.capsuleHeight, follow.capsuleHeight * 0.5f);
                root.AddComponent<PetMovementController>();
                var actor = root.AddComponent<PetActor>(); SetReference(actor, "_definition", definition);
                var presentation = root.AddComponent<PetPresentation>();
                SetReference(presentation, "_body", visual.GetComponentInChildren<SkinnedMeshRenderer>());
                SetReference(presentation, "_visualRoot", visual);
                presentation.ApplyModelScale();
                foreach (Transform child in root.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = layer;
                return PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        private static GameObject CreateDropPrefab()
        {
            string path = "Assets/03.Prefabs/Actor/Pet/GroundDrop.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;
            var root = new GameObject("GroundDrop");
            try
            {
                root.layer = LayerMask.NameToLayer("InteractableObject");
                var sphere = root.AddComponent<SphereCollider>(); sphere.radius = 0.25f; sphere.isTrigger = true;
                var icon = new GameObject("Icon", typeof(SpriteRenderer)); icon.transform.SetParent(root.transform, false);
                icon.transform.localPosition = new Vector3(0f, 0.25f, 0f); icon.transform.localScale = Vector3.one * 0.4f;
                var view = root.AddComponent<GroundDropRepresentation>(); SetReference(view, "_icon", icon.GetComponent<SpriteRenderer>());
                return PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        private static GameObject CreateLabelPrefab()
        {
            string path = "Assets/03.Prefabs/UI/WorldSpace/UIItemDropLabel.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;
            var root = RectObject("UIItemDropLabel", null, new Vector2(220f, 36f), Vector2.zero);
            try
            {
                var image = root.AddComponent<Image>(); image.sprite = s_panel; image.type = Image.Type.Sliced;
                image.color = new Color(0.06f, 0.08f, 0.1f, 0.9f);
                var button = root.AddComponent<Button>(); button.targetGraphic = image;
                var group = root.AddComponent<CanvasGroup>();
                var text = Text(root.transform, "Name", "", new Vector2(175f, 32f), new Vector2(17f, 0f), 18f);
                var icon = RectObject("Icon", root.transform, new Vector2(28f, 28f), new Vector2(-92f, 0f)).AddComponent<Image>();
                icon.raycastTarget = false;
                var view = root.AddComponent<UIItemDropLabel>();
                SetReference(view, "_button", button); SetReference(view, "_text", text);
                SetReference(view, "_itemIcon", icon); SetReference(view, "_group", group);
                return PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        private static GameObject CreatePetMenu()
        {
            string path = "Assets/03.Prefabs/UI/Scene/Pet/UI_Scene_PetMenu.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;
            var root = RectObject("UI_Scene_PetMenu", null, new Vector2(1100f, 760f), Vector2.zero);
            try
            {
                var group = root.AddComponent<CanvasGroup>();
                var panel = root.AddComponent<Image>(); panel.sprite = s_panel; panel.type = Image.Type.Sliced;
                panel.color = new Color(0.07f, 0.09f, 0.12f, 0.98f);
                Text(root.transform, "Title", "함께하는 친구", new Vector2(900f, 60f), new Vector2(0, 300), 32);
                var name = Text(root.transform, "PetName", "원숭이", new Vector2(800f, 50f), new Vector2(0, 225), 28);
                var bond = Text(root.transform, "Bond", "", new Vector2(800f, 40f), new Vector2(0, 170), 22);
                var status = Text(root.transform, "Status", "", new Vector2(900f, 50f), new Vector2(0, -240), 20);
                var appearances = RectObject("Appearances", root.transform, new Vector2(850f, 210f), new Vector2(0, 0));
                var grid = appearances.AddComponent<GridLayoutGroup>(); grid.cellSize = new Vector2(155, 85); grid.spacing = new Vector2(15, 18);
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = 5;
                var template = Button(appearances.transform, "Appearance", "", Vector2.zero, new Vector2(155, 85));
                var toggle = Button(root.transform, "Accompany", "함께 다니기", new Vector2(-160, -315), new Vector2(280, 58));
                var close = Button(root.transform, "Close", "닫기", new Vector2(160, -315), new Vector2(280, 58));
                var menu = root.AddComponent<UI_Scene_PetMenu>();
                SetReference(menu, "_name", name); SetReference(menu, "_bond", bond); SetReference(menu, "_status", status);
                SetReference(menu, "_toggle", toggle); SetReference(menu, "_close", close);
                SetReference(menu, "_appearanceTemplate", template); SetReference(menu, "_appearanceRoot", appearances.transform);
                SetReference(menu, "_sceneContent", root.transform);
                return PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        private static void AddMenuButton()
        {
            const string path = "Assets/03.Prefabs/UI/HUD/UI_Scene_MenuPanel.prefab";
            string original = File.ReadAllText(path).Replace("\r\n", "\n");
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var menu = root.GetComponent<UI_Scene_MenuPanel>();
                var serialized = new SerializedObject(menu);
                var previous = serialized.FindProperty("_petButton").objectReferenceValue as Button;
                if (previous != null && previous.name == "PetMenuButton") return;
                if (previous != null) throw new InvalidOperationException("기존 펫 메뉴 버튼은 자동 교체하지 않습니다.");
                var exit = (Button)serialized.FindProperty("_exitButton").objectReferenceValue;
                var tile = UnityEngine.Object.Instantiate(exit.gameObject, exit.transform.parent);
                tile.name = "PetMenuButton";
                foreach (var text in tile.GetComponentsInChildren<TextMeshProUGUI>(true)) text.text = "동행 펫";
                var button = tile.GetComponent<Button>();
                button.onClick = new Button.ButtonClickedEvent();
                SetReference(menu, "_petButton", button);
                var rect = (RectTransform)tile.transform;
                rect.anchoredPosition = ((RectTransform)exit.transform).anchoredPosition + new Vector2(-220f, 0f);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            PreserveMenuObjects(path, original);
        }

        /// <summary>기존 오브젝트의 값과 순서를 보존하고 새 버튼의 참조 두 곳만 반영한다.</summary>
        private static void PreserveMenuObjects(string path, string original)
        {
            const string pattern = @"(?ms)^--- !u!(\d+) &(-?\d+).*?(?=^--- !u!|\z)";
            var previous = Regex.Matches(original, pattern).Cast<Match>().ToDictionary(match => match.Groups[2].Value);
            var written = Regex.Matches(File.ReadAllText(path).Replace("\r\n", "\n"), pattern)
                .Cast<Match>().ToDictionary(match => match.Groups[2].Value);
            foreach (string id in previous.Keys)
                if (!written.ContainsKey(id)) throw new InvalidOperationException("기존 메뉴 오브젝트가 제거되어 저장을 중단합니다: " + id);
            string merged = Regex.Replace(original, pattern, match =>
            {
                string before = match.Value;
                string after = written[match.Groups[2].Value].Value;
                if (before.Contains("  _partyButton:"))
                {
                    string reference = Regex.Match(after, @"(?m)^  _petButton:.*$").Value;
                    if (string.IsNullOrEmpty(reference)) throw new InvalidOperationException("펫 메뉴 버튼 참조 누락");
                    return Regex.Replace(before, @"(?m)^(  _partyButton:.*)$", "$1\n" + reference);
                }
                if (match.Groups[1].Value != "224") return before;
                var oldChildren = Regex.Matches(before, @"(?m)^  - \{fileID: \d+\}$").Cast<Match>().Select(value => value.Value).ToHashSet();
                var added = Regex.Matches(after, @"(?m)^  - \{fileID: \d+\}$").Cast<Match>()
                    .Select(value => value.Value).Where(value => !oldChildren.Contains(value)).ToArray();
                if (added.Length == 0) return before;
                if (added.Length != 1) throw new InvalidOperationException("메뉴 버튼 외의 자식 변경은 허용하지 않습니다.");
                return before.Replace("  m_Father:", added[0] + "\n  m_Father:");
            });
            foreach (var pair in written)
                if (!previous.ContainsKey(pair.Key)) merged += "\n" + Regex.Replace(pair.Value.Value, @"(?m)[ \t]+\r?$", "");
            File.WriteAllText(path, merged, new UTF8Encoding(false));
            AssetDatabase.ImportAsset(path);
        }

        private static int EnsurePetLayer()
        {
            int layer = LayerMask.NameToLayer("Pet");
            if (layer < 0)
            {
                var tags = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
                var layers = tags.FindProperty("layers");
                for (int i = 8; i < layers.arraySize; i++)
                    if (string.IsNullOrEmpty(layers.GetArrayElementAtIndex(i).stringValue))
                    { layers.GetArrayElementAtIndex(i).stringValue = "Pet"; layer = i; break; }
                if (layer < 0) throw new InvalidOperationException("펫 전용 레이어를 추가할 빈 슬롯이 없습니다.");
                tags.ApplyModifiedPropertiesWithoutUndo();
            }
            for (int other = 0; other < 32; other++)
                Physics.IgnoreLayerCollision(layer, other, other != LayerMask.NameToLayer("Ground"));
            SavePetCollisionSettings(layer);
            return layer;
        }

        private static void SavePetCollisionSettings(int layer)
        {
            const string tagPath = "ProjectSettings/TagManager.asset";
            string[] lines = File.ReadAllLines(tagPath);
            int start = Array.IndexOf(lines, "  layers:");
            int index = start + layer + 1;
            if (start < 0 || index >= lines.Length || (lines[index].Trim() != "-" && lines[index].Trim() != "- Pet"))
                throw new InvalidOperationException("기존 레이어를 덮어쓸 수 없습니다.");
            lines[index] = "  - Pet";
            File.WriteAllLines(tagPath, lines, new UTF8Encoding(false));
            const string physicsPath = "ProjectSettings/DynamicsManager.asset";
            var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath(physicsPath)[0]);
            var matrix = settings.FindProperty("m_LayerCollisionMatrix");
            if (matrix == null || matrix.arraySize != 32) throw new InvalidOperationException("충돌 행렬 형식이 다릅니다.");
            var encoded = new StringBuilder(256);
            for (int row = 0; row < 32; row++)
                foreach (byte value in BitConverter.GetBytes(matrix.GetArrayElementAtIndex(row).intValue))
                    encoded.Append(value.ToString("x2"));
            string physics = File.ReadAllText(physicsPath);
            if (!Regex.IsMatch(physics, @"(?m)^  m_LayerCollisionMatrix: [0-9a-fA-F]+\r?$"))
                throw new InvalidOperationException("충돌 행렬 저장 필드를 찾을 수 없습니다.");
            File.WriteAllText(physicsPath, Regex.Replace(physics,
                @"(?m)^(  m_LayerCollisionMatrix: )[0-9a-fA-F]+", match => match.Groups[1].Value + encoded), new UTF8Encoding(false));
        }
        private static GameObject RectObject(string name, Transform parent, Vector2 size, Vector2 position)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform; rect.sizeDelta = size; rect.anchoredPosition = position;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            return go;
        }
        private static TextMeshProUGUI Text(Transform parent, string name, string text, Vector2 size, Vector2 position, float fontSize)
        {
            var label = RectObject(name, parent, size, position).AddComponent<TextMeshProUGUI>();
            label.font = s_font; label.fontSize = fontSize; label.text = text; label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false; return label;
        }
        private static Button Button(Transform parent, string name, string text, Vector2 position, Vector2 size)
        {
            var go = UnityEngine.Object.Instantiate(s_buttonTemplate, parent); go.name = name;
            var rect = (RectTransform)go.transform; rect.sizeDelta = size; rect.anchoredPosition = position;
            var button = go.GetComponent<Button>(); button.onClick = new Button.ButtonClickedEvent();
            foreach (var label in go.GetComponentsInChildren<TextMeshProUGUI>(true)) label.text = text;
            return button;
        }
        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(asset, path); return asset;
        }
        private static T FindSingle<T>() where T : UnityEngine.Object
        {
            var guids = AssetDatabase.FindAssets("t:" + typeof(T).Name);
            if (guids.Length != 1) throw new InvalidOperationException(typeof(T).Name + " 단일 원본을 확인해야 합니다.");
            return AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }
        private static void SetReference(UnityEngine.Object owner, string field, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(owner);
            var property = serialized.FindProperty(field) ?? throw new InvalidOperationException("필드 누락: " + field);
            property.objectReferenceValue = value; serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        private static void RegisterAddress(UnityEngine.Object asset, string address)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) throw new InvalidOperationException("기존 Addressables 설정이 없습니다.");
            var entry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(asset)), settings.DefaultGroup);
            entry.address = address;
            EditorUtility.SetDirty(settings); AssetDatabase.SaveAssetIfDirty(settings);
            AssetDatabase.SaveAssetIfDirty(settings.DefaultGroup);
        }
    }
}
