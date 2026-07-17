using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.Manager
{
    /// <summary>
    /// 매 프레임 갱신이 필요한 에이전트 구성요소가 구현한다.
    /// 개별 MonoBehaviour.Update 대신 <see cref="AgentTickManager"/>가 일괄 호출한다.
    /// </summary>
    public interface IManagedTick
    {
        void ManagedTick(float deltaTime);
    }

    /// <summary>
    /// 적 BT 러너/탐지 등 다수 MonoBehaviour의 개별 Update를 단일 루프로 통합한다.
    /// Unity의 네이티브↔매니지드 Update 디스패치는 호출 개수 자체가 비용이므로,
    /// N개의 Update를 1개의 매니저 루프 + N개의 매니지드 메서드 호출로 치환해 오버헤드를 낮춘다.
    /// 각 구성요소는 자체 타이머/인터벌을 유지하므로 동작 의미는 기존과 동일하다.
    /// </summary>
    public class AgentTickManager : BaseManager<AgentTickManager>, IManager, IUpdatableManager
    {
        private readonly List<IManagedTick> _ticks = new();
        private bool _needsCompact;

        // ── IManager 생명주기 (GameManager가 순차 구동) ──
        public void Init() { }
        public void AfterInit() { }
        public void OnFixedUpdate() { }
        public void OnLateUpdate() { }

        public void Dispose()
        {
            _ticks.Clear();
            _needsCompact = false;
        }

        public void OnSceneChanged(string sceneType)
        {
            // 이전 씬 에이전트는 OnDisable에서 Unregister되지만, 파괴 순서가 보장되지 않으므로
            // 씬 전환 시 죽은(파괴된) 엔트리를 한 번 정리해 둔다.
            _ticks.RemoveAll(IsDead);
            _needsCompact = false;
        }

        public void Register(IManagedTick tick)
        {
            if (tick == null)
                return;
            if (!_ticks.Contains(tick))
                _ticks.Add(tick);
        }

        public void Unregister(IManagedTick tick)
        {
            int idx = _ticks.IndexOf(tick);
            if (idx >= 0)
            {
                // 순회 도중 호출돼도 안전하도록 null 마킹 후 프레임 말미에 일괄 제거
                _ticks[idx] = null;
                _needsCompact = true;
            }
        }

        public void OnUpdate()
        {
            float dt = Time.deltaTime;

            // for-인덱스 순회: ManagedTick 내부에서 Register/Unregister가 일어나도 안전
            for (int i = 0; i < _ticks.Count; i++)
            {
                var tick = _ticks[i];
                if (tick == null)
                    continue;
                tick.ManagedTick(dt);
            }

            if (_needsCompact)
            {
                _ticks.RemoveAll(t => t == null);
                _needsCompact = false;
            }
        }

        // IManagedTick은 인터페이스라 destroy된 MonoBehaviour를 참조해도 일반 null 비교로는 걸러지지 않는다.
        // UnityEngine.Object로 캐스팅해 파괴 여부(가짜 null)를 판별한다.
        private static bool IsDead(IManagedTick tick)
            => tick == null || (tick is global::UnityEngine.Object obj && obj == null);
    }
}
