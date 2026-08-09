#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Data.Actor;
using UPlayGround.Data.EnumType;
using UPlayGround.Manager;

namespace UPlayGround.UI.DevCheat
{
    /// <summary>UI_DevCheatPanel — 스폰 탭(ActorDatabase 액터를 플레이어 전방에 소환).</summary>
    public partial class UI_DevCheatPanel
    {
        private TMP_InputField  _spawnSearch;
        private TMP_InputField  _spawnCountInput;
        private TMP_InputField  _spawnDistanceInput;
        private RectTransform   _spawnListContent;
        private TextMeshProUGUI _spawnName, _spawnIdText, _spawnInfoText;

        private string _spawnSelectedId;
        private string _spawnSelectedName;

        // Monster / NPC 등 타입 필터. null이면 전체.
        private ActorType? _spawnTypeFilter;

        private int   SpawnCount    => ParseIntOr(_spawnCountInput, 1, 1, 20);
        private float SpawnDistance => ParseFloatOr(_spawnDistanceInput, 5f, 0f, 50f);

        private void BuildSpawnTab(RectTransform panel)
        {
            AddHLG(panel.gameObject, 12, 12);

            // 좌: 검색 + 타입 필터 + 리스트
            var center = NewRect("SpawnCenter", panel);
            SetSize(center.gameObject, flexW: 1);
            AddImage(center.gameObject, PanelBg);
            var cv = AddVLG(center.gameObject, 8, 8);
            cv.childForceExpandHeight = false;

            _spawnSearch = MakeInput(center, "ActorId 또는 이름 검색", _ => RefreshSpawnList());
            SetSize(_spawnSearch.gameObject, minH: 40, prefH: 40);

            var filterRow = NewRect("SpawnFilter", center);
            SetSize(filterRow.gameObject, minH: 40, prefH: 40);
            AddHLG(filterRow.gameObject, 6, 0);
            MakeButton(filterRow, "전체", BtnBg, () => SetSpawnTypeFilter(null), 15);
            MakeButton(filterRow, "몬스터", BtnBg, () => SetSpawnTypeFilter(ActorType.Monster), 15);
            MakeButton(filterRow, "NPC", BtnBg, () => SetSpawnTypeFilter(ActorType.NPC), 15);
            MakeButton(filterRow, "플레이어", BtnBg, () => SetSpawnTypeFilter(ActorType.Player), 15);

            var listScroll = MakeScroll(center, out _);
            SetSize(((RectTransform)listScroll.parent.parent).gameObject, flexH: 1);
            _spawnListContent = listScroll;

            // 우: 상세 + 스폰 옵션
            var right = NewRect("SpawnDetail", panel);
            SetSize(right.gameObject, minW: 360, prefW: 360);
            AddImage(right.gameObject, PanelBg);
            var rv = AddVLG(right.gameObject, 8, 12);
            rv.childForceExpandHeight = false;

            _spawnName = MakeText(right, "-", 22, TextMain, TextAlignmentOptions.Center);
            SetSize(_spawnName.gameObject, minH: 34, prefH: 34);
            _spawnIdText = MakeText(right, "ActorId  -", 15, TextSub, TextAlignmentOptions.Center);
            _spawnInfoText = MakeText(right, "-", 15, TextSub, TextAlignmentOptions.Left);
            SetSize(_spawnInfoText.gameObject, minH: 90, prefH: 90);

            MakeText(right, "마리 수 (1~20)", 15, TextSub);
            _spawnCountInput = MakeInput(right, "1", null, TMP_InputField.ContentType.IntegerNumber);
            _spawnCountInput.text = "1";

            MakeText(right, "전방 거리 (m)", 15, TextSub);
            _spawnDistanceInput = MakeInput(right, "5", null, TMP_InputField.ContentType.DecimalNumber);
            _spawnDistanceInput.text = "5";

            var spawnBtn = MakeButton(right, "플레이어 전방에 스폰", AccentBtn, SpawnSelected, 18);
            SetSize(spawnBtn.gameObject, minH: 48, prefH: 48);
        }

        private void SetSpawnTypeFilter(ActorType? type)
        {
            _spawnTypeFilter = type;
            RefreshSpawnList();
        }

        private void RefreshSpawnList()
        {
            if (_spawnListContent == null) return;
            ClearChildren(_spawnListContent);

            var cheat = CheatManager.Instance;
            IReadOnlyList<ActorDefinitionSO> defs = cheat != null
                ? cheat.GetSpawnableDefinitions()
                : Array.Empty<ActorDefinitionSO>();

            if (defs.Count == 0)
            {
                MakeText(_spawnListContent, "ActorDatabase 로드 대기 중…", 16, TextSub);
                RefreshSpawnDetail();
                return;
            }

            string search = _spawnSearch != null ? _spawnSearch.text : string.Empty;
            int rowIndex = 0;
            for (int i = 0; i < defs.Count; i++)
            {
                ActorDefinitionSO def = defs[i];
                if (def == null || string.IsNullOrEmpty(def.actorId)) continue;
                if (_spawnTypeFilter.HasValue && !def.actorType.HasFlag(_spawnTypeFilter.Value)) continue;
                if (!MatchesSpawnSearch(def, search)) continue;

                string id   = def.actorId;
                string name = string.IsNullOrEmpty(def.displayName) ? id : def.displayName;
                bool   hasPrefab = def.prefab != null;

                var row = NewRect("Row", _spawnListContent);
                SetSize(row.gameObject, minH: 44, prefH: 44);
                var bg = AddImage(row.gameObject, id == _spawnSelectedId ? AccentBtn : (rowIndex++ % 2 == 0 ? RowBg : RowBgAlt));
                var btn = row.gameObject.AddComponent<Button>();
                btn.targetGraphic = bg;
                btn.onClick.AddListener(() => SelectSpawn(id, name));

                var rh = AddHLG(row.gameObject, 8, 8);
                rh.childForceExpandWidth = false;

                var nameT = MakeText(row, name, 16, hasPrefab ? TextMain : TextSub);
                SetSize(nameT.gameObject, flexW: 1);
                MakeText(row, def.actorType.ToString(), 14, TextSub, TextAlignmentOptions.Center);
                var lvT = MakeText(row, hasPrefab ? $"Lv{def.EffectiveLevel}" : "프리팹 X",
                    15, hasPrefab ? Positive : DangerBtn, TextAlignmentOptions.Right);
                SetSize(lvT.gameObject, minW: 72, prefW: 72);
            }

            if (_spawnListContent.childCount == 0)
                MakeText(_spawnListContent, "검색 결과가 없습니다.", 16, TextSub);

            RefreshSpawnDetail();
        }

        private static bool MatchesSpawnSearch(ActorDefinitionSO def, string search)
        {
            if (string.IsNullOrWhiteSpace(search)) return true;
            search = search.Trim();
            if (def.actorId.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return !string.IsNullOrEmpty(def.displayName) &&
                   def.displayName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void SelectSpawn(string id, string name)
        {
            _spawnSelectedId   = id;
            _spawnSelectedName = name;
            RefreshSpawnList();
        }

        private void RefreshSpawnDetail()
        {
            ActorDefinitionSO def = FindSpawnDefinition(_spawnSelectedId);
            if (def == null)
            {
                if (_spawnName != null)     _spawnName.text = "-";
                if (_spawnIdText != null)   _spawnIdText.text = "ActorId  -";
                if (_spawnInfoText != null) _spawnInfoText.text = "-";
                return;
            }

            if (_spawnName != null)
                _spawnName.text = string.IsNullOrEmpty(def.displayName) ? def.actorId : def.displayName;
            if (_spawnIdText != null)
                _spawnIdText.text = $"ActorId  {def.actorId}";
            if (_spawnInfoText != null)
                _spawnInfoText.text =
                    $"타입      {def.actorType}\n" +
                    $"등급      {def.EffectiveGrade}   Lv {def.EffectiveLevel}\n" +
                    $"프리팹    {(def.prefab != null ? def.prefab.name : "<color=#D04448>없음</color>")}\n" +
                    $"AbilitySet {(def.EffectiveAbilitySet != null ? def.EffectiveAbilitySet.name : "없음")}";
        }

        private void SpawnSelected()
        {
            if (string.IsNullOrEmpty(_spawnSelectedId)) return;
            CheatManager.Instance?.SpawnActorInFrontOfPlayer(
                _spawnSelectedId, SpawnCount, SpawnDistance, _spawnSelectedName);
        }

        private static ActorDefinitionSO FindSpawnDefinition(string actorId)
        {
            if (string.IsNullOrEmpty(actorId)) return null;
            var cheat = CheatManager.Instance;
            if (cheat == null) return null;

            IReadOnlyList<ActorDefinitionSO> defs = cheat.GetSpawnableDefinitions();
            for (int i = 0; i < defs.Count; i++)
                if (defs[i] != null && defs[i].actorId == actorId)
                    return defs[i];
            return null;
        }

        private static int ParseIntOr(TMP_InputField field, int fallback, int min, int max)
        {
            if (field == null || !int.TryParse(field.text, out int v)) return fallback;
            return Mathf.Clamp(v, min, max);
        }

        private static float ParseFloatOr(TMP_InputField field, float fallback, float min, float max)
        {
            if (field == null || !float.TryParse(field.text, out float v)) return fallback;
            return Mathf.Clamp(v, min, max);
        }
    }
}
#endif
