using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.Data.Cycle
{
    // 파일명과 클래스명이 일치해야 MonoScript가 에셋에 연결된다 (BossAssistDefinitionSO.cs에서 분리).
    [CreateAssetMenu(fileName = "BossAssistDatabase", menuName = "UPlayGround/사이클/보스 어시스트 DB")]
    public sealed class BossAssistDatabaseSO : ScriptableObject
    {
        public List<BossAssistDefinitionSO> definitions = new();

        public BossAssistDefinitionSO FindByAssistId(string id) => definitions?.Find(value => value != null && value.assistId == id);
        public BossAssistDefinitionSO FindByBossActorId(string id) => definitions?.Find(value => value != null && value.sourceBossActorId == id);
    }
}
