using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UPlayGround.Animation;
using UPlayGround.Data.Event;
using UPlayGround.Data.Projectile;

namespace UPlayGround.EditorTools
{
    /// <summary>무기 복귀 섬광과 실제 착탄 지점 예고를 전용 데이터로 연결한다.</summary>
    public static class WeaponSignatureReturnFxAuthoring
    {
        private const string Root = "Assets/03.Prefabs/Effects/Signature/";
        public static string Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorUtility.scriptCompilationFailed)
                throw new InvalidOperationException("정상 편집 모드에서 저작합니다.");
            string flashPath = Root + "WeaponReturn_Flash_Signature.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(flashPath) == null)
            {
                var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ExternalAssets/FX/Cartoon FX Remaster/CFXR Prefabs/Misc/CFXR Flash.prefab");
                if (source == null) throw new InvalidOperationException("복귀 섬광 원본 누락");
                var copy = UnityEngine.Object.Instantiate(source);
                try
                {
                    copy.name = "WeaponReturn_Flash_Signature";
                    copy.transform.localScale *= .4f;
                    foreach (var script in copy.GetComponentsInChildren<MonoBehaviour>(true)) UnityEngine.Object.DestroyImmediate(script);
                    foreach (var system in copy.GetComponentsInChildren<ParticleSystem>(true))
                    {
                        system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                        var main = system.main;
                        main.loop = false; main.playOnAwake = true; main.stopAction = ParticleSystemStopAction.None;
                    }
                    PrefabUtility.SaveAsPrefabAsset(copy, flashPath);
                }
                finally { UnityEngine.Object.DestroyImmediate(copy); }
            }
            string telegraphPath = Root + "SwordShield_Telegraph_Signature.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(telegraphPath) == null)
            {
                var copy = PrefabUtility.LoadPrefabContents(Root + "Bow_Telegraph_Signature.prefab");
                try
                {
                    copy.name = "SwordShield_Telegraph_Signature";
                    var line = copy.GetComponent<LineRenderer>();
                    line.startColor = line.endColor = new Color(1f, .7f, .22f, .7f);
                    line.startWidth = line.endWidth = .08f;
                    for (int i = 0; i < line.positionCount; i++)
                    {
                        float angle = i * Mathf.PI * 2 / line.positionCount;
                        line.SetPosition(i, new Vector3(Mathf.Cos(angle) * 4.5f, .04f, Mathf.Sin(angle) * 4.5f));
                    }
                    PrefabUtility.SaveAsPrefabAsset(copy, telegraphPath);
                }
                finally { PrefabUtility.UnloadPrefabContents(copy); }
            }
            var shot = AssetDatabase.LoadAssetAtPath<ProjectileDefinitionSO>("Assets/10.Datas/Projectile/Signature/Projectile_SwordShieldSignatureFall.asset");
            var flash = AssetDatabase.LoadAssetAtPath<GameObject>(flashPath);
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            var motions = new MotionSetAsset[2];
            try
            {
                Undo.RegisterCompleteObjectUndo(shot, "방패 착탄 예고 연결");
                shot.landingTelegraphPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(telegraphPath);
                for (int i = 0; i < 2; i++)
                {
                    string weapon = i == 0 ? "Spear" : "SwordShield";
                    string socket = i == 0 ? "Hand.R" : "Hand_L";
                    string character = i == 0 ? "Reine" : "MyoRyeong";
                    var model = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03.Prefabs/Actor/Player/Models/PlayerModel_" + character + ".prefab");
                    if (!model.GetComponentsInChildren<Transform>(true).Any(t => t.name == socket))
                        throw new InvalidOperationException("복귀 섬광 소켓 누락: " + socket);
                    var motion = motions[i] = AssetDatabase.LoadAssetAtPath<MotionSetAsset>("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/Signature/" + weapon + "_Signature.asset");
                    Undo.RegisterCompleteObjectUndo(motion, "무기 복귀 섬광 연결");
                    if (!motion.motionSet.globalEvents.OfType<BeginParticleEvent>().Any(e => e.particlePrefab == flash))
                    {
                        float time = (i == 0 ? 50f : 55f) / 30f;
                        motion.motionSet.globalEvents.Add(new BeginParticleEvent
                        {
                            particlePrefab = flash, spawnPointName = socket, startTime = time, endTime = time + .15f,
                            attachToTarget = true, destroyOnFinish = true
                        });
                    }
                    EditorUtility.SetDirty(motion);
                }
                EditorUtility.SetDirty(shot);
                AssetDatabase.SaveAssetIfDirty(shot);
                foreach (var motion in motions) AssetDatabase.SaveAssetIfDirty(motion);
                Undo.CollapseUndoOperations(group);
            }
            catch
            {
                Undo.RevertAllDownToGroup(group);
                AssetDatabase.SaveAssetIfDirty(shot);
                foreach (var motion in motions) if (motion != null) AssetDatabase.SaveAssetIfDirty(motion);
                throw;
            }
            string report = "창 F50·방패 F55 복귀 손 섬광, 방패 실제 착탄 위치 반경4.5m 예고 연결. 원본 CFXR Flash·기존 Bow 예고는 보존.";
            File.WriteAllText("docs/work-status/weapon-signature/RETURN_FX_RECEIPT.txt", report);
            return report;
        }
    }
}
