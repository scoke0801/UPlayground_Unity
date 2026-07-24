#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using UPlayGround.Ability.Core;
using UPlayGround.Components;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Party;
using UPlayGround.Data.Stat;

namespace UPlayGround.Tool.Editor.Balance
{
    /// <summary>
    /// 현재 Player 데이터(PartyConfig 성장 데이터 + 캐릭터 모델 공격 데이터)를 기반으로
    /// <see cref="BalanceScenarioAsset"/>을 자동 생성/갱신하는 에디터 전용 서비스.
    ///
    /// 플레이어 파생 4개 필드(playerCharacter / playerStatData / playerAbilitySet / playerLevel)만 채우고,
    /// 인카운터·방어 가정값은 건드리지 않는다.
    /// - 신규 생성: 인카운터/방어 가정값은 BalanceScenarioAsset의 필드 기본값을 그대로 사용.
    /// - 기존 갱신: 사용자가 손으로 튜닝한 인카운터/방어 가정값을 보존하고 플레이어 4개 필드만 새로고침.
    /// </summary>
    public static class BalanceScenarioGenerator
    {
        private const string ScenarioFolder = "Assets/10.Datas/Balance/Scenarios";
        private const string ModelPrefabFolder = "Assets/03.Prefabs";

        public sealed class ScenarioGenResult
        {
            public BalanceScenarioAsset Asset;
            public CharacterActorType Character;
            public string Path;
            public int Level;
            public bool HasStat;
            public bool HasAttack;
            public bool Created; // true=신규 생성, false=기존 갱신
            public string Note;
        }

        /// <summary>프로젝트에서 첫 번째 PartyConfigSO를 찾는다. 없으면 null.</summary>
        public static PartyConfigSO FindPartyConfig()
        {
            string[] guids = AssetDatabase.FindAssets("t:PartyConfigSO");
            return guids.Length > 0
                ? AssetDatabase.LoadAssetAtPath<PartyConfigSO>(AssetDatabase.GUIDToAssetPath(guids[0]))
                : null;
        }

        /// <summary>
        /// 에디터 밸런스 도구의 대표 캐릭터를 해석한다.
        /// 시작 캐릭터는 런타임 선택값이므로 growthData의 첫 유효 캐릭터를 사용하고,
        /// 성장 데이터도 비어 있으면 기본 고정 캐릭터 Bokusei를 사용한다.
        /// </summary>
        public static CharacterActorType ResolveActiveCharacter(PartyConfigSO config)
        {
            if (config?.growthData != null)
                foreach (PartyMemberGrowthSO growth in config.growthData)
                    if (growth != null && growth.characterType != CharacterActorType.None)
                        return growth.characterType;
            return CharacterActorType.Bokusei;
        }

        /// <summary>현재 조작 캐릭터 1명에 대한 시나리오를 생성/갱신한다.</summary>
        public static ScenarioGenResult GenerateForActiveCharacter(PartyConfigSO config)
        {
            config = config != null ? config : FindPartyConfig();
            CharacterActorType character = ResolveActiveCharacter(config);

            Dictionary<CharacterActorType, AbilitySetSO> attackMap = BuildAttackDataMap();
            List<AbilitySetSO> allAttackData = LoadAllPlayerAttackData();

            ScenarioGenResult result = GenerateOne(config, character, attackMap, allAttackData);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return result;
        }

        /// <summary>PartyConfig.growthData에 등록된 모든 파티 캐릭터에 대해 시나리오를 일괄 생성/갱신한다.</summary>
        public static List<ScenarioGenResult> GenerateForAllPartyMembers(PartyConfigSO config)
        {
            config = config != null ? config : FindPartyConfig();
            var results = new List<ScenarioGenResult>();
            if (config == null || config.growthData == null)
                return results;

            Dictionary<CharacterActorType, AbilitySetSO> attackMap = BuildAttackDataMap();
            List<AbilitySetSO> allAttackData = LoadAllPlayerAttackData();

            var seen = new HashSet<CharacterActorType>();
            for (int i = 0; i < config.growthData.Count; i++)
            {
                PartyMemberGrowthSO growth = config.growthData[i];
                if (growth == null || !seen.Add(growth.characterType))
                    continue;
                results.Add(GenerateOne(config, growth.characterType, attackMap, allAttackData));
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return results;
        }

        private static ScenarioGenResult GenerateOne(
            PartyConfigSO config,
            CharacterActorType character,
            Dictionary<CharacterActorType, AbilitySetSO> attackMap,
            List<AbilitySetSO> allAttackData)
        {
            EnsureFolder(ScenarioFolder);

            PartyMemberGrowthSO growth = FindGrowth(config, character);
            AttributeProfileSO profile =
                growth != null ? growth.baseProfile : null;
            int level = growth != null ? Mathf.Max(1, growth.initialLevel) : 1;

            AbilitySetSO attackData = ResolveAttackData(character, attackMap, allAttackData, out string attackSource);

            string path = $"{ScenarioFolder}/BalanceScenario_{SanitizeFileName(character.ToString())}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<BalanceScenarioAsset>(path);
            bool created = asset == null;

            if (created)
            {
                asset = ScriptableObject.CreateInstance<BalanceScenarioAsset>();
            }
            else
            {
                Undo.RecordObject(asset, "Generate Balance Scenario From Player");
            }

            // 플레이어 파생 4개 필드만 갱신. 인카운터/방어 가정값(targetDuration, hitReceiveRate 등)은 보존한다.
            asset.playerCharacter = character;
            asset.playerAttributeProfile = profile;
            asset.playerAbilitySet = attackData;
            asset.playerLevel = level;

            if (created)
                AssetDatabase.CreateAsset(asset, path);
            EditorUtility.SetDirty(asset);

            return new ScenarioGenResult
            {
                Asset = asset,
                Character = character,
                Path = path,
                Level = level,
                HasStat = profile != null,
                HasAttack = attackData != null,
                Created = created,
                Note = BuildNote(growth, profile, attackSource),
            };
        }

        /// <summary>
        /// 캐릭터 → AbilitySetSO 해석. 우선순위:
        /// 1) Model 프리팹의 CharacterModelData.abilitySet (캐릭터 타입 일치)
        /// 2) 이름에 캐릭터 이름이 포함된 AbilitySetSO
        /// 3) 프로젝트에 AbilitySetSO가 하나뿐이면 공용 기본값으로 사용
        /// 모두 실패하면 null(추정기는 manualPlayerDps로 폴백).
        /// </summary>
        private static AbilitySetSO ResolveAttackData(
            CharacterActorType character,
            Dictionary<CharacterActorType, AbilitySetSO> attackMap,
            List<AbilitySetSO> allAttackData,
            out string source)
        {
            if (attackMap.TryGetValue(character, out AbilitySetSO fromPrefab) && fromPrefab != null)
            {
                source = $"Model 프리팹: {fromPrefab.name}";
                return fromPrefab;
            }

            string token = character.ToString();
            for (int i = 0; i < allAttackData.Count; i++)
            {
                AbilitySetSO candidate = allAttackData[i];
                if (candidate != null && candidate.name.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    source = $"이름 매칭: {candidate.name}";
                    return candidate;
                }
            }

            if (allAttackData.Count == 1 && allAttackData[0] != null)
            {
                source = $"단일 기본 데이터: {allAttackData[0].name}";
                return allAttackData[0];
            }

            source = "매칭 실패(null) → manualPlayerDps 폴백";
            return null;
        }

        /// <summary>모든 프리팹을 스캔해 CharacterModelData가 가진 캐릭터 타입 → 공격 데이터 맵을 만든다.</summary>
        private static Dictionary<CharacterActorType, AbilitySetSO> BuildAttackDataMap()
        {
            var map = new Dictionary<CharacterActorType, AbilitySetSO>();
            // 캐릭터 모델 프리팹은 03.Prefabs에 있으므로 그 폴더만 스캔한다(ExternalAssets 전체 로드 방지).
            string[] guids = AssetDatabase.IsValidFolder(ModelPrefabFolder)
                ? AssetDatabase.FindAssets("t:Prefab", new[] { ModelPrefabFolder })
                : AssetDatabase.FindAssets("t:Prefab");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (go == null)
                    continue;

                var model = go.GetComponentInChildren<CharacterModelData>(true);
                if (model == null || model.abilitySet == null)
                    continue;

                // 같은 캐릭터 타입의 프리팹이 여러 개면 먼저 발견된 것을 사용한다.
                if (!map.ContainsKey(model.characterType))
                    map[model.characterType] = model.abilitySet;
            }

            return map;
        }

        private static PartyMemberGrowthSO FindGrowth(PartyConfigSO config, CharacterActorType character)
        {
            if (config?.growthData == null)
                return null;

            for (int i = 0; i < config.growthData.Count; i++)
            {
                PartyMemberGrowthSO g = config.growthData[i];
                if (g != null && g.characterType == character)
                    return g;
            }

            return null;
        }

        private static string BuildNote(
            PartyMemberGrowthSO growth,
            AttributeProfileSO profile,
            string attackSource)
        {
            var builder = new StringBuilder();
            if (growth == null)
                builder.Append("성장 데이터 없음(레벨 1 가정) / ");
            else if (profile == null)
                builder.Append("growth.baseProfile 없음(Attribute 기본값 가정) / ");
            else
                builder.Append($"Profile: {profile.name} / ");

            builder.Append($"Attack: {attackSource}");
            return builder.ToString();
        }

        private static List<AbilitySetSO> LoadAllPlayerAttackData()
        {
            var list = new List<AbilitySetSO>();
            string[] guids = AssetDatabase.FindAssets("t:AbilitySetSO");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var asset = AssetDatabase.LoadAssetAtPath<AbilitySetSO>(path);
                if (asset != null && asset.combatBindings.Count > 0)
                    list.Add(asset);
            }

            return list;
        }

        private static string SanitizeFileName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "Unnamed";

            foreach (char invalid in System.IO.Path.GetInvalidFileNameChars())
                value = value.Replace(invalid, '_');

            return value.Replace('/', '_').Replace('\\', '_').Replace(' ', '_');
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
#endif
