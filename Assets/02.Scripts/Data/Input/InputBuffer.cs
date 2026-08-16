using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.InputDefine;

namespace UPlayGround.Input
{
    /// <summary>
    /// 입력 버퍼 데이터
    /// </summary>
    public class BufferedInput
    {
        public string InputName;
        public float Timestamp;
        public float BufferTime;
        public object Data;
        // Time 값이 같은 프레임에서도 입력의 실제 적재 순서를 판별하는 단조 증가 순번.
        public long Sequence;

        public BufferedInput(string name, float time, float bufferTime, object data = null, long sequence = 0)
        {
            InputName = name;
            Timestamp = time;
            BufferTime = bufferTime;
            Data = data;
            Sequence = sequence;
        }

        /// <summary>
        /// 만료 여부. 판정 기준은 scaled time(<see cref="Time.time"/>)이므로
        /// 히트스톱처럼 timeScale = 0인 구간에서는 경과 시간이 늘지 않아 만료가 진행되지 않는다.
        /// 즉 정지 중에 선입력이 조용히 사라지지 않는다(별도의 만료 일시정지 기능이 필요 없는 이유).
        /// </summary>
        public bool IsExpired()
        {
            return Time.time - Timestamp > BufferTime;
        }

        public float RemainingTime => Mathf.Max(0f, BufferTime - (Time.time - Timestamp));
    }

    /// <summary>
    /// 입력 버퍼 시스템
    /// 짧은 시간 동안 입력을 저장하여 프레임 단위 손실 방지
    ///
    /// 시간 기준: 모든 만료 판정은 scaled time(<see cref="Time.time"/>)을 사용한다.
    /// timeScale = 0(히트스톱 등) 구간에서는 만료가 멈추므로 버퍼 창이 연출 시간만큼 잠식되지 않는다.
    /// 반대로 슬로모션 구간에서는 실시간 기준보다 버퍼가 길게 유지된다.
    /// </summary>
    public class InputBuffer
    {
        private Queue<BufferedInput> _buffer = new Queue<BufferedInput>();
        private float _bufferTime;
        private int _maxBufferSize;
        private long _nextSequence;

        public InputBuffer(float bufferTime = 0.15f, int maxSize = 10)
        {
            if (maxSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxSize), "입력 버퍼 크기는 1 이상이어야 합니다.");

            _bufferTime = Mathf.Max(0f, bufferTime);
            _maxBufferSize = maxSize;
        }

        /// <summary>
        /// 액션별 "버퍼에 1개만 유지(단일 슬롯)" 정책의 단일 소스.
        ///
        /// "다음 행동 의도"를 보관하는 액션은 같은 이름을 여러 번 큐잉하지 않는다.
        /// 연타 횟수만큼 미래 행동을 예약하면 입력을 멈춘 뒤에도 공격/회피가 반복되기 때문이다.
        /// 판정을 호출부가 아니라 여기 한 곳에 두어, 새 호출부가 생겨도 규칙이 조용히 깨지지 않게 한다.
        ///
        /// 배치 근거: 이 판정은 Manager뿐 아니라 Actor 모듈(PlayerActor 입력 콜백)에서
        /// 직접 호출하는 <see cref="AddInput"/> 경로에도 적용돼야 한다. Data 모듈은 Manager를
        /// 참조할 수 없으므로, 모든 호출부가 반드시 지나가는 InputBuffer(Data)에 둔다.
        /// 액션 이름 상수(<see cref="PlayerAction"/>)도 같은 Data 모듈에 있어 경계 위반이 없다.
        /// </summary>
        public static bool IsSingleSlotAction(string inputName)
        {
            switch (inputName)
            {
                case PlayerAction.Attack:
                case PlayerAction.HeavyAttack:
                case PlayerAction.Dodge:
                case PlayerAction.Jump:
                case PlayerAction.Dash:
                case PlayerAction.SkillAbility:
                case PlayerAction.SkillUltimate:
                case PlayerAction.ElementBuff:
                case PlayerAction.Interact:
                case PlayerAction.CharacterSwap_1:
                case PlayerAction.CharacterSwap_2:
                case PlayerAction.CharacterSwap_3:
                case PlayerAction.CharacterSwap_4:
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>
        /// 입력 추가.
        /// <paramref name="timestamp"/>를 지정하면 만료 기준 시각을 직접 준다.
        /// 조합키 중재(grace) 때문에 디스패치가 늦어져도 버퍼 유효 시간이 줄지 않도록
        /// 호출자가 원래 물리 입력 시각을 넘기는 용도다(스펙 §9.3).
        /// <paramref name="replaceExisting"/>은 정책과 무관하게 교체를 강제하는 옵션이며,
        /// <see cref="IsSingleSlotAction"/>이 true인 액션은 이 값과 상관없이 항상 교체된다.
        /// </summary>
        public void AddInput(
            string inputName,
            object data = null,
            float? bufferTime = null,
            float? timestamp = null,
            bool replaceExisting = false)
        {
            // 단일 슬롯 액션은 최신 입력 하나로 갱신해 다음 소비 기회 1회만 보장한다.
            if (replaceExisting || IsSingleSlotAction(inputName))
                RemoveInputs(inputName);

            // 버퍼 크기 제한
            while (_buffer.Count >= _maxBufferSize)
            {
                _buffer.Dequeue();
            }

            float duration = Mathf.Max(0f, bufferTime ?? _bufferTime);
            // 미래 타임스탬프는 만료를 늘려버리므로 현재 시각으로 잘라낸다.
            float time = Mathf.Min(timestamp ?? Time.time, Time.time);
            _buffer.Enqueue(new BufferedInput(inputName, time, duration, data, ++_nextSequence));
        }

        private void RemoveInputs(string inputName)
        {
            if (_buffer.Count == 0) return;

            // 단일 슬롯 정책 때문에 모든 PlayerAction 입력마다 호출된다.
            // 매칭이 없으면 큐를 건드리지 않고 바로 빠져나가 입력당 GC 할당을 없앤다.
            bool hasMatch = false;
            foreach (var input in _buffer)
            {
                if (input.InputName == inputName)
                {
                    hasMatch = true;
                    break;
                }
            }

            if (!hasMatch) return;

            // 큐를 한 바퀴 회전시키며 걸러낸다. 남는 항목의 상대 순서는 그대로 유지되고
            // 임시 큐를 새로 만들지 않는다.
            int count = _buffer.Count;
            for (int i = 0; i < count; i++)
            {
                BufferedInput input = _buffer.Dequeue();
                if (input.InputName != inputName)
                    _buffer.Enqueue(input);
            }
        }

        /// <summary>
        /// 특정 입력이 버퍼에 있는지 확인
        /// </summary>
        public bool HasInput(string inputName)
        {
            CleanExpiredInputs();

            foreach (var input in _buffer)
            {
                if (input.InputName == inputName)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 특정 입력을 소비하지 않고 조회한다. ConsumeInput과 동일한 대상(가장 오래된 매칭)을 돌려주므로
        /// 두 입력의 Sequence를 비교해 "어느 쪽이 실제로 먼저/나중에 적재됐는지" 판정하는 데 쓸 수 있다.
        /// </summary>
        public BufferedInput PeekInput(string inputName)
        {
            CleanExpiredInputs();

            foreach (var input in _buffer)
            {
                if (input.InputName == inputName)
                    return input;
            }

            return null;
        }

        /// <summary>
        /// 특정 입력 소비 (가져오고 제거)
        /// </summary>
        public BufferedInput ConsumeInput(string inputName)
        {
            CleanExpiredInputs();

            BufferedInput result = null;

            // 임시 큐 대신 제자리 회전으로 처리한다. 가장 오래된 매칭 1개만 빼고
            // 나머지는 원래 순서 그대로 다시 들어간다.
            int count = _buffer.Count;
            for (int i = 0; i < count; i++)
            {
                var input = _buffer.Dequeue();

                if (result == null && input.InputName == inputName)
                {
                    result = input;
                    continue;
                }

                _buffer.Enqueue(input);
            }

            return result;
        }

        /// <summary>
        /// 가장 최근 입력 가져오기
        /// </summary>
        public BufferedInput GetLatestInput()
        {
            CleanExpiredInputs();

            BufferedInput latest = null;

            foreach (var input in _buffer)
            {
                if (latest == null || input.Sequence > latest.Sequence)
                    latest = input;
            }

            return latest;
        }

        /// <summary>
        /// 디버그/모니터링용 스냅샷. 외부에서 큐를 직접 건드리지 않도록 복사본만 반환한다.
        /// </summary>
        public List<BufferedInput> GetSnapshot()
        {
            CleanExpiredInputs();
            return new List<BufferedInput>(_buffer);
        }

        /// <summary>
        /// 버퍼 비우기
        /// </summary>
        public void Clear()
        {
            _buffer.Clear();
        }

        /// <summary>
        /// 만료된 입력 제거.
        /// 만료 판정은 scaled time 기준이라 timeScale = 0 구간에서는 아무것도 제거되지 않는다.
        /// </summary>
        private void CleanExpiredInputs()
        {
            if (_buffer.Count == 0) return;

            // 거의 매 조회마다 호출되므로 만료 항목이 없으면 큐를 건드리지 않는다.
            bool hasExpired = false;
            foreach (var input in _buffer)
            {
                if (input.IsExpired())
                {
                    hasExpired = true;
                    break;
                }
            }

            if (!hasExpired) return;

            // 제자리 회전으로 만료 항목만 버린다. 남는 항목의 순서는 그대로다.
            int count = _buffer.Count;
            for (int i = 0; i < count; i++)
            {
                var input = _buffer.Dequeue();

                if (!input.IsExpired())
                {
                    _buffer.Enqueue(input);
                }
            }
        }

        /// <summary>
        /// 버퍼 크기
        /// </summary>
        public int Count
        {
            get
            {
                CleanExpiredInputs();
                return _buffer.Count;
            }
        }

        /// <summary>
        /// 디버그 정보
        /// </summary>
        public void DebugPrint()
        {
            CleanExpiredInputs();

            Debug.Log($"[InputBuffer] Count: {_buffer.Count}");
            foreach (var input in _buffer)
            {
                Debug.Log($"  - {input.InputName} ({Time.time - input.Timestamp:F3}s ago / {input.BufferTime:F3}s)");
            }
        }
    }
}
