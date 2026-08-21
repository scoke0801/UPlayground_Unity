using UnityEngine;
using UPlayGround.Data.Actor;
using UPlayGround.UI;

namespace UPlayGround.Gameplay.Quest
{
    /// <summary>
    /// NPC 데이터가 지정한 퀘스트 마커 지점을 씬의 NPC에 설치한다.
    ///
    /// NPC는 <see cref="UPlayGround.Actor"/> 어셈블리에 있어 UI의 마커 레지스트리를 직접 참조할 수 없고,
    /// 지역 씬 파일은 저장소에 없어 마커를 씬에 저장해 둘 수도 없다.
    /// 그래서 두 어셈블리를 모두 볼 수 있는 이 통합 계층이 데이터 값을 씬에 연결한다.
    /// </summary>
    public static class NpcQuestMarkerInstaller
    {
        /// <summary>
        /// 지정 씬의 모든 NPC를 훑어 데이터에 적힌 마커 지점을 설치한다.
        /// 아직 꺼져 있는 NPC도 포함해, 나중에 켜지는 스토리 NPC가 마커를 놓치지 않게 한다.
        /// </summary>
        public static int InstallAll(UnityEngine.SceneManagement.Scene scene)
        {
            NpcActor[] npcs = Object.FindObjectsByType<NpcActor>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            int installed = 0;
            for (int i = 0; i < npcs.Length; i++)
            {
                NpcActor npc = npcs[i];
                if (npc == null || npc.gameObject.scene != scene)
                    continue;

                if (npc.GetData() is not NpcActorSO data
                    || string.IsNullOrWhiteSpace(data.questMarkerLocationId))
                    continue;

                // QuestTarget으로 등록해야 상시 NPC 아이콘이 아니라 목표가 살아 있는 동안만 마커가 뜬다.
                if (MinimapMarkerRegistrar.Install(
                        npc.gameObject, data.questMarkerLocationId, MinimapMarkerType.QuestTarget) != null)
                    installed++;
            }

            return installed;
        }
    }
}
