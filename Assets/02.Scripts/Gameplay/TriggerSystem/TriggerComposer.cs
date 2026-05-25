using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.TriggerSystem
{
    [AddComponentMenu("UPlayGround/Trigger/Trigger Composer")]
    public sealed class TriggerComposer : MonoBehaviour
    {
        private static readonly HashSet<string> SessionTriggeredIds = new();

        [SerializeField] private string _triggerId;
        [SerializeField] private TriggerSourceSO _source;
        [SerializeField] private TriggerConditionSO _condition;
        [SerializeField] private TriggerActionSO _action;

        [Header("재진입 정책")]
        [SerializeField] private TriggerRepeatPolicy _repeat = TriggerRepeatPolicy.Once;
        [SerializeField] private float _cooldownSeconds = 0f;
        [SerializeField] private bool _disableColliderAfterTrigger = false;

        [Header("디버그")]
        [SerializeField] private bool _logVerbose = false;

        private bool _triggered;
        private bool _isExecuting;
        private float _lastTriggeredTime = -999f;
        private Collider _collider;

        public string TriggerId => _triggerId;
        public bool LogVerbose => _logVerbose;

        private void Awake()
        {
            _collider = GetComponent<Collider>();
            if (_collider != null)
                _collider.isTrigger = true;
        }

        private void OnEnable()
        {
            _source?.Subscribe(this, HandleSourceFired);
        }

        private void OnDisable()
        {
            _source?.Unsubscribe(this, HandleSourceFired);

            // 코루틴 실행 중 비활성화되면 Unity가 코루틴을 중단해 _isExecuting이 true로 고착,
            // Always가 아닌 재진입 정책이 영구 차단된다. 재활성화 시 다시 실행되도록 리셋한다.
            _isExecuting = false;
        }

        private void OnTriggerEnter(Collider other)
        {
            _source?.HandleTriggerEnter(this, other, HandleSourceFired);
        }

        private void OnTriggerExit(Collider other)
        {
            _source?.HandleTriggerExit(this, other, HandleSourceFired);
        }

        private void HandleSourceFired(TriggerContext context)
        {
            if (context == null || IsRepeatBlocked())
            {
                Log("반복 정책으로 발화 차단");
                return;
            }

            if (_condition != null && !_condition.Evaluate(context))
            {
                Log("조건 평가 실패");
                return;
            }

            if (_action == null || !_action.CanExecute(context))
            {
                Log("실행 가능한 액션 없음");
                return;
            }

            if (_isExecuting && _repeat != TriggerRepeatPolicy.Always)
            {
                Log("이전 액션 실행 중");
                return;
            }

            StartCoroutine(ExecuteAction(context));
        }

        private IEnumerator ExecuteAction(TriggerContext context)
        {
            _isExecuting = true;
            Log("액션 실행 시작");
            yield return _action.Execute(context);

            if (_action.ConsumesTrigger(context))
                MarkTriggered();

            _isExecuting = false;
            Log("액션 실행 종료");
        }

        private bool IsRepeatBlocked()
        {
            return _repeat switch
            {
                TriggerRepeatPolicy.Once => _triggered,
                TriggerRepeatPolicy.OncePerSession => !string.IsNullOrEmpty(_triggerId) && SessionTriggeredIds.Contains(_triggerId),
                TriggerRepeatPolicy.Cooldown => Time.time < _lastTriggeredTime + Mathf.Max(0f, _cooldownSeconds),
                _ => false,
            };
        }

        private void MarkTriggered()
        {
            _triggered = true;
            _lastTriggeredTime = Time.time;

            if (_repeat == TriggerRepeatPolicy.OncePerSession && !string.IsNullOrEmpty(_triggerId))
                SessionTriggeredIds.Add(_triggerId);

            if (_disableColliderAfterTrigger && _collider != null)
                _collider.enabled = false;
        }

        private void Log(string message)
        {
            if (_logVerbose)
                Debug.Log($"[TriggerComposer:{name}] {message}", this);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(_triggerId))
                _triggerId = gameObject.scene.IsValid() ? $"{gameObject.scene.name}/{name}" : name;

            var triggerCollider = GetComponent<Collider>();
            if (triggerCollider != null)
                triggerCollider.isTrigger = true;
        }
#endif
    }
}
