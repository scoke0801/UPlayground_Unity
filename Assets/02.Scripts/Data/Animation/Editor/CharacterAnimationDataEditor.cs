using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.FSM
{
    [CustomEditor(typeof(CharacterAnimationData))]
    public class CharacterAnimationDataEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            CharacterAnimationData data = (CharacterAnimationData)target;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("데이터 관리 도구", EditorStyles.boldLabel);

            if (GUILayout.Button("1. AnimKey 기반 리스트 동기화"))
            {
                SyncWithEnum(data);
            }

            if (GUILayout.Button("2. 애니메이션 자동 매칭 (유연한 검색)"))
            {
                AutoMatchClips(data);
            }
        }

        private void SyncWithEnum(CharacterAnimationData data)
        {
            Undo.RecordObject(data, "Sync Enum");

            var field = typeof(CharacterAnimationData).GetField("clipAnimations", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var targetList = (List<CharacterAnimationData.ClipEntry>)field.GetValue(data);

            var existingKeys = targetList.Select(e => e.key).ToHashSet();
            
            foreach (AnimKey key in Enum.GetValues(typeof(AnimKey)))
            {
                if (key == AnimKey.None || key.ToString().Contains("Mixer")) continue;

                if (!existingKeys.Contains(key))
                {
                    targetList.Add(new CharacterAnimationData.ClipEntry { key = key });
                }
            }

            targetList.Sort((a, b) => a.key.CompareTo(b.key));
            EditorUtility.SetDirty(data);
            Debug.Log("리스트 동기화 완료.");
        }

        private void AutoMatchClips(CharacterAnimationData data)
        {
            // 폴더 경로 확인
            string folderPath = AssetDatabase.GetAssetPath(data.animationFolder);
            if (string.IsNullOrEmpty(folderPath))
            {
                Debug.LogError("Source Folder가 비어있습니다. 폴더를 할당해주세요.");
                return;
            }

            Undo.RecordObject(data, "Auto Match Clips");

            var field = typeof(CharacterAnimationData).GetField("clipAnimations", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var targetList = (List<CharacterAnimationData.ClipEntry>)field.GetValue(data);

            // 해당 폴더 및 하위 폴더의 모든 클립 로드
            string[] guids = AssetDatabase.FindAssets("t:AnimationClip", new[] { folderPath });
            var allClips = guids.Select(g => AssetDatabase.LoadAssetAtPath<AnimationClip>(AssetDatabase.GUIDToAssetPath(g))).ToList();

            if (allClips.Count == 0)
            {
                Debug.LogWarning($"[{folderPath}] 내에 검색된 애니메이션 클립이 없습니다.");
                return;
            }

            int matchCount = 0;
            foreach (var entry in targetList)
            {
                // 이미 할당된 것은 유지하려면 아래 주석 해제
                // if (entry.transition.Clip != null) continue;

                string enumName = entry.key.ToString().ToLower();
                
                // 검색 알고리즘 개선:
                // 1. Enum 이름에서 언더바(_) 제거 (예: stand_turn_l45 -> standturnl45)
                // 2. 파일 이름에서 언더바 제거 및 소문자화
                // 3. InPlace가 붙은 파일을 최우선순위로
                var matchedClip = allClips
                    .Where(clip => {
                        string fileName = clip.name.ToLower().Replace("_", "");
                        string searchKey = enumName.Replace("_", "");
                        
                        // 예외 처리: Walk_Turn_L45(Enum) -> Walk_F_Turn_L45(File) 대응을 위해 
                        // Enum의 핵심 키워드들이 파일명에 포함되어 있는지 확인
                        return fileName.Contains(searchKey) || IsFlexibleMatch(fileName, enumName);
                    })
                    .OrderByDescending(clip => clip.name.Contains("InPlace") == false)
                    .FirstOrDefault();

                if (matchedClip != null)
                {
                    entry.transition.Clip = matchedClip;
                    matchCount++;
                }
            }

            EditorUtility.SetDirty(data);
            Debug.Log($"{matchCount}개의 클립 매칭 완료. (검색 대상 폴더: {folderPath})");
        }

        // 간단한 키워드 포함 검사 (예: Walk_Turn_L180 이 Walk_F_Turn_L180_InPlace 에 매칭되도록)
        private bool IsFlexibleMatch(string fileName, string enumName)
        {
            string[] parts = enumName.Split('_');
            // Enum의 모든 단어가 파일명에 포함되어 있는지 확인 (F, B 같은 방향 지시자 제외하고도 매칭되게 함)
            return parts.All(p => fileName.Contains(p.ToLower()));
        }
    }
}