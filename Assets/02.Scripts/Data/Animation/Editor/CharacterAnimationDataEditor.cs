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
            EditorGUILayout.LabelField("자동화 도구", EditorStyles.boldLabel);

            if (GUILayout.Button("AnimKey 기반 리스트 동기화"))
            {
                SyncWithEnum(data);
            }

            if (GUILayout.Button("애니메이션 클립 자동 매칭 (InPlace 우선)"))
            {
                AutoMatchClips(data);
            }
        }

        private void SyncWithEnum(CharacterAnimationData data)
        {
            Undo.RecordObject(data, "Sync Enum");

            // 리플렉션으로 private 필드인 clipAnimations에 접근
            var field = typeof(CharacterAnimationData).GetField("clipAnimations", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var list = (List<CharacterAnimationData.ClipEntry>)field.GetValue(data);

            var existingKeys = list.Select(e => e.key).ToHashSet();
            
            foreach (AnimKey key in Enum.GetValues(typeof(AnimKey)))
            {
                if (key == AnimKey.None || key == AnimKey.Mixer_Locomotion) continue;

                if (!existingKeys.Contains(key))
                {
                    list.Add(new CharacterAnimationData.ClipEntry { key = key });
                }
            }

            // Enum 순서대로 정렬
            list.Sort((a, b) => a.key.CompareTo(b.key));
            
            EditorUtility.SetDirty(data);
            Debug.Log("리스트 동기화 완료.");
        }

        private void AutoMatchClips(CharacterAnimationData data)
        {
            Undo.RecordObject(data, "Auto Match Clips");
            
            // 1. 폴더 경로 가져오기
            string folderPath = AssetDatabase.GetAssetPath(data.animationFolder);
    
            if (string.IsNullOrEmpty(folderPath))
            {
                Debug.LogError("검색할 폴더가 지정되지 않았습니다!");
                return;
            }
            var field = typeof(CharacterAnimationData).GetField("clipAnimations", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var targetList = (List<CharacterAnimationData.ClipEntry>)field.GetValue(data);
            
            // 2. 해당 폴더 내에서만 클립 검색
            string[] searchPath = { folderPath };
            string[] guids = AssetDatabase.FindAssets("t:AnimationClip", searchPath);
    
            var allClips = guids
                .Select(g => AssetDatabase.LoadAssetAtPath<AnimationClip>(AssetDatabase.GUIDToAssetPath(g)))
                .ToList();
            
            int matchCount = 0;
            foreach (var entry in targetList)
            {
                if (entry.transition.Clip != null) continue; // 이미 있는 경우 패스

                string keyStr = entry.key.ToString().ToLower();
                
                // 매칭 규칙: 
                // 1. Enum 이름 포함 여부 확인
                // 2. InPlace가 붙은 파일을 우선 순위로 탐색
                var matchedClip = allClips
                    .Where(c => c.name.ToLower().Contains(keyStr.Replace("_", ""))) 
                    .OrderByDescending(c => c.name.Contains("InPlace"))
                    .FirstOrDefault();

                if (matchedClip != null)
                {
                    entry.transition.Clip = matchedClip;
                    matchCount++;
                }
            }

            EditorUtility.SetDirty(data);
            Debug.Log($"{matchCount}개의 클립 자동 매칭 완료.");
        }
    }
}