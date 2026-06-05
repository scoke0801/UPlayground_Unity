using System.Collections.Generic;
using UnityEngine;

namespace Game.Input
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

        public InputBuffer(float bufferTime = 0.15f, int maxSize = 10)
        {
            _bufferTime = bufferTime;
            _maxBufferSize = maxSize;
        }

        /// <summary>
        /// 입력 추가
        /// </summary>
        public void AddInput(string inputName, object data = null, float? bufferTime = null)
        {
            // 버퍼 크기 제한
            while (_buffer.Count >= _maxBufferSize)
            {
                _buffer.Dequeue();
            }

            float duration = Mathf.Max(0f, bufferTime ?? _bufferTime);
            _buffer.Enqueue(new BufferedInput(inputName, Time.time, duration, data));
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
        /// 버퍼 비우기
        /// </summary>
        public void Clear()
        {
            _buffer.Clear();
        }

        /// <summary>
        /// 만료된 입력 제거
        /// </summary>
        private void CleanExpiredInputs()
        {
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
