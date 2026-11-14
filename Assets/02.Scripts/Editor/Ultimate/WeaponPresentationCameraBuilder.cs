#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Data.Editor
{
    /// <summary>기존 Snapshot 카메라에 5비트 앵글을 저작한다. 모션·판정·Stage는 변경하지 않는다.</summary>
    public static class WeaponPresentationCameraBuilder
    {
        /// <summary>기존 파일과 수작업 후보를 보존하며 무기 프로필의 첫 카메라 초안을 만든다.</summary>
        public static CameraSnapshotProfile Create(WeaponPresentationProfileSO profile, float motionDuration)
        {
            if (profile == null || motionDuration <= 0f || float.IsNaN(motionDuration) || float.IsInfinity(motionDuration))
                throw new ArgumentException("프로필과 확정 모션 길이가 필요합니다.");
            if (!profile.IsValid(out string validationError))
                throw new ArgumentException(validationError);
            string folder = WeaponPresentationAssetBuilder.Root + "/CameraSnapshots";
            Directory.CreateDirectory(folder);
            string path = $"{folder}/CameraSnapshot_Ultimate_{profile.weaponType}.asset";
            CameraSnapshotProfile camera = AssetDatabase.LoadAssetAtPath<CameraSnapshotProfile>(path);
            if (camera == null)
            {
                if (File.Exists(path) || !string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(path)))
                    throw new InvalidOperationException($"기존 파일을 덮어쓸 수 없습니다: {path}");
                camera = ScriptableObject.CreateInstance<CameraSnapshotProfile>();
                camera.sequenceName = $"Ultimate.{profile.weaponType}";
                camera.useUnscaledTime = false;
                camera.useCollision = true;
                camera.restorePreviousModeOnFinish = true;
                camera.releaseLockOnOnEnter = false;
                camera.actorAnchor = CameraSnapshotActorReference.ActivePlayer(ActorSocketType.None);
                camera.lookAtTarget = CameraSnapshotActorReference.ActivePlayer(ActorSocketType.Center);
                Vector3 entry = ResolveEntry(profile.weaponType);
                bool orbit = profile.weaponType == WeaponType.DualBlade
                    || profile.weaponType == WeaponType.DoubleAxe || profile.weaponType == WeaponType.Whip;
                for (int i = 0; i < profile.beats.Count; i++)
                {
                    float end = i + 1 < profile.beats.Count ? profile.beats[i + 1].normalizedTime : 1f;
                    Vector3 position = entry;
                    if (i == 2) position = new Vector3(-entry.x, entry.y + 0.3f, entry.z);
                    if (i == 3) position = new Vector3(entry.x * 0.4f, 1.1f, entry.z * 0.85f);
                    if (i == 4) position = new Vector3(0f, 1.4f, -4f);
                    camera.shots.Add(new CameraSnapshotShot
                    {
                        shotName = profile.beats[i].beat.ToString(),
                        space = CameraSnapshotSpace.ActorRelative,
                        position = position,
                        rotationEuler = Quaternion.LookRotation(new Vector3(0f, 1f, 0f) - position).eulerAngles,
                        fieldOfView = profile.weaponType == WeaponType.Bow ? 42f : 50f,
                        duration = Mathf.Max(0.01f, (end - profile.beats[i].normalizedTime) * motionDuration),
                        moveType = orbit ? CameraSnapshotMoveType.OrbitAroundAnchor : CameraSnapshotMoveType.Linear
                    });
                }
                try
                {
                    AssetDatabase.CreateAsset(camera, path);
                    EditorUtility.SetDirty(camera);
                    AssetDatabase.SaveAssetIfDirty(camera);
                }
                catch
                {
                    if (AssetDatabase.LoadMainAssetAtPath(path) == camera)
                        AssetDatabase.DeleteAsset(path);
                    else if (!AssetDatabase.Contains(camera))
                        UnityEngine.Object.DestroyImmediate(camera);
                    throw;
                }
            }
            if (profile.cameraPresets.Count == 0)
            {
                profile.cameraPresets.Add(camera);
                EditorUtility.SetDirty(profile);
                AssetDatabase.SaveAssetIfDirty(profile);
            }
            return camera;
        }

        private static Vector3 ResolveEntry(WeaponType weapon) => weapon switch
        {
            WeaponType.Sword => new Vector3(-3f, 1.4f, -3f),
            WeaponType.SwordShield => new Vector3(2f, 0.7f, -2.8f),
            WeaponType.GreatSword => new Vector3(2.5f, 0.5f, -3.6f),
            WeaponType.Katana => new Vector3(-1.2f, 1.3f, -5f),
            WeaponType.DualBlade => new Vector3(3f, 1.5f, -2f),
            WeaponType.DoubleAxe => new Vector3(2f, 4f, -3f),
            WeaponType.Spear => new Vector3(1.2f, 1.5f, -4.2f),
            WeaponType.Bow => new Vector3(0.8f, 1.6f, -2.2f),
            WeaponType.Staff => new Vector3(1.5f, 3.4f, -3.4f),
            WeaponType.Whip => new Vector3(3.2f, 1.8f, -3f),
            _ => throw new ArgumentException("궁극기 주무기 타입이 아닙니다.")
        };
    }
}
#endif
