using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UPlayGround.Animation;
using Motion = UPlayGround.Animation.Motion;

namespace UPlayGround.Editor.P09Builder
{
    /// <summary>기존 공격의 손가락 포즈를 스태프 전용 마스크 레이어로 연결한다.</summary>
    public static class P09StaffGripAuthoring
    {
        public const string Channel = "weapon.staff.rightHandGrip";
        public const string ToolId = "UPlayGround/캐릭터/P09/스태프 손 포즈 연결";
        private const string Folder = "Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/Staff";
        private const string MaskPath = Folder + "/StaffRightFingers.mask";

        /// <summary>오른손 손가락에만 기존 쥐기 포즈를 적용하고 기존 레이어·이벤트를 보존한다.</summary>
        [UPlayGround.EditorTools.UPlaygroundTool(ToolId, priority = 1103)]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                throw new InvalidOperationException("컴파일이 끝난 편집 모드에서 실행하세요.");
            var assets = AssetDatabase.FindAssets("t:MotionSetAsset", new[] { Folder })
                .Select(guid => AssetDatabase.LoadAssetAtPath<MotionSetAsset>(AssetDatabase.GUIDToAssetPath(guid))).ToArray();
            var source = AssetDatabase.LoadAssetAtPath<MotionSetAsset>(P09StaffAttachment.MotionRoot + "Attack_1.asset");
            var clip = source?.motionSet.motions.FirstOrDefault()?.motionClip;
            if (clip == null || assets.Any(asset => SerializationUtility.HasManagedReferencesWithMissingTypes(asset)))
                throw new InvalidOperationException("원본 쥐기 클립 또는 MotionEvent 타입을 확인하세요.");

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("스태프 손가락 포즈 연결");
            bool createdMask = false;
            try
            {
                var mask = AssetDatabase.LoadAssetAtPath<AvatarMask>(MaskPath);
                if (mask == null)
                {
                    if (File.Exists(MaskPath)) throw new InvalidOperationException("손가락 마스크 경로의 에셋 타입이 다릅니다.");
                    mask = new AvatarMask();
                    for (int i = 0; i < (int)AvatarMaskBodyPart.LastBodyPart; i++)
                        mask.SetHumanoidBodyPartActive((AvatarMaskBodyPart)i, i == (int)AvatarMaskBodyPart.RightFingers);
                    AssetDatabase.CreateAsset(mask, MaskPath);
                    createdMask = true;
                }
                foreach (var asset in assets)
                {
                    var existing = asset.motionSet.layers?.FirstOrDefault(layer => layer.channelId == Channel);
                    if (existing != null)
                    {
                        if (existing.weightCurve == null || existing.weightCurve.length == 0)
                        {
                            Undo.RecordObject(asset, "스태프 손 포즈 가중치");
                            existing.weightCurve = AnimationCurve.Constant(0f, 1f, 1f);
                            EditorUtility.SetDirty(asset);
                        }
                        continue;
                    }
                    Undo.RecordObject(asset, "스태프 오른손 포즈 레이어");
                    asset.motionSet.layers ??= new List<MotionLayer>();
                    int index = Math.Max(1, asset.motionSet.layers.Select(layer => layer.animancerLayerIndex + 1).DefaultIfEmpty(1).Max());
                    asset.motionSet.layers.Add(new MotionLayer
                    {
                        layerName = "스태프 오른손 쥐기",
                        channelId = Channel,
                        concurrencyGroupId = Channel,
                        animancerLayerIndex = index,
                        avatarMask = mask,
                        holdLastFrame = true,
                        weightCurve = AnimationCurve.Constant(0f, 1f, 1f),
                        motions = new List<Motion>
                        {
                            new Motion
                            {
                                id = "staff_right_hand_grip",
                                motionName = "기존 공격의 쥐기 자세",
                                motionClip = clip,
                                clipStartTime = 0f,
                                clipEndTime = 1f / clip.frameRate,
                                playbackSpeed = 1f,
                            }
                        }
                    });
                    EditorUtility.SetDirty(asset);
                }
                AssetDatabase.SaveAssets();
                Undo.CollapseUndoOperations(group);
                Debug.Log($"스태프 모션 {assets.Length}개의 오른손 포즈 연결 완료");
            }
            catch
            {
                Undo.RevertAllDownToGroup(group);
                if (createdMask) AssetDatabase.DeleteAsset(MaskPath);
                AssetDatabase.SaveAssets();
                throw;
            }
        }
    }
}
