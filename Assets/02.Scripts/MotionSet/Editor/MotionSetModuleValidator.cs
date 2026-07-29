using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data.Event;
using Motion = UPlayGround.Animation.Motion;
using MotionSetData = UPlayGround.Animation.MotionSet;

namespace UPlayGround.Animation.Editor
{
    /// <summary>
    /// Test Runner 라이선스와 무관하게 MotionSet 모듈의 핵심 수직 절편을 검증하는 명령행 진입점.
    /// </summary>
    public static class MotionSetModuleValidator
    {
        private const string ResultFileName = "MotionSetModuleValidation.txt";

        [MenuItem("Tools/MotionSet/모듈 핵심 검증")]
        public static void RunFromMenu()
        {
            Run(false);
        }

        public static void RunFromCommandLine()
        {
            Run(true);
        }

        private static void Run(bool exitEditor)
        {
            string resultPath = Path.GetFullPath(
                Path.Combine(Application.dataPath, "..", "Temp", ResultFileName));
            try
            {
                ValidateResolver();
                ValidateTargetResolution();
                ValidateExecutorLifecycle();
                ValidateEditorAssemblyBoundary();
                ValidateEditorExtensions();
                File.WriteAllText(
                    resultPath,
                    "PASS: resolver,target,executor,editor-boundary,extensions");
                Debug.Log("[MotionSetModuleValidator] PASS");
                if (exitEditor)
                    EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                File.WriteAllText(resultPath, $"FAIL: {exception}");
                Debug.LogException(exception);
                if (exitEditor)
                    EditorApplication.Exit(1);
                else
                    throw;
            }
        }

        private static void ValidateResolver()
        {
            Motion first = CreateMotion("first", 1f);
            Motion second = CreateMotion("second", 2f);
            ValidationEvent motionEvent = new()
            {
                startTime = 0.25f,
                endTime = 0.75f,
            };
            second.events.Add(motionEvent);
            MotionSetData set = new()
            {
                motions = new List<Motion> { first, second },
            };

            if (!MotionTimelineResolver.TryGetEventGlobalRange(
                    set,
                    motionEvent,
                    out float start,
                    out float end) ||
                Mathf.Abs(start - 1.25f) > 0.001f ||
                Mathf.Abs(end - 1.75f) > 0.001f)
                throw new InvalidOperationException("Resolver 누적 시간 검증 실패");
        }

        private static void ValidateTargetResolution()
        {
            GameObject parent = new("MotionSetValidationProvider");
            GameObject child = new("MotionSetValidationExecutor");
            GameObject explicitTarget = new("MotionSetValidationExplicit");
            try
            {
                child.transform.SetParent(parent.transform);
                ValidationTargetProvider provider =
                    parent.AddComponent<ValidationTargetProvider>();
                provider.Target = parent;
                MotionEventExecutor executor = child.AddComponent<MotionEventExecutor>();

                if (executor.TargetObject != parent)
                    throw new InvalidOperationException("부모 provider 대상 해석 실패");
                executor.SetTargetObject(explicitTarget);
                if (executor.TargetObject != explicitTarget)
                    throw new InvalidOperationException("명시적 대상 우선순위 실패");
                executor.SetTargetObject(null);
                if (executor.TargetObject != parent)
                    throw new InvalidOperationException("provider 재해석 실패");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(child);
                UnityEngine.Object.DestroyImmediate(parent);
                UnityEngine.Object.DestroyImmediate(explicitTarget);
            }
        }

        private static void ValidateExecutorLifecycle()
        {
            GameObject target = new("MotionSetValidationLifecycle");
            try
            {
                MotionEventExecutor executor = target.AddComponent<MotionEventExecutor>();
                ValidationEvent motionEvent = new()
                {
                    startTime = 0f,
                    endTime = 1f,
                    Signal = "Window",
                };
                Motion motion = CreateMotion("base", 2f);
                motion.events.Add(motionEvent);
                MotionSetData set = new()
                {
                    motions = new List<Motion> { motion },
                };
                int signalCount = 0;
                executor.SignalChanged += (_, _) => signalCount++;

                executor.PlayMotionSet(set);
                executor.UpdateTime(0f);
                executor.UpdateTime(0.5f);
                executor.ExitActiveEvents();

                if (motionEvent.EnterCount != 1 ||
                    motionEvent.TickCount != 1 ||
                    motionEvent.ExitCount != 1 ||
                    signalCount != 2)
                    throw new InvalidOperationException("Executor 생명주기 검증 실패");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        private static Motion CreateMotion(string id, float duration)
        {
            AnimationClip clip = new();
            clip.SetCurve(
                string.Empty,
                typeof(Transform),
                "m_LocalPosition.x",
                AnimationCurve.Linear(0f, 0f, duration, 1f));
            return new Motion
            {
                id = id,
                motionName = id,
                motionClip = clip,
            };
        }

        private static void ValidateEditorAssemblyBoundary()
        {
            Assembly assembly = typeof(MotionSetEditorWindow).Assembly;
            string[] forbidden =
            {
                "UPlayGround.Data",
                "UPlayGround.Actor",
                "UPlayGround.Contracts",
                "KinematicCharacterController",
            };
            string[] references = assembly.GetReferencedAssemblies()
                .Select(reference => reference.Name)
                .ToArray();
            string invalid = references.FirstOrDefault(reference =>
                forbidden.Contains(reference, StringComparer.Ordinal) ||
                reference.StartsWith("Assembly-CSharp", StringComparison.Ordinal));
            if (invalid != null)
            {
                throw new InvalidOperationException(
                    $"MotionSet.Editor 금지 어셈블리 참조: {invalid}");
            }

            Type[] editorWindowTypes = TypeCache.GetTypesDerivedFrom<EditorWindow>()
                .Where(type => type.Name == nameof(MotionSetEditorWindow))
                .ToArray();
            if (editorWindowTypes.Length != 1 ||
                editorWindowTypes[0].Assembly != assembly)
            {
                string summary = string.Join(
                    ", ",
                    editorWindowTypes.Select(type =>
                        $"{type.FullName}@{type.Assembly.GetName().Name}"));
                throw new InvalidOperationException(
                    $"MotionSetEditorWindow 어셈블리 분산: {summary}");
            }
        }

        private static void ValidateEditorExtensions()
        {
            ValidateConstructible<IMotionEditorPanel>();
            ValidateConstructible<IMotionEventSceneEditor>();
            ValidateConstructible<IMotionEventOffsetFieldProvider>();
            ValidateConstructible<IMotionPreviewSubjectBinder>();
            ValidateConstructible<IMotionPreviewCatalogPopulator>();
        }

        private static void ValidateConstructible<T>()
        {
            foreach (Type type in TypeCache.GetTypesDerivedFrom<T>())
            {
                if (type.IsAbstract || type.IsInterface)
                    continue;
                ConstructorInfo constructor = type.GetConstructor(Type.EmptyTypes);
                if (!type.IsPublic || constructor == null || !constructor.IsPublic)
                {
                    throw new InvalidOperationException(
                        $"{typeof(T).Name} 구현은 public 무인자 생성자가 필요합니다: " +
                        type.FullName);
                }

                try
                {
                    Activator.CreateInstance(type);
                }
                catch (Exception exception)
                {
                    throw new InvalidOperationException(
                        $"{typeof(T).Name} 구현 생성 실패: {type.FullName}",
                        exception);
                }
            }
        }

        [Serializable]
        private sealed class ValidationEvent :
            MotionEventBase,
            IMotionEventTick,
            IMotionEventSignal
        {
            public int EnterCount;
            public int TickCount;
            public int ExitCount;
            public string Signal;

            public string SignalId => Signal;
            public override string GetDisplayName() => "Validation";
            public override void Execute(GameObject target) => EnterCount++;
            public void Tick(GameObject target, float normalizedTime, float deltaTime) => TickCount++;
            public override void OnCompleteEvent(GameObject target) => ExitCount++;
        }
    }

    internal sealed class ValidationTargetProvider :
        MonoBehaviour,
        IMotionEventTargetProvider
    {
        public GameObject Target;
        public GameObject MotionEventTarget => Target;
    }
}
