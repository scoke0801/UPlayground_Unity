using System.Collections.Generic;
using UnityEngine;

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

        public BufferedInput(string name, float time, float bufferTime, object data = null)
        {
            InputName = name;
            Timestamp = time;
            BufferTime = bufferTime;
            Data = data;
        }

        public bool IsExpired()
        {
            return Time.time - Timestamp > BufferTime;
        }

        public float RemainingTime => Mathf.Max(0f, BufferTime - (Time.time - Timestamp));
    }

    /// <summary>
    /// 입력 버퍼 시스템
    /// 짧은 시간 동안 입력을 저장하여 프레임 단위 손실 방지
    /// </summary>
    public class InputBuffer
    {
        private Queue<BufferedInput> _buffer = new Queue<BufferedInput>();
        private float _bufferTime;
        private int _maxBufferSize;

        // 만료 일시정지: 공격의 액티브 히트(캔슬 불가) 구간처럼 입력을 즉시 처리할 수 없는 동안
        // 선입력이 만료돼 유실되는 것을 막는다. 재개 시 정지 길이만큼 타임스탬프를 밀어
        // "캔슬창이 열리는 순간 가득 찬 버퍼 창"을 보장한다(버퍼 시간을 늘리지 않아 과버퍼 부작용 없음).
        private bool  _expiryPaused;
        private float _pauseStartTime;

        public InputBuffer(float bufferTime = 0.15f, int maxSize = 10)
        {
            _bufferTime = bufferTime;
            _maxBufferSize = maxSize;
        }

        /// <summary>
        /// 입력 추가.
        /// <paramref name="timestamp"/>를 지정하면 만료 기준 시각을 직접 준다.
        /// 조합키 중재(grace) 때문에 디스패치가 늦어져도 버퍼 유효 시간이 줄지 않도록
        /// 호출자가 원래 물리 입력 시각을 넘기는 용도다(스펙 §9.3).
        /// </summary>
        public void AddInput(
            string inputName,
            object data = null,
            float? bufferTime = null,
            float? timestamp = null)
        {
            // 버퍼 크기 제한
            while (_buffer.Count >= _maxBufferSize)
            {
                _buffer.Dequeue();
            }

            float duration = Mathf.Max(0f, bufferTime ?? _bufferTime);
            // 미래 타임스탬프는 만료를 늘려버리므로 현재 시각으로 잘라낸다.
            float time = Mathf.Min(timestamp ?? Time.time, Time.time);
            _buffer.Enqueue(new BufferedInput(inputName, time, duration, data));
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
        /// 특정 입력 소비 (가져오고 제거)
        /// </summary>
        public BufferedInput ConsumeInput(string inputName)
        {
            CleanExpiredInputs();

            Queue<BufferedInput> tempQueue = new Queue<BufferedInput>();
            BufferedInput result = null;

            while (_buffer.Count > 0)
            {
                var input = _buffer.Dequeue();

                if (result == null && input.InputName == inputName)
                {
                    result = input;
                }
                else
                {
                    tempQueue.Enqueue(input);
                }
            }

            // 나머지를 다시 버퍼에 넣음
            while (tempQueue.Count > 0)
            {
                _buffer.Enqueue(tempQueue.Dequeue());
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
                if (latest == null || input.Timestamp > latest.Timestamp)
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
        /// 만료 타이머 일시정지/재개. 캔슬 불가 구간(콜리전 활성)에서 true로 두면 그 동안 선입력이
        /// 만료되지 않고, false로 재개할 때 정지된 시간만큼 기존 입력의 타임스탬프를 밀어
        /// 만료 시점을 보존한다. 멱등(상태 변화 시에만 동작)이라 매 프레임 호출해도 안전하다.
        /// </summary>
        public void SetExpiryPaused(bool paused)
        {
            if (paused == _expiryPaused) return;

            if (paused)
            {
                // 정지 직전에 이미 만료된 입력은 먼저 버린다. 그대로 두면 재개 시 타임스탬프가 밀려
                // 오래된 입력이 되살아날 수 있다(아직 _expiryPaused=false라 청소가 실제로 동작한다).
                CleanExpiredInputs();
                _expiryPaused = true;
                _pauseStartTime = Time.time;
            }
            else
            {
                _expiryPaused = false;
                float frozen = Time.time - _pauseStartTime;
                float now = Time.time;
                if (frozen > 0f)
                {
                    // 각 입력이 만료 정지 상태에서 보낸 시간만큼만 보정한다.
                    // 정지 전 입력은 전체 정지 시간을, 정지 중 입력은 입력 시점부터 재개까지를 제외한다.
                    foreach (var input in _buffer)
                    {
                        float frozenForInput = now - Mathf.Max(input.Timestamp, _pauseStartTime);
                        if (frozenForInput > 0f)
                            input.Timestamp += frozenForInput;
                    }
                }
            }
        }

        /// <summary>
        /// 만료된 입력 제거
        /// </summary>
        private void CleanExpiredInputs()
        {
            // 만료 정지 중에는 어떤 입력도 버리지 않는다(캔슬창이 열릴 때까지 선입력 보존).
            if (_expiryPaused) return;

            Queue<BufferedInput> tempQueue = new Queue<BufferedInput>();

            while (_buffer.Count > 0)
            {
                var input = _buffer.Dequeue();

                if (!input.IsExpired())
                {
                    tempQueue.Enqueue(input);
                }
            }

            _buffer = tempQueue;
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
