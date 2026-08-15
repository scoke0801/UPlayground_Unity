using UnityEngine;
using UPlayGround.Data.Actor;
using UPlayGround.Data.Item;
using UPlayGround.Data.Quest;
using UPlayGround.Components;

namespace UPlayGround.Manager
{
    /// <summary>
    /// P0 분실물 앵커 하나만 책임지는 씬 컴포넌트.
    /// 범용 월드 리셋으로 확장하지 않고 플래그·인벤토리·반복 퀘스트 상태만 대조한다.
    /// </summary>
    public sealed class CycleLoopAnchorSpawner : MonoBehaviour
    {
        private const string TargetMapId = "LakeOfLife";
        private const string AnchorNpcObjectName = "Npc_Mia";
        private const int AnchorItemId = 250101;
        private const string AnchorQuestId = "quest_cycle_anchor_lost_ribbon";
        private const string FirstRequestFlag = "cycle.anchor.first_request_started";
        private const string ResolvedOnceFlag = "cycle.anchor.lostitem_resolved_once";
        private const string FirstReturnStartedFlag = "cycle.story.first_return_started";
        private const string FirstReturnCompletedFlag = "cycle.anchor.first_return_anchor_completed";

        private static readonly Vector3 AnchorNpcWorldPosition =
            new(1079.48f, 58.519f, 421.11f);

        // 스토리 플롯 검토용 마을 주민 배치다. 보스 인물인 보쿠세이·리안리안·호노카는 제외한다.
        // 미아의 분실물 위치(-3.68, +4.39 방향)로 향하는 동선은 비워 둔다.
        private static readonly StoryReviewNpcPlacement[] StoryReviewNpcPlacements =
        {
            new("NPC_Guide",  new Vector3(6.4f, -0.55f, 3.8f)),
            new("Npc_Hazel",  new Vector3(-7f, 0f, 0.5f)),
            new("NPC_Joan",   new Vector3(-5f, 0f, -4f)),
            new("NPC_Joy",    new Vector3(-1.5f, 0f, -6f)),
            new("NPC_Morgan", new Vector3(3f, 0f, -6f)),
            new("Npc_Lucia",  new Vector3(6f, -0.3f, -3.5f)),
            new("NPC_Shop",   new Vector3(7.5f, -0.4f, 0.5f)),
            new("Npc_Penny",  new Vector3(-0.5f, 0f, 7f)),
        };

        [SerializeField] private ItemSO _lostItem;
        [SerializeField] private NpcActorSO _anchorNpcData;
        [SerializeField] private InteractableActorSO _interactionData;
        [SerializeField] private Vector3 _worldPosition = new(1075.8f, 58.2f, 425.5f);
        [SerializeField] private Color _ribbonColor = new(0.18f, 0.55f, 1f, 1f);
        [SerializeField, Min(0.1f)] private float _refreshInterval = 0.25f;
        [Header("스토리 검토")]
        [SerializeField, Tooltip("에디터와 Development 빌드에서 일반 주민을 미아 주변에 모아 대화 흐름을 검토한다.")]
        private bool _enableStoryReviewNpcCluster;

        private DropItemActor _spawnedPickup;
        private Material _runtimeMaterial;
        private float _nextRefreshAt;

        /// <summary>
        /// LakeOfLife 씬이 저장소에서 제외되어 있어 씬 직렬화에 의존하지 않고 설치한다.
        /// 기존 비활성 Mia를 재사용하며, 데이터와 분실물은 추적되는 에셋/DB에서 해석한다.
        /// </summary>
        public static void EnsureInstalled(GameObject host, string mapId)
        {
            if (host == null || mapId != TargetMapId)
                return;

            CycleLoopAnchorSpawner spawner = null;
            CycleLoopAnchorSpawner[] existing = Object.FindObjectsByType<CycleLoopAnchorSpawner>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (int i = 0; i < existing.Length; i++)
            {
                if (existing[i] != null && existing[i].gameObject.scene == host.scene)
                {
                    spawner = existing[i];
                    break;
                }
            }

            if (spawner == null)
                spawner = host.AddComponent<CycleLoopAnchorSpawner>();

            spawner.ResolveRuntimeData();
            spawner.EnsureAnchorNpcActive();
            spawner.EnsureStoryReviewNpcCluster();
        }

        private void Start()
        {
            ResolveRuntimeData();
            EnsureAnchorNpcActive();
            EnsureStoryReviewNpcCluster();
            RefreshSpawnState();
        }

        private void Update()
        {
            if (Time.unscaledTime < _nextRefreshAt)
                return;

            _nextRefreshAt = Time.unscaledTime + Mathf.Max(0.1f, _refreshInterval);
            ResolveRuntimeData();
            RefreshSpawnState();
        }

        private void OnDestroy()
        {
            if (_spawnedPickup != null)
                Destroy(_spawnedPickup.gameObject);
            if (_runtimeMaterial != null)
                Destroy(_runtimeMaterial);
        }

        private void RefreshSpawnState()
        {
            if (_spawnedPickup != null || !ShouldSpawn())
                return;

            _spawnedPickup = CreatePickup();
            if (_spawnedPickup == null)
                Debug.LogError("[CycleLoopAnchorSpawner] 분실물 오브젝트 생성에 실패했습니다.", this);
        }

        private bool ShouldSpawn()
        {
            if (_lostItem == null)
                return false;
            if (QuestManager.Instance?.GetQuestStatus(AnchorQuestId) != QuestStatus.Active)
                return false;
            if (InventoryManager.Instance == null
                || InventoryManager.Instance.GetItemCount(_lostItem.itemId) > 0)
                return false;

            bool firstRunPickup = Svc.Flags?.GetFlag(FirstRequestFlag) == true
                                  && Svc.Flags.GetFlag(ResolvedOnceFlag) == false;
            bool firstReturnPickup = Svc.Flags?.GetFlag(FirstReturnStartedFlag) == true
                                     && Svc.Flags.GetFlag(FirstReturnCompletedFlag) == false;
            return firstRunPickup || firstReturnPickup;
        }

        private void ResolveRuntimeData()
        {
            if (_lostItem == null)
                _lostItem = Svc.Item?.GetItemData(AnchorItemId);
            if (_anchorNpcData == null)
                _anchorNpcData = Resources.Load<NpcActorSO>("Story/NPC_CycleAnchor_Mia");
        }

        private void EnsureAnchorNpcActive()
        {
            if (_anchorNpcData == null)
            {
                Debug.LogError(
                    "[CycleLoopAnchorSpawner] 반복 앵커 NPC 데이터를 불러오지 못했습니다.",
                    this);
                return;
            }

            NpcActor[] actors = Object.FindObjectsByType<NpcActor>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            for (int i = 0; i < actors.Length; i++)
            {
                NpcActor actor = actors[i];
                if (actor == null
                    || actor.gameObject.scene != gameObject.scene
                    || actor.gameObject.name != AnchorNpcObjectName)
                {
                    continue;
                }

                actor.transform.position = AnchorNpcWorldPosition;
                actor.SetNpcData(_anchorNpcData);
                actor.GetComponent<NpcBrain>()?.ConfigureStationary(actor.transform.position);
                if (!actor.gameObject.activeSelf)
                    actor.gameObject.SetActive(true);
                return;
            }

            Debug.LogError(
                $"[CycleLoopAnchorSpawner] '{AnchorNpcObjectName}' NPC를 찾지 못했습니다. " +
                "반복 앵커 진행을 시작할 수 없습니다.",
                this);
        }

        private void EnsureStoryReviewNpcCluster()
        {
            if (!_enableStoryReviewNpcCluster || (!Application.isEditor && !Debug.isDebugBuild))
                return;

            NpcActor[] actors = Object.FindObjectsByType<NpcActor>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            for (int placementIndex = 0;
                 placementIndex < StoryReviewNpcPlacements.Length;
                 placementIndex++)
            {
                StoryReviewNpcPlacement placement = StoryReviewNpcPlacements[placementIndex];
                NpcActor target = null;
                for (int actorIndex = 0; actorIndex < actors.Length; actorIndex++)
                {
                    NpcActor actor = actors[actorIndex];
                    if (actor != null
                        && actor.gameObject.scene == gameObject.scene
                        && actor.gameObject.name == placement.ObjectName)
                    {
                        target = actor;
                        break;
                    }
                }

                if (target == null)
                {
                    Debug.LogWarning(
                        $"[CycleLoopAnchorSpawner] 스토리 검토용 NPC를 찾지 못했습니다: {placement.ObjectName}",
                        this);
                    continue;
                }

                Vector3 position = AnchorNpcWorldPosition + placement.Offset;
                target.transform.position = position;

                Vector3 lookDirection = AnchorNpcWorldPosition - position;
                lookDirection.y = 0f;
                if (lookDirection.sqrMagnitude > 0.01f)
                    target.transform.rotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);

                target.GetComponent<NpcBrain>()?.ConfigureStationary(position);
                if (!target.gameObject.activeSelf)
                    target.gameObject.SetActive(true);
            }
        }

        private readonly struct StoryReviewNpcPlacement
        {
            public readonly string ObjectName;
            public readonly Vector3 Offset;

            public StoryReviewNpcPlacement(string objectName, Vector3 offset)
            {
                ObjectName = objectName;
                Offset = offset;
            }
        }

        private DropItemActor CreatePickup()
        {
            var root = new GameObject("CycleAnchor_BlueRibbon");
            root.layer = 9;
            root.transform.position = _worldPosition;

            var collider = root.AddComponent<BoxCollider>();
            collider.size = new Vector3(0.9f, 0.35f, 0.9f);
            collider.center = new Vector3(0f, 0.18f, 0f);

            Material material = _runtimeMaterial != null
                ? _runtimeMaterial
                : _runtimeMaterial = CreateRibbonMaterial();
            CreateRibbonStrip(root.transform, material, new Vector3(0.7f, 0.08f, 0.16f), 28f);
            CreateRibbonStrip(root.transform, material, new Vector3(0.7f, 0.08f, 0.16f), -28f);

            DropItemActor pickup = root.AddComponent<DropItemActor>();
            pickup.Init(_lostItem, 1, _interactionData);
            return pickup;
        }

        private Material CreateRibbonMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material = new Material(shader) { color = _ribbonColor };
            material.name = "CycleAnchor_Ribbon_Runtime";
            return material;
        }

        private static void CreateRibbonStrip(
            Transform parent,
            Material material,
            Vector3 scale,
            float yaw)
        {
            GameObject strip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            strip.name = "RibbonStrip";
            strip.layer = parent.gameObject.layer;
            strip.transform.SetParent(parent, false);
            strip.transform.localPosition = new Vector3(0f, 0.2f, 0f);
            strip.transform.localRotation = Quaternion.Euler(0f, yaw, 8f);
            strip.transform.localScale = scale;
            Destroy(strip.GetComponent<Collider>());
            strip.GetComponent<Renderer>().sharedMaterial = material;
        }
    }
}
