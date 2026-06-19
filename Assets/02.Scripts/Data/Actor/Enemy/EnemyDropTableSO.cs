using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.Data.Enemy
{
    /// <summary>
    /// 적 아이템 드랍 테이블. 여러 몬스터 종류에서 공유 가능.
    /// </summary>
    [CreateAssetMenu(fileName = "DropTable_", menuName = "UPlayGround/적/Drop Table")]
    public class EnemyDropTableSO : ScriptableObject
    {
        [Tooltip("드랍 아이템 목록. 각 항목의 rate(0~100)로 독립적으로 확률 계산.")]
        public List<ItemDropList> dropItems = new List<ItemDropList>();
    }
}
