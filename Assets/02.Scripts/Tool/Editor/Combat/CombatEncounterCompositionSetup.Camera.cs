using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data;
using UPlayGround.Data.Combat;

namespace UPlayGround.Tool.Editor.Combat
{
    public static partial class CombatEncounterCompositionSetup
    {
        /// <summary>실사용 카메라 한 개의 승인된 필드만 변경하고 원본과 보존 검증을 남긴다.</summary>
        [UPlayGround.EditorTools.UPlaygroundTool("UPlayGround/게임플레이/전투/정책/전투 구도 프리셋 적용")]
        public static void ApplyCompositionProfiles()
        {
            const string path = "Assets/10.Datas/Camera/CameraSettings.asset";
            const string output = "output/combat-encounter-composition";
            var settings = AssetDatabase.LoadAssetAtPath<CameraSettings>(path);
            if (settings == null) throw new InvalidOperationException("실사용 카메라 설정을 찾을 수 없습니다.");
            Directory.CreateDirectory(output);
            string backup = output + "/camera-settings-before.asset.txt";
            if (!File.Exists(backup)) File.Copy(path, backup);
            var original = UnityEngine.Object.Instantiate(settings);
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.RecordObject(settings, "교전 카메라 초기값");
            try
            {
                ApplyProfile(settings.explore, new Vector2(5f, 5f), 14f, 55f, 1.3f, 6f);
                ApplyProfile(settings.sweep, new Vector2(7f, 9f), 24f, 60f, 1.2f, 3f);
                ApplyProfile(settings.duel, new Vector2(5.5f, 5.5f), 16f, 56f, 1.35f, 6f);
                settings.enableLockOnFixedDistance = true;
                settings.lockOnDistance = 5.8f;
                settings.lockOnPreferredPitch = 22f;
                var before = new SerializedObject(original);
                var after = new SerializedObject(settings);
                var property = before.GetIterator();
                while (property.Next(true))
                {
                    string field = property.propertyPath;
                    if (field == "m_Name" || field.StartsWith("explore") || field.StartsWith("sweep") || field.StartsWith("duel")
                        || field == "enableLockOnFixedDistance" || field == "lockOnDistance" || field == "lockOnPreferredPitch") continue;
                    var comparison = after.FindProperty(field);
                    if (comparison == null || !SerializedProperty.DataEquals(property, comparison))
                        throw new InvalidOperationException("허용되지 않은 필드 변경: " + field);
                }
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssetIfDirty(settings);
                LoadOrCreate<CombatEncounterProfileSO>(ResourceFolder + "/" + CombatEncounterProfileSO.ResourceKey + ".asset");
                File.WriteAllText(output + "/camera-field-preservation.txt", path + "\n프리셋·락온 3필드 외 기존 설정 보존 검사 통과\n원본: " + backup);
                Undo.CollapseUndoOperations(group);
            }
            catch
            {
                Undo.RevertAllDownToGroup(group);
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssetIfDirty(settings);
                throw;
            }
            finally { UnityEngine.Object.DestroyImmediate(original); }
        }

        private static void ApplyProfile(EncounterCameraProfile profile, Vector2 distance, float pitch, float fov, float height, float hitAngle)
        {
            profile.distanceRange = distance;
            profile.pitch = pitch;
            profile.fieldOfView = fov;
            profile.pivotOffset = new Vector3(0f, height, 0f);
            profile.maxDistance = 9.5f;
            profile.hitAssistMaxAngle = hitAngle;
            profile.transitionTime = 1f;
        }
    }
}
