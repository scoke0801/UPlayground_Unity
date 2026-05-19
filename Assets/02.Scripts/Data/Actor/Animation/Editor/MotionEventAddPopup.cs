using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UPlayGround.Data.Event;

namespace UPlayGround.Animation.Editor
{
    /// <summary>
    /// MotionEvent 추가용 검색 팝업.
    /// 카테고리, 별칭 검색, 최근 사용, 프리셋을 한 곳에서 처리한다.
    /// </summary>
    public sealed class MotionEventAddPopup : PopupWindowContent
    {
        const string RecentPrefsKey = "MotionEventAddPopup_Recent";
        const int MaxRecentCount = 6;

        readonly List<MotionEventBase> _eventList;
        readonly float _defaultStartTime;
        readonly Action _onBeforeAdd;
        readonly Action _onAdd;
        readonly SearchField _searchField = new SearchField();
        readonly List<EventMeta> _eventMetas;
        List<EventPreset> _presets;

        Vector2 _scroll;
        string _searchText = string.Empty;
        bool _showPresetCreate;
        string _newPresetName = string.Empty;
        string _newPresetDescription = string.Empty;
        string _newPresetAliases = string.Empty;

        public MotionEventAddPopup(List<MotionEventBase> eventList, float defaultStartTime, Action onBeforeAdd, Action onAdd)
        {
            _eventList = eventList;
            _defaultStartTime = defaultStartTime;
            _onBeforeAdd = onBeforeAdd;
            _onAdd = onAdd;
            _eventMetas = MotionEventMetadata.GetAll();
            ReloadPresets();
        }

        public override Vector2 GetWindowSize() => new Vector2(430f, 520f);

        public override void OnOpen()
        {
            _searchField.SetFocus();
        }

        public override void OnGUI(Rect rect)
        {
            DrawSearchBar();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            {
                string query = Normalize(_searchText);

                if (string.IsNullOrWhiteSpace(query))
                {
                    DrawPresetCreateSection();
                    DrawPresetSection("프리셋", _presets);
                    DrawRecentSection();
                    DrawEventGroups(_eventMetas);
                }
                else
                {
                    var filteredPresets = _presets
                        .Where(p => Matches(p.SearchText, query))
                        .ToList();
                    var filteredEvents = _eventMetas
                        .Where(m => Matches(m.SearchText, query))
                        .ToList();

                    DrawPresetSection("프리셋 검색 결과", filteredPresets);
                    DrawEventGroups(filteredEvents);

                    if (filteredPresets.Count == 0 && filteredEvents.Count == 0)
                        EditorGUILayout.HelpBox("검색 결과가 없습니다.", MessageType.Info);
                }
            }
            EditorGUILayout.EndScrollView();
        }

        void DrawSearchBar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            _searchText = _searchField.OnToolbarGUI(_searchText, GUILayout.ExpandWidth(true));
            if (GUILayout.Button("초기화", EditorStyles.toolbarButton, GUILayout.Width(54f)))
            {
                _searchText = string.Empty;
                _searchField.SetFocus();
            }
            EditorGUILayout.EndHorizontal();
        }

        void DrawRecentSection()
        {
            var recent = LoadRecent()
                .Select(key => FindRecentItem(key))
                .Where(item => item.IsValid)
                .ToList();

            if (recent.Count == 0)
                return;

            DrawRecentHeader();
            foreach (var item in recent)
            {
                if (item.Preset != null)
                    DrawPresetButton(item.Preset, true);
                else if (item.EventMeta != null)
                    DrawEventButton(item.EventMeta, true);
            }
        }

        void DrawPresetSection(string label, List<EventPreset> presets)
        {
            if (presets == null || presets.Count == 0)
                return;

            DrawHeader(label);
            foreach (var preset in presets)
                DrawPresetButton(preset);
        }

        void DrawPresetCreateSection()
        {
            bool hasEvents = _eventList != null && _eventList.Any(evt => evt != null);

            GUILayout.Space(5f);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            _showPresetCreate = EditorGUILayout.Foldout(_showPresetCreate, "현재 이벤트 목록을 프리셋으로 저장", true);

            using (new EditorGUI.DisabledScope(!hasEvents || string.IsNullOrWhiteSpace(_newPresetName)))
            {
                if (GUILayout.Button("저장", EditorStyles.miniButton, GUILayout.Width(52f)))
                {
                    SaveCurrentEventsAsPreset();
                    GUIUtility.ExitGUI();
                }
            }
            EditorGUILayout.EndHorizontal();

            if (_showPresetCreate)
            {
                using (new EditorGUI.DisabledScope(!hasEvents))
                {
                    _newPresetName = EditorGUILayout.TextField("이름", _newPresetName);
                    _newPresetDescription = EditorGUILayout.TextField("설명", _newPresetDescription);
                    _newPresetAliases = EditorGUILayout.TextField("검색 별칭", _newPresetAliases);
                }

                if (!hasEvents)
                    EditorGUILayout.HelpBox("저장할 이벤트가 없습니다. 먼저 타임라인에 이벤트를 추가하세요.", MessageType.Info);
                else
                    EditorGUILayout.HelpBox("현재 목록의 이벤트들을 복제해 저장합니다. 가장 빠른 이벤트 시작 시간을 0초로 정규화합니다.", MessageType.None);
            }
            EditorGUILayout.EndVertical();
        }

        void DrawEventGroups(List<EventMeta> metas)
        {
            if (metas == null || metas.Count == 0)
                return;

            foreach (var group in metas.GroupBy(m => m.Category).OrderBy(g => g.Key.SortOrder))
            {
                DrawHeader(group.Key.DisplayName);
                foreach (var meta in group.OrderBy(m => m.DisplayName))
                    DrawEventButton(meta);
            }
        }

        void DrawHeader(string label)
        {
            GUILayout.Space(5f);
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
        }

        void DrawRecentHeader()
        {
            GUILayout.Space(5f);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("최근 사용", EditorStyles.boldLabel);
            if (GUILayout.Button("전체 비우기", EditorStyles.miniButton, GUILayout.Width(76f)))
            {
                ClearRecent();
                GUIUtility.ExitGUI();
            }
            EditorGUILayout.EndHorizontal();
        }

        void DrawEventButton(EventMeta meta, bool showRemoveRecent = false)
        {
            var visual = MotionEventStyle.GetByType(meta.Type);
            string title = $"{visual.icon}  {meta.DisplayName}";

            if (DrawItemButton(title, meta.Description, showRemoveRecent, meta.RecentKey))
            {
                var evt = MotionEventTypeRegistry.CreateEventInstance(meta.Type);
                ApplyDefaultTime(evt, _defaultStartTime, 0.5f);
                _onBeforeAdd?.Invoke();
                _eventList.Add(evt);
                SaveRecent(meta.RecentKey);
                _onAdd?.Invoke();
                editorWindow.Close();
            }
        }

        void DrawPresetButton(EventPreset preset, bool showRemoveRecent = false)
        {
            bool showDeletePreset = preset.IsUserPreset && !showRemoveRecent;
            if (DrawItemButton($"★  {preset.DisplayName}", preset.Description, showRemoveRecent, preset.RecentKey,
                    showDeletePreset, () => DeleteUserPreset(preset.Id)))
            {
                _onBeforeAdd?.Invoke();
                foreach (var evt in preset.CreateEvents(_defaultStartTime))
                    _eventList.Add(evt);

                SaveRecent(preset.RecentKey);
                _onAdd?.Invoke();
                editorWindow.Close();
            }
        }

        bool DrawItemButton(string title, string description, bool showRemoveRecent = false, string recentKey = null,
            bool showDelete = false, Action onDelete = null)
        {
            Rect rect = EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(title, EditorStyles.miniBoldLabel);
            if (showDelete)
            {
                if (GUILayout.Button("삭제", EditorStyles.miniButton, GUILayout.Width(42f)))
                {
                    onDelete?.Invoke();
                    GUIUtility.ExitGUI();
                }
            }
            if (showRemoveRecent)
            {
                if (GUILayout.Button("제거", EditorStyles.miniButton, GUILayout.Width(42f)))
                {
                    RemoveRecent(recentKey);
                    GUIUtility.ExitGUI();
                }
            }
            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(description))
                EditorGUILayout.LabelField(description, EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.EndVertical();

            EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);

            bool clicked = Event.current.type == EventType.MouseDown &&
                           Event.current.button == 0 &&
                           rect.Contains(Event.current.mousePosition);
            if (clicked)
                Event.current.Use();

            return clicked;
        }

        void ReloadPresets()
        {
            _presets = MotionEventMetadata.GetPresets();

            var library = MotionEventPresetLibraryUtility.Load();
            if (library?.presets == null)
                return;

            foreach (var entry in library.presets)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.displayName))
                    continue;

                _presets.Add(EventPreset.FromUserPreset(entry));
            }
        }

        void SaveCurrentEventsAsPreset()
        {
            if (_eventList == null || !_eventList.Any(evt => evt != null) || string.IsNullOrWhiteSpace(_newPresetName))
                return;

            var library = MotionEventPresetLibraryUtility.LoadOrCreate();
            var entry = MotionEventPresetEntry.FromEvents(
                _newPresetName.Trim(),
                _newPresetDescription?.Trim(),
                _newPresetAliases?.Trim(),
                _eventList);

            library.presets.Add(entry);
            MotionEventPresetLibraryUtility.Save(library);

            _newPresetName = string.Empty;
            _newPresetDescription = string.Empty;
            _newPresetAliases = string.Empty;
            ReloadPresets();
        }

        void DeleteUserPreset(string id)
        {
            if (string.IsNullOrEmpty(id))
                return;

            var library = MotionEventPresetLibraryUtility.Load();
            if (library?.presets == null)
                return;

            if (!EditorUtility.DisplayDialog("프리셋 삭제", "선택한 사용자 프리셋을 삭제할까요?", "삭제", "취소"))
                return;

            library.presets.RemoveAll(preset => preset != null && preset.id == id);
            MotionEventPresetLibraryUtility.Save(library);
            RemoveRecent($"preset:{id}");
            ReloadPresets();
        }

        static void ApplyDefaultTime(MotionEventBase evt, float startTime, float duration)
        {
            if (evt == null) return;
            evt.startTime = startTime;
            evt.endTime = startTime + Mathf.Max(0.01f, duration);
        }

        RecentItem FindRecentItem(string key)
        {
            var preset = _presets.FirstOrDefault(p => p.RecentKey == key);
            if (preset != null)
                return new RecentItem { Preset = preset };

            var meta = _eventMetas.FirstOrDefault(m => m.RecentKey == key);
            if (meta != null)
                return new RecentItem { EventMeta = meta };

            return default;
        }

        static bool Matches(string text, string query)
        {
            return !string.IsNullOrEmpty(text) && text.Contains(query);
        }

        static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim().ToLowerInvariant();
        }

        static List<string> LoadRecent()
        {
            string raw = EditorPrefs.GetString(RecentPrefsKey, string.Empty);
            return raw.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries).ToList();
        }

        static void SaveRecent(string key)
        {
            var recent = LoadRecent();
            recent.RemoveAll(x => x == key);
            recent.Insert(0, key);

            if (recent.Count > MaxRecentCount)
                recent.RemoveRange(MaxRecentCount, recent.Count - MaxRecentCount);

            EditorPrefs.SetString(RecentPrefsKey, string.Join("|", recent));
        }

        static void RemoveRecent(string key)
        {
            if (string.IsNullOrEmpty(key)) return;

            var recent = LoadRecent();
            recent.RemoveAll(x => x == key);
            EditorPrefs.SetString(RecentPrefsKey, string.Join("|", recent));
        }

        static void ClearRecent()
        {
            EditorPrefs.DeleteKey(RecentPrefsKey);
        }

        struct RecentItem
        {
            public EventMeta EventMeta;
            public EventPreset Preset;
            public bool IsValid => EventMeta != null || Preset != null;
        }

        public sealed class EventCategory
        {
            public readonly string DisplayName;
            public readonly int SortOrder;

            public EventCategory(string displayName, int sortOrder)
            {
                DisplayName = displayName;
                SortOrder = sortOrder;
            }
        }

        public sealed class EventMeta
        {
            public readonly Type Type;
            public readonly string DisplayName;
            public readonly EventCategory Category;
            public readonly string Description;
            public readonly string SearchText;
            public string RecentKey => $"event:{Type.FullName}";

            public EventMeta(Type type, string displayName, EventCategory category, string description, params string[] aliases)
            {
                Type = type;
                DisplayName = displayName;
                Category = category;
                Description = description;

                string aliasText = aliases != null ? string.Join(" ", aliases) : string.Empty;
                SearchText = Normalize($"{displayName} {category.DisplayName} {description} {type.Name} {aliasText}");
            }
        }

        public sealed class EventPreset
        {
            public readonly string Id;
            public readonly string DisplayName;
            public readonly string Description;
            public readonly string SearchText;
            public readonly bool IsUserPreset;
            readonly Func<float, IEnumerable<MotionEventBase>> _factory;

            public string RecentKey => $"preset:{Id}";

            public EventPreset(string id, string displayName, string description, Func<float, IEnumerable<MotionEventBase>> factory, params string[] aliases)
                : this(id, displayName, description, factory, false, aliases)
            {
            }

            EventPreset(string id, string displayName, string description, Func<float, IEnumerable<MotionEventBase>> factory, bool isUserPreset, string[] aliases)
            {
                Id = id;
                DisplayName = displayName;
                Description = description;
                IsUserPreset = isUserPreset;
                _factory = factory;

                string aliasText = aliases != null ? string.Join(" ", aliases) : string.Empty;
                SearchText = Normalize($"{displayName} {description} {aliasText}");
            }

            public static EventPreset FromUserPreset(MotionEventPresetEntry entry)
            {
                string[] aliases = string.IsNullOrWhiteSpace(entry.aliases)
                    ? Array.Empty<string>()
                    : entry.aliases.Split(new[] { ' ', ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries);

                return new EventPreset(
                    entry.id,
                    entry.displayName,
                    entry.description,
                    entry.CreateEvents,
                    true,
                    aliases);
            }

            public IEnumerable<MotionEventBase> CreateEvents(float startTime)
            {
                return _factory?.Invoke(startTime) ?? Enumerable.Empty<MotionEventBase>();
            }
        }

        static class MotionEventMetadata
        {
            static readonly EventCategory Combat = new EventCategory("Combat", 0);
            static readonly EventCategory VfxSfx = new EventCategory("VFX / SFX", 10);
            static readonly EventCategory Camera = new EventCategory("Camera", 20);
            static readonly EventCategory Movement = new EventCategory("Movement / Time", 30);
            static readonly EventCategory Utility = new EventCategory("Utility", 40);

            public static List<EventMeta> GetAll()
            {
                var known = new List<EventMeta>
                {
                    Meta<BeginCollisionEvent>("Collision", Combat, "공격 판정을 켜고 hitPhaseIndex를 Combat에 전달합니다.", "hitbox", "attack", "damage", "타격", "피격", "콜리전"),
                    Meta<DisableCollisionEvent>("DisableCollision", Combat, "공격 판정을 명시적으로 끕니다.", "hitbox off", "collision off", "판정 종료"),
                    Meta<ComboWindowEvent>("ComboWindow", Combat, "다음 콤보 입력을 받을 수 있는 구간을 엽니다.", "combo", "cancel", "chain", "연계", "콤보"),
                    Meta<FinishAttackEvent>("FinishAttack", Combat, "피니시 공격 처리 타이밍을 발생시킵니다.", "finish", "execution", "처형", "피니시"),
                    Meta<SpecialBreakAttackEvent>("SpecialBreakAttack", Combat, "브레이크 특수공격 피해 적용 타이밍을 발생시킵니다.", "break", "special", "groggy", "브레이크", "특수공격"),
                    Meta<InvincibilityEvent>("Invincibility", Combat, "피격 무적 구간을 설정합니다.", "iframe", "invincible", "무적", "회피"),
                    Meta<HealSkillEvent>("HealSkill", Combat, "회복 스킬 판정을 실행합니다.", "heal", "recovery", "힐", "회복"),

                    Meta<BeginParticleEvent>("Particle", VfxSfx, "파티클/VFX를 생성합니다.", "vfx", "effect", "fx", "이펙트", "파티클"),
                    Meta<PlaySoundEvent>("PlaySound", VfxSfx, "오디오 클립을 재생합니다.", "audio", "sfx", "sound", "소리", "사운드"),
                    Meta<FootstepEvent>("Footstep", VfxSfx, "지형 기반 발자국 사운드를 재생합니다.", "foot", "step", "walk", "발소리", "발자국"),
                    Meta<SpawnProjectileEvent>("SpawnProjectile", VfxSfx, "투사체를 지정 위치에서 발사합니다.", "projectile", "bullet", "arrow", "shot", "투사체", "화살"),
                    Meta<SpawnSkillEvent>("SpawnSkill", VfxSfx, "스킬 오브젝트를 생성합니다.", "skill", "cast", "스킬", "소환"),

                    Meta<CameraEffectEvent>("CameraEffect", Camera, "카메라 흔들림, 줌, FOV 등 CameraEffectData를 재생합니다.", "shake", "zoom", "fov", "camera", "카메라", "흔들림"),
                    Meta<CameraLookAtSocketEvent>("CameraLookAtSocket", Camera, "카메라가 특정 소켓/본을 바라보도록 합니다.", "lookat", "socket", "target", "시선", "소켓"),
                    Meta<FinishSideViewEvent>("FinishSideView", Camera, "피니시 연출용 사이드뷰 카메라를 시작/종료합니다.", "side", "cinematic", "연출", "사이드뷰"),

                    Meta<AddForceEvent>("AddForce", Movement, "대상에게 힘/이동량을 가합니다.", "force", "move", "push", "넉백", "이동"),
                    Meta<MotionEvent_MotionWarp>("MotionWarp", Movement, "모션 워핑을 적용합니다.", "warp", "root", "타겟 보정", "워프"),
                    Meta<AnimationSpeedEvent>("AnimationSpeed", Movement, "애니메이션 재생 속도를 구간별로 변경합니다.", "speed", "slow", "fast", "속도"),
                    Meta<TimeScaleEvent>("TimeScale", Movement, "게임 시간 배율을 변경합니다.", "hit stop", "slow motion", "time", "히트스톱", "시간"),
                    Meta<LoopEvent>("Loop", Movement, "모션 구간 반복, 정지, 무한 루프를 설정합니다.", "repeat", "freeze", "loop", "반복", "정지"),

                    Meta<CustomCallbackEvent>("CustomCallback", Utility, "문자열 기반 커스텀 콜백을 남깁니다.", "callback", "custom", "콜백"),
                    Meta<FreezeEnemyEvent>("FreezeEnemy", Utility, "적을 일시 정지시킵니다.", "freeze", "enemy", "stop", "빙결", "정지"),
                    Meta<HideTargetEvent>("HideTarget", Utility, "대상을 숨기거나 표시합니다.", "hide", "visible", "렌더", "숨김"),
                };

                var knownTypes = new HashSet<Type>(known.Select(m => m.Type));
                foreach (var type in MotionEventTypeRegistry.GetAllEventTypes())
                {
                    if (knownTypes.Contains(type))
                        continue;

                    known.Add(new EventMeta(
                        type,
                        MotionEventTypeRegistry.GetFriendlyName(type),
                        Utility,
                        "분류되지 않은 MotionEvent입니다.",
                        type.Name));
                }

                return known;
            }

            public static List<EventPreset> GetPresets()
            {
                return new List<EventPreset>
                {
                    new EventPreset(
                        "melee_basic",
                        "근접 공격 기본",
                        "Collision, ComboWindow를 기본 타이밍으로 추가합니다.",
                        start => new MotionEventBase[]
                        {
                            Timed(new BeginCollisionEvent(), start + 0.10f, 0.15f),
                            Timed(new ComboWindowEvent(), start + 0.25f, 0.25f),
                        },
                        "melee", "attack", "combo", "근접", "공격", "콤보"),

                    new EventPreset(
                        "camera_melee",
                        "카메라 연출 공격",
                        "CameraEffect, Collision, FinishAttack을 함께 추가합니다.",
                        start => new MotionEventBase[]
                        {
                            Timed(new CameraEffectEvent(), start + 0.00f, 0.25f),
                            Timed(new BeginCollisionEvent(), start + 0.12f, 0.15f),
                            Timed(new FinishAttackEvent(), start + 0.45f, 0.05f),
                        },
                        "camera", "shake", "attack", "카메라", "연출"),

                    new EventPreset(
                        "special_break_attack",
                        "브레이크 특수공격",
                        "SpecialBreakAttackEvent와 CameraEffect를 기본 타이밍으로 추가합니다.",
                        start => new MotionEventBase[]
                        {
                            Timed(new CameraEffectEvent(), start + 0.00f, 0.25f),
                            Timed(new SpecialBreakAttackEvent(), start + 0.18f, 0.05f),
                        },
                        "break", "special", "finish", "브레이크", "특수공격", "처형"),

                    new EventPreset(
                        "projectile_basic",
                        "투사체 발사 기본",
                        "PlaySound와 SpawnProjectile을 기본 타이밍으로 추가합니다.",
                        start => new MotionEventBase[]
                        {
                            Timed(new PlaySoundEvent(), start + 0.00f, 0.05f),
                            Timed(new SpawnProjectileEvent(), start + 0.10f, 0.05f),
                        },
                        "projectile", "shoot", "arrow", "bullet", "투사체", "발사"),
                };
            }

            static EventMeta Meta<T>(string displayName, EventCategory category, string description, params string[] aliases)
                where T : MotionEventBase
            {
                return new EventMeta(typeof(T), displayName, category, description, aliases);
            }

            static MotionEventBase Timed(MotionEventBase evt, float start, float duration)
            {
                ApplyDefaultTime(evt, start, duration);
                return evt;
            }
        }
    }
}
