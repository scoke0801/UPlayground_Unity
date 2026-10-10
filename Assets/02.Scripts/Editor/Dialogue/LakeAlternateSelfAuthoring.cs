using System;
using System.Linq;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UPlayGround.Animation;
using UPlayGround.Components;
using UPlayGround.Data.Actor;
using UPlayGround.Data.EnumType;
using UPlayGround.Editor.P09Builder;

namespace UPlayGround.Editor.Dialogue
{
    /// <summary>선택 주인공의 현재 모델과 검증된 카타나 전투를 전용 분신 프리팹으로 연결한다.</summary>
    public static class LakeAlternateSelfAuthoring
    {
        private const string ReferencePath = "Assets/10.Datas/Actor/DataBase/Boss/MonsterBossAlternateSelfRaon.asset";

        [UPlayGround.EditorTools.UPlaygroundTool("UPlayGround/내러티브/대화/분신 캐스팅 구성")]
        public static void Build()
        {
            BuildVariant(CharacterActorType.Raon, ReferencePath);
            BuildVariant(CharacterActorType.Arin, "Assets/10.Datas/Actor/DataBase/Boss/MonsterBossAlternateSelfNenmir.asset");
            BakeCastProfiles();
        }

        /// <summary>분신의 누락 워프 캐시를 채우고 현재 활성 빌드 씬을 Windows로 검증한다.</summary>
        [UPlayGround.EditorTools.UPlaygroundTool("UPlayGround/내러티브/대화/스토리 Windows 빌드 검증")]
        public static void BuildWindows()
        {
            BakeCastProfiles();
            string[] scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
            if (scenes.Length == 0 || scenes.Any(path => string.IsNullOrEmpty(path) || !File.Exists(path)))
                throw new BuildFailedException("빌드 씬 경로가 유효하지 않습니다.");
            // 배치 테스트 뒤 빈 임시 씬이 열려 있으면 lilToon이 이전 씬 복원 중 빈 경로를 열게 된다.
            EditorSceneManager.OpenScene(scenes[0]);
            const string directory = "tmp/narrative-overhaul/Player";
            Directory.CreateDirectory(directory);
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development,
                locationPathName = directory + "/UPlayground.exe"
            });
            File.WriteAllText("tmp/narrative-overhaul/player-build.txt",
                $"result={report.summary.result}, errors={report.summary.totalErrors}, warnings={report.summary.totalWarnings}, bytes={report.summary.totalSize}");
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new BuildFailedException("스토리 Windows 빌드에 실패했습니다.");
        }

        private static void BakeCastProfiles()
        {
            var definition = Load<ActorDefinitionSO>(ReferencePath);
            var animator = definition.prefab.GetComponent<ActorAnimator>();
            WarpRootMotionBatchBakeWindow.BakeMissingProfiles(animator.MotionSet.abilityMotions.Values);
        }

        private static void BuildVariant(CharacterActorType character, string definitionPath)
        {
            string prefabPath = "Assets/03.Prefabs/Actor/Monster/Humanoid/MonsterActor_AlternateSelf" + character + ".prefab";
            string modelPath = "Assets/03.Prefabs/Actor/Player/Models/PlayerModel_" + character + ".prefab";
            var definition = Load<ActorDefinitionSO>(definitionPath);
            var reference = Load<ActorDefinitionSO>(ReferencePath);
            var model = Load<GameObject>(modelPath);
            if (definition.monsterProfile == null)
                throw new InvalidOperationException("분신 전용 프로필이 없습니다.");
            string definitionBackup = EditorJsonUtility.ToJson(definition);
            string profileBackup = EditorJsonUtility.ToJson(definition.monsterProfile);
            bool created = false;
            GameObject instance = null;
            try
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab == null)
                {
                    // 기존 프리팹을 교체하지 않는다. 새 인스턴스의 구성·검사가 끝난 뒤 전용 경로에 저장한다.
                    instance = UnityEngine.Object.Instantiate(model);
                    instance.name = "MonsterActor_AlternateSelf" + character;
                    instance.SetActive(false);
                    var playerAnimator = instance.GetComponent<PlayerActorAnimator>();
                    var sourceAnimator = reference.prefab.GetComponentInChildren<ActorAnimator>(true);
                    if (playerAnimator == null || sourceAnimator == null)
                        throw new InvalidOperationException("주인공 모델 또는 카타나 기준 모션이 없습니다.");
                    var original = new SerializedObject(playerAnimator);
                    var animator = instance.AddComponent<ActorAnimator>();
                    var destination = new SerializedObject(animator);
                    destination.FindProperty("_motionSet").objectReferenceValue =
                        new SerializedObject(sourceAnimator).FindProperty("_motionSet").objectReferenceValue;
                    destination.FindProperty("_upperBodyMask").objectReferenceValue =
                        original.FindProperty("_upperBodyMask").objectReferenceValue;
                    destination.FindProperty("_eventExecutor").objectReferenceValue = instance.GetComponent<MotionEventExecutor>();
                    destination.ApplyModifiedPropertiesWithoutUndo();
                    UnityEngine.Object.DestroyImmediate(playerAnimator);

                    CopyPresentationAnchor(reference.prefab, instance, "HpBarSocket");
                    CopyPresentationAnchor(reference.prefab, instance, "LockOn");
                    new EnemyActorTemplate().AttachComponents(instance, null);
                    SetObject(instance.GetComponent<EnemyCombat>(), "_abilitySet", reference.EffectiveAbilitySet);
                    SetObject(instance.GetComponent<EnemyAIController>(), "_behaviorData", reference.EffectiveBehaviorData);
                    SetObject(instance.GetComponent<PoiseStat>(), "_data", definition.poiseData);
                    var actor = new SerializedObject(instance.GetComponent<MonsterActor>());
                    actor.FindProperty("_actorId").stringValue = definition.actorId;
                    actor.FindProperty("_characterActorType").intValue = (int)character;
                    actor.ApplyModifiedPropertiesWithoutUndo();
                    if (instance.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length == 0
                        || instance.GetComponent<Animator>().avatar == null)
                        throw new InvalidOperationException("분신 모델 또는 아바타가 없습니다.");
                    foreach (Transform child in instance.GetComponentsInChildren<Transform>(true))
                        if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) > 0)
                            throw new InvalidOperationException("분신 모델의 Missing Script: " + child.name);
                    instance.SetActive(true);
                    prefab = PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
                    created = prefab != null;
                    if (!created) throw new InvalidOperationException("분신 프리팹 저장에 실패했습니다.");
                }

                Undo.RecordObjects(new UnityEngine.Object[] { definition, definition.monsterProfile }, "분신 캐스팅");
                definition.prefab = prefab;
                definition.characterType = character;
                definition.abilitySet = reference.EffectiveAbilitySet;
                definition.behaviorData = reference.EffectiveBehaviorData;
                definition.monsterProfile.abilitySet = reference.EffectiveAbilitySet;
                definition.monsterProfile.behaviorData = reference.EffectiveBehaviorData;
                EditorUtility.SetDirty(definition);
                EditorUtility.SetDirty(definition.monsterProfile);
                AssetDatabase.SaveAssetIfDirty(definition);
                AssetDatabase.SaveAssetIfDirty(definition.monsterProfile);
                Debug.Log($"{character} 분신의 기존 Actor ID를 보존하고 현재 주인공 모델·카타나 전투를 연결했습니다.");
            }
            catch
            {
                EditorJsonUtility.FromJsonOverwrite(definitionBackup, definition);
                EditorJsonUtility.FromJsonOverwrite(profileBackup, definition.monsterProfile);
                EditorUtility.SetDirty(definition);
                EditorUtility.SetDirty(definition.monsterProfile);
                AssetDatabase.SaveAssetIfDirty(definition);
                AssetDatabase.SaveAssetIfDirty(definition.monsterProfile);
                if (created) AssetDatabase.DeleteAsset(prefabPath);
                throw;
            }
            finally
            {
                if (instance != null) UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        private static void CopyPresentationAnchor(GameObject source, GameObject destination, string name)
        {
            if (destination.GetComponentsInChildren<Transform>(true).Any(item => item.name == name)) return;
            Transform anchor = source.GetComponentsInChildren<Transform>(true).FirstOrDefault(item => item.name == name);
            if (anchor == null) throw new InvalidOperationException("기준 프리팹 소켓 누락: " + name);
            var clone = UnityEngine.Object.Instantiate(anchor.gameObject, destination.transform);
            clone.name = name;
            clone.transform.localPosition = source.transform.InverseTransformPoint(anchor.position);
        }

        private static void SetObject(UnityEngine.Object owner, string field, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(owner);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static T Load<T>(string path) where T : UnityEngine.Object =>
            AssetDatabase.LoadAssetAtPath<T>(path) ?? throw new InvalidOperationException("필수 에셋 누락: " + path);
    }
}
