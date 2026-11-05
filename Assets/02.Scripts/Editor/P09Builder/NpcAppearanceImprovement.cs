using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;
using UPlayGround.Animation;
using UPlayGround.Data.Actor;
using UPlayGround.Data.Actor.Animation;
using UPlayGround.EditorTools;

namespace UPlayGround.Editor.P09Builder
{
    /// <summary>기존 NPC의 모션 참조만 원문 백업·복구가 가능한 방식으로 교정한다.</summary>
    public static class NpcAppearanceImprovement
    {
        public const string Output = "output/NpcAppearanceImprovement";
        private const string EmptyField = "  _motionSet: {fileID: 0}";
        internal static readonly UPlayGround.Gameplay.Tag.GameplayTag[] Slots = { MotionTags.Idle, MotionTags.Walk, MotionTags.Talk_1 };

        [Serializable]
        private sealed class Patch
        {
            public string id, path, guid, beforeHash, afterHash, metaHash, value;
        }

        /// <summary>전체 사전 검사와 145번째 쓰기 실패 복구 후 한 필드만 갱신한다.</summary>
        [UPlaygroundTool("p09.npc-appearance.repair-motion")]
        public static void RepairMotion()
        {
            RequireEditMode();
            Directory.CreateDirectory(Output);
            if (Directory.Exists(Output + "/backup"))
                throw new InvalidOperationException("기존 백업이 있습니다. 재실행 전에 이전 상태를 확인하세요.");
            var source = AssetDatabase.LoadAssetAtPath<NpcAppearanceSetSO>(NpcAppearanceGenerator.RulesPath);
            RequireClean(source);
            var configs = NpcAppearanceGenerator.CreateConfigs(source);
            if (configs.Count != 288) throw new InvalidOperationException("288종 계약 불일치");
            var rows = new List<Patch>();
            var report = new List<string>();
            foreach (var config in configs)
            {
                string id = config.ManualName;
                string path = $"{NpcAppearanceGenerator.PrefabRoot}/Npc/{id}/{id}.prefab";
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                RequireClean(prefab);
                foreach (var component in prefab.GetComponentsInChildren<Component>(true)) RequireClean(component);
                var preset = AssetDatabase.LoadAssetAtPath<BuildPreset>($"{NpcAppearanceGenerator.PresetFolder}/{id}_BuildPreset.asset");
                RequireClean(preset);
                if (JsonUtility.ToJson(preset.config) != JsonUtility.ToJson(config)) throw new InvalidOperationException(id + ": 프리셋 불일치");
                var actor = prefab.GetComponent<NpcActor>();
                var serialized = new SerializedObject(actor);
                var definition = serialized.FindProperty("_definition").objectReferenceValue as ActorDefinitionSO;
                var data = serialized.FindProperty("_data").objectReferenceValue as NpcActorSO;
                RequireClean(definition); RequireClean(data);
                if (definition.actorId != id || definition.prefab != prefab || definition.npcData != data)
                    throw new InvalidOperationException(id + ": 연결 불일치");
                var animators = prefab.GetComponentsInChildren<ActorAnimator>(true);
                if (animators.Length != 1 || animators[0].MotionSet != null) throw new InvalidOperationException(id + ": 예상한 null 필드가 아님");
                var set = ApplyWeaponStep.LoadNpcMotionSet(config.Sex);
                RequireClean(set);
                foreach (var slot in Slots)
                {
                    var motion = set.GetMotionSetAsset(slot);
                    RequireClean(motion);
                    if (motion.motionSet?.motions == null || motion.motionSet.motions.Count == 0
                        || motion.motionSet.motions.Any(m => m == null || m.motionClip == null))
                        throw new InvalidOperationException(id + ": 슬롯·클립 누락 " + slot);
                    foreach (var clip in motion.motionSet.motions.Select(m => m.motionClip))
                        report.Add(config.Sex + "|" + slot + "|" + AssetDatabase.GetAssetPath(motion) + "|" + AssetDatabase.GetAssetPath(clip) + "|" + clip.name + "|" + clip.length);
                }
                string text = ReadExact(path);
                if (Count(text, EmptyField) != 1) throw new InvalidOperationException(path + ": 단일 필드 계약 불일치");
                string value = "  _motionSet: {fileID: 11400000, guid: " + AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(set)) + ", type: 2}";
                rows.Add(new Patch { id = id, path = path, guid = AssetDatabase.AssetPathToGUID(path),
                    beforeHash = Hash(path), afterHash = HashBytes(Encoding.UTF8.GetBytes(text.Replace(EmptyField, value))),
                    metaHash = Hash(path + ".meta"), value = value });
            }
            RequireClean(AssetDatabase.LoadMainAssetAtPath(PathConfig.ActorDefinitionRoot + "/ActorDatabase.asset"));
            File.WriteAllLines(Output + "/motion-clips.txt", report.Distinct());
            File.WriteAllLines(Output + "/patch-plan.jsonl", rows.Select(JsonUtility.ToJson));
            // 원문과 메타는 전체 사전 검사가 끝난 뒤, 첫 쓰기 전에 모두 보관한다.
            foreach (var row in rows)
            {
                string backup = Output + "/backup/" + row.path;
                Directory.CreateDirectory(Path.GetDirectoryName(backup));
                File.Copy(row.path, backup); File.Copy(row.path + ".meta", backup + ".meta");
                if (Hash(backup) != row.beforeHash || Hash(backup + ".meta") != row.metaHash) throw new IOException("백업 불일치");
            }
            try { ApplyRows(rows, 145); throw new InvalidOperationException("실패 주입이 실행되지 않음"); }
            catch (ApplicationException) { }
            if (rows.Any(r => Hash(r.path) != r.beforeHash || Hash(r.path + ".meta") != r.metaHash)) throw new IOException("주입 후 복구 불일치");
            File.WriteAllText(Output + "/rollback-check.txt", "145번째 원본 쓰기 직후 실패 주입: 288개 원문·메타 SHA-256 동일. Undo 호출 0.\n");
            ApplyRows(rows, 0);
            File.WriteAllText(Output + "/repair-result.txt", "288개 _motionSet만 수정. Unity 재로드 연결·GUID·슬롯·클립 검증 통과. 외형 필드는 원문 보존.\n");
        }

        private static void ApplyRows(List<Patch> rows, int failAt)
        {
            var written = new List<Patch>();
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var row in rows)
                {
                    if (Hash(row.path) != row.beforeHash || Hash(row.path + ".meta") != row.metaHash) throw new IOException("동시 변경 거부: " + row.path);
                    written.Add(row);
                    File.WriteAllBytes(row.path, Encoding.UTF8.GetBytes(ReadExact(row.path).Replace(EmptyField, row.value)));
                    if (Hash(row.path) != row.afterHash) throw new IOException("쓰기 불일치");
                    if (written.Count == failAt) throw new ApplicationException("중간 실패 주입");
                }
            }
            catch
            {
                RestoreRows(written);
                throw;
            }
            finally { AssetDatabase.StopAssetEditing(); }
            try
            {
                foreach (var row in rows)
                {
                    AssetDatabase.ImportAsset(row.path, ImportAssetOptions.ForceUpdate);
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(row.path);
                    var animator = prefab.GetComponentInChildren<ActorAnimator>(true);
                    if (Hash(row.path) != row.afterHash || Hash(row.path + ".meta") != row.metaHash
                        || AssetDatabase.AssetPathToGUID(row.path) != row.guid || animator.MotionSet == null
                        || Slots.Any(s => animator.MotionSet.GetMotionSetAsset(s) == null)) throw new IOException("재로드 검증 실패: " + row.path);
                }
            }
            catch
            {
                RestoreRows(written);
                foreach (var row in written) AssetDatabase.ImportAsset(row.path, ImportAssetOptions.ForceUpdate);
                throw;
            }
        }

        private static void RestoreRows(List<Patch> rows)
        {
            foreach (var row in rows)
            {
                File.Copy(Output + "/backup/" + row.path, row.path, true);
                if (Hash(row.path) != row.beforeHash) throw new IOException("원문 복구 실패: " + row.path);
            }
        }

        internal static void RequireEditMode()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating
                || UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage() != null)
                throw new InvalidOperationException("컴파일·임포트·프리팹 편집이 없는 Edit Mode가 필요합니다.");
        }

        internal static void RequireClean(UnityEngine.Object asset)
        {
            if (asset == null || EditorUtility.IsDirty(asset)) throw new InvalidOperationException("누락 또는 dirty 자산: " + asset);
        }

        private static string ReadExact(string path)
        {
            byte[] bytes = File.ReadAllBytes(path);
            string text = new UTF8Encoding(false, true).GetString(bytes);
            if (!Encoding.UTF8.GetBytes(text).SequenceEqual(bytes)) throw new IOException("UTF-8 원문 복원 불가");
            return text;
        }

        private static int Count(string text, string value) => (text.Length - text.Replace(value, "").Length) / value.Length;
        private static string Hash(string path) => HashBytes(File.ReadAllBytes(path));
        private static string HashBytes(byte[] bytes)
        {
            using var hash = SHA256.Create();
            return BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-", "");
        }
    }
}
