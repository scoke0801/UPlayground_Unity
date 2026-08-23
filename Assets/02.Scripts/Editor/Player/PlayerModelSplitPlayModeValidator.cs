#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UPlayGround.Animation.Editor;
using UPlayGround.Components;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Editor.Player
{
    /// <summary>Motion Editor가 사용하는 실제 Play Mode 경로로 플레이어 프리뷰를 검증한다.</summary>
    [InitializeOnLoad]
    public static class PlayerModelSplitPlayModeValidator
    {
        private const string CatalogPath =
            "Assets/10.Datas/System/MotionPreviewCatalog.asset";
        private const string RequestedKey =
            "UPlayGround.PlayerModelSplit.PlayModeValidationRequested";
        private const string SucceededKey =
            "UPlayGround.PlayerModelSplit.PlayModeValidationSucceeded";
        private const string PreviewEntriesKey =
            "UPlayGround.PlayerModelSplit.PlayModeValidationEntries";

        [Serializable]
        private sealed class PreviewEntry
        {
            public string id;
            public string prefabPath;
        }

        [Serializable]
        private sealed class PreviewRequest
        {
            public List<PreviewEntry> entries = new();
        }

        private static IReadOnlyList<PreviewEntry> s_entries;
        private static int s_entryIndex;
        private static GameObject s_instance;
        private static IMotionPreviewSubject s_subject;
        private static PreviewEntry s_currentEntry;
        private static double s_validateAt;

        static PlayerModelSplitPlayModeValidator()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        /// <summary>프리뷰 씬에 진입해 모든 플레이어 프리뷰를 순차 생성·바인딩한다.</summary>
        public static void RunBatch()
        {
            try
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    throw new InvalidOperationException(
                        "Play Mode가 종료된 상태에서 검증을 시작해야 합니다.");
                }

                MotionPreviewCatalogSO catalog = LoadCatalog();
                string scenePath = AssetDatabase.GetAssetPath(catalog.previewScene);
                if (string.IsNullOrWhiteSpace(scenePath))
                {
                    throw new InvalidOperationException(
                        "Motion Preview Catalog의 프리뷰 씬이 없습니다.");
                }

                var request = new PreviewRequest();
                foreach (MotionPreviewCatalogSO.SubjectEntry entry in
                         catalog.subjects
                             .Where(entry =>
                                 entry != null
                                 && entry.id?.StartsWith(
                                     "player-", StringComparison.Ordinal) == true)
                             .OrderBy(entry => entry.id, StringComparer.Ordinal))
                {
                    string prefabPath = AssetDatabase.GetAssetPath(entry.prefab);
                    if (string.IsNullOrWhiteSpace(prefabPath))
                    {
                        throw new InvalidOperationException(
                            $"플레이어 프리뷰 경로가 없습니다: {entry.id}");
                    }

                    request.entries.Add(new PreviewEntry
                    {
                        id = entry.id,
                        prefabPath = prefabPath,
                    });
                }

                SessionState.SetBool(RequestedKey, true);
                SessionState.SetBool(SucceededKey, false);
                SessionState.SetString(
                    PreviewEntriesKey,
                    JsonUtility.ToJson(request));
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                Debug.Log(
                    $"[PlayerModelSplitPlayMode] START scene={scenePath}");
                EditorApplication.isPlaying = true;
            }
            catch (Exception exception)
            {
                Fail(exception);
            }
        }

        /// <summary>Motion Preview Catalog의 모든 프리팹에서 Missing Script를 찾는다.</summary>
        public static void ValidateCatalogPrefabsBatch()
        {
            try
            {
                MotionPreviewCatalogSO catalog = LoadCatalog();
                var failures = new List<string>();
                foreach (GameObject prefab in catalog.subjects
                             .Where(entry => entry?.prefab != null)
                             .Select(entry => entry.prefab)
                             .Distinct())
                {
                    Transform[] transforms =
                        prefab.GetComponentsInChildren<Transform>(true);
                    for (int i = 0; i < transforms.Length; i++)
                    {
                        GameObject target = transforms[i].gameObject;
                        int missing = GameObjectUtility
                            .GetMonoBehavioursWithMissingScriptCount(target);
                        if (missing > 0)
                        {
                            failures.Add(
                                $"{AssetDatabase.GetAssetPath(prefab)}/" +
                                $"{GetRelativePath(prefab.transform, transforms[i])} " +
                                $"({missing})");
                        }
                    }
                }

                if (failures.Count > 0)
                {
                    throw new InvalidOperationException(
                        "Motion Preview Catalog Missing Script:\n" +
                        string.Join("\n", failures));
                }

                Debug.Log(
                    $"[PlayerModelSplitPlayMode] CATALOG_VALIDATION_SUCCESS " +
                    $"subjects={catalog.subjects.Count}");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(RequestedKey, false))
                return;

            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                BeginValidation();
                return;
            }

            if (state != PlayModeStateChange.EnteredEditMode)
                return;

            bool succeeded = SessionState.GetBool(SucceededKey, false);
            SessionState.EraseBool(RequestedKey);
            SessionState.EraseBool(SucceededKey);
            SessionState.EraseString(PreviewEntriesKey);
            EditorApplication.Exit(succeeded ? 0 : 1);
        }

        private static void BeginValidation()
        {
            try
            {
                string requestJson =
                    SessionState.GetString(PreviewEntriesKey, string.Empty);
                PreviewRequest request = string.IsNullOrWhiteSpace(requestJson)
                    ? null
                    : JsonUtility.FromJson<PreviewRequest>(requestJson);
                s_entries = request?.entries?.ToArray()
                    ?? Array.Empty<PreviewEntry>();
                int expected = Enum.GetValues(typeof(CharacterActorType))
                    .Cast<CharacterActorType>()
                    .Count(type => type != CharacterActorType.None);
                if (s_entries.Count != expected)
                {
                    throw new InvalidOperationException(
                        $"Player Motion Preview 수가 잘못되었습니다. " +
                        $"expected={expected}, actual={s_entries.Count}");
                }

                s_entryIndex = 0;
                s_validateAt = EditorApplication.timeSinceStartup + 0.5d;
                EditorApplication.update -= ValidateNext;
                EditorApplication.update += ValidateNext;
            }
            catch (Exception exception)
            {
                Fail(exception);
            }
        }

        private static void ValidateNext()
        {
            if (EditorApplication.timeSinceStartup < s_validateAt)
                return;

            try
            {
                if (s_instance == null)
                {
                    if (s_entryIndex >= s_entries.Count)
                    {
                        Complete();
                        return;
                    }

                    SpawnCurrentPreview();
                    s_validateAt = EditorApplication.timeSinceStartup + 0.1d;
                    return;
                }

                ValidateCurrentPreview();
                ReleaseCurrentPreview();
                s_entryIndex++;
                s_validateAt = EditorApplication.timeSinceStartup + 0.05d;
            }
            catch (Exception exception)
            {
                Fail(exception);
            }
        }

        private static void SpawnCurrentPreview()
        {
            s_currentEntry = s_entries[s_entryIndex];
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                s_currentEntry.prefabPath);
            if (prefab == null)
            {
                throw new InvalidOperationException(
                    $"프리뷰 프리팹이 없습니다: " +
                    $"{s_currentEntry.id}/{s_currentEntry.prefabPath}");
            }

            s_instance = UnityEngine.Object.Instantiate(prefab);
            s_instance.name = $"Validation_{prefab.name}";
            s_subject = MotionPreviewSubjectBinderRegistry.Bind(s_instance);
            if (s_subject is not PlayerActorPreviewSubject)
            {
                throw new InvalidOperationException(
                    $"PlayerActorPreviewSubject로 바인딩되지 않았습니다: " +
                    $"{s_currentEntry.id}/{s_subject?.GetType().Name ?? "null"}");
            }

            s_subject.Refresh();
            if (s_subject is IMotionPreviewSubjectSession session)
                session.OnPreviewLoaded(spawned: true);
            if (s_subject is IMotionPreviewInputLock inputLock)
            {
                inputLock.SetInputSuppressed(true, allowCameraLook: true);
                inputLock.ClearBufferedInput();
            }
        }

        private static void ValidateCurrentPreview()
        {
            CharacterModelData[] models =
                s_instance.GetComponentsInChildren<CharacterModelData>(true);
            if (models.Length != 1 || models[0].Definition == null)
            {
                throw new InvalidOperationException(
                    $"프리뷰는 정의가 연결된 모델 하나만 포함해야 합니다: " +
                    $"{s_currentEntry.id}/{models.Length}");
            }

            CharacterActorType expectedType = models[0].Definition.characterType;
            PlayerSwapBehaviour swap =
                s_instance.GetComponentInChildren<PlayerSwapBehaviour>(true);
            if (swap == null
                || swap.GetAllCharacterTypes().Count != 1
                || swap.ActiveCharacterType != expectedType
                || swap.GetModelData(expectedType) != models[0])
            {
                throw new InvalidOperationException(
                    $"프리뷰 모델 등록 또는 초기 활성화 실패: " +
                    $"{s_currentEntry.id}/{expectedType}");
            }

            s_subject.Refresh();
            if (s_subject.Animancer == null || s_subject.Catalog == null)
            {
                throw new InvalidOperationException(
                    $"Animancer 또는 MotionSet Catalog 바인딩 실패: " +
                    $"{s_currentEntry.id}/{expectedType}");
            }

            if (s_subject is IMotionPreviewVariants variants
                && variants.Axes.Any(axis => axis?.Id == "character"))
            {
                throw new InvalidOperationException(
                    $"단일 모델 프리뷰에 불필요한 캐릭터 축이 표시됩니다: " +
                    $"{s_currentEntry.id}");
            }

            Debug.Log(
                $"[PlayerModelSplitPlayMode] PASS " +
                $"id={s_currentEntry.id}, character={expectedType}, " +
                $"motions={s_subject.Catalog.Slots.Count}");
        }

        private static void ReleaseCurrentPreview()
        {
            if (s_subject is IMotionPreviewInputLock inputLock)
            {
                inputLock.SetInputSuppressed(false, allowCameraLook: true);
                inputLock.ClearBufferedInput();
            }
            if (s_subject is IMotionPreviewSubjectSession session)
                session.OnPreviewReleased();
            if (s_instance != null)
                UnityEngine.Object.DestroyImmediate(s_instance);
            s_instance = null;
            s_subject = null;
            s_currentEntry = null;
        }

        private static void Complete()
        {
            EditorApplication.update -= ValidateNext;
            SessionState.SetBool(SucceededKey, true);
            Debug.Log(
                $"[PlayerModelSplitPlayMode] VALIDATION_SUCCESS " +
                $"previews={s_entries.Count}");
            EditorApplication.isPlaying = false;
        }

        private static void Fail(Exception exception)
        {
            EditorApplication.update -= ValidateNext;
            try
            {
                ReleaseCurrentPreview();
            }
            catch (Exception releaseException)
            {
                Debug.LogException(releaseException);
            }

            SessionState.SetBool(SucceededKey, false);
            Debug.LogException(exception);
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                EditorApplication.isPlaying = false;
            else
                EditorApplication.Exit(1);
        }

        private static MotionPreviewCatalogSO LoadCatalog() =>
            AssetDatabase.LoadAssetAtPath<MotionPreviewCatalogSO>(CatalogPath)
            ?? throw new InvalidOperationException(
                $"Motion Preview Catalog이 없습니다: {CatalogPath}");

        private static string GetRelativePath(Transform root, Transform target)
        {
            if (target == root)
                return root.name;

            var names = new Stack<string>();
            Transform current = target;
            while (current != null && current != root)
            {
                names.Push(current.name);
                current = current.parent;
            }
            names.Push(root.name);
            return string.Join("/", names);
        }
    }
}
#endif
