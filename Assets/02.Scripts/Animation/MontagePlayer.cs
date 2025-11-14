using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Animancer;

namespace Animation
{
    /// <summary>
    /// 애니메이션 몽타쥬를 재생하는 플레이어 컴포넌트
    /// Animancer를 사용하여 언리얼 스타일의 몽타쥬 시스템 구현
    /// Avatar Mask와 레이어를 사용하여 상체/하체 분리 애니메이션 지원
    /// </summary>
    [RequireComponent(typeof(AnimancerComponent))]
    public class MontagePlayer : MonoBehaviour
    {
        [Header("슬롯 설정")]
        [Tooltip("사용할 애니메이션 슬롯들을 등록합니다")]
        [SerializeField] private List<AnimationSlot> registeredSlots = new List<AnimationSlot>();
        
        private AnimancerComponent _animancer;
        private AnimationMontage _currentMontage;
        private MontageSection _currentSection;
        private AnimancerState _currentState;
        private Dictionary<string, bool> _triggeredNotifies = new Dictionary<string, bool>();
        private Dictionary<string, AnimationSlot> _slotCache = new Dictionary<string, AnimationSlot>();
        private Dictionary<int, AnimancerLayer> _layerCache = new Dictionary<int, AnimancerLayer>();
        
        // 이벤트
        public event Action<AnimationMontage> OnMontageStarted;
        public event Action<AnimationMontage> OnMontageEnded;
        public event Action<MontageSection> OnSectionChanged;
        public event Action<string> OnNotifyTriggered;
        
        private void Awake()
        {
            _animancer = GetComponent<AnimancerComponent>();
            InitializeSlots();
        }
        
        /// <summary>
        /// 슬롯 초기화 및 Animancer 레이어 설정
        /// </summary>
        private void InitializeSlots()
        {
            _slotCache.Clear();
            _layerCache.Clear();
            
            foreach (var slot in registeredSlots)
            {
                if (slot == null) continue;
                
                // 슬롯 캐시
                _slotCache[slot.SlotName] = slot;
                
                // Animancer 레이어 설정
                if (slot.LayerIndex > 0) // 레이어 0은 기본 레이어
                {
                    var layer = _animancer.Layers[slot.LayerIndex];
                    layer.Weight = slot.LayerWeight;
                    
                    // Additive 모드 설정
                    layer.IsAdditive = slot.IsAdditive;
                    
                    // Avatar Mask 적용
                    if (slot.AvatarMask != null)
                    {
                        layer.Mask = slot.AvatarMask;
                    }
                    
                    _layerCache[slot.LayerIndex] = layer;
                    
                    Debug.Log($"[MontagePlayer] Slot '{slot.SlotName}' initialized - Layer: {slot.LayerIndex}, Weight: {slot.LayerWeight}, Additive: {slot.IsAdditive}");
                }
            }
        }
        
        /// <summary>
        /// 슬롯 이름으로 AnimationSlot 가져오기
        /// </summary>
        private AnimationSlot GetSlot(string slotName)
        {
            if (_slotCache.TryGetValue(slotName, out var slot))
            {
                return slot;
            }
            
            Debug.LogWarning($"[MontagePlayer] Slot '{slotName}' not found! Using default layer.");
            return null;
        }
        
        /// <summary>
        /// 몽타쥬 재생
        /// </summary>
        public void PlayMontage(AnimationMontage montage, string startSectionName = null)
        {
            if (montage == null)
            {
                Debug.LogWarning("Montage is null!");
                return;
            }
            
            // 슬롯 그룹 체크 - 같은 그룹의 다른 몽타쥬 중단
            MontageSlotManager.Instance.RegisterMontagePlayback(this, montage.SlotName);
            
            StopCurrentMontage();
            
            _currentMontage = montage;
            
            // 시작 섹션 결정
            MontageSection startSection = string.IsNullOrEmpty(startSectionName)
                ? montage.GetFirstSection()
                : montage.GetSection(startSectionName);
            
            if (startSection == null)
            {
                Debug.LogWarning($"Section '{startSectionName}' not found in montage '{montage.MontageName}'");
                return;
            }
            
            PlaySection(startSection);
            OnMontageStarted?.Invoke(montage);
        }
        
        /// <summary>
        /// 특정 섹션으로 점프
        /// </summary>
        public void JumpToSection(string sectionName)
        {
            if (_currentMontage == null)
            {
                Debug.LogWarning("No montage is currently playing!");
                return;
            }
            
            MontageSection section = _currentMontage.GetSection(sectionName);
            if (section == null)
            {
                Debug.LogWarning($"Section '{sectionName}' not found!");
                return;
            }
            
            PlaySection(section);
        }
        
        /// <summary>
        /// 다음 섹션 설정
        /// </summary>
        public void SetNextSection(string currentSectionName, string nextSectionName)
        {
            // 런타임에서 다음 섹션 동적 변경
            Debug.Log($"Setting next section: {currentSectionName} -> {nextSectionName}");
        }
        
        /// <summary>
        /// 몽타쥬 정지
        /// </summary>
        public void StopMontage(float fadeDuration = 0.25f)
        {
            StopCurrentMontage(fadeDuration);
        }
        
        /// <summary>
        /// 재생 속도 변경
        /// </summary>
        public void SetPlayRate(float playRate)
        {
            if (_currentState != null)
            {
                _currentState.Speed = playRate;
            }
        }
        
        /// <summary>
        /// 슬롯 가중치 변경 (런타임)
        /// </summary>
        public void SetSlotWeight(string slotName, float weight)
        {
            var slot = GetSlot(slotName);
            if (slot != null && slot.LayerIndex > 0)
            {
                _animancer.Layers[slot.LayerIndex].Weight = Mathf.Clamp01(weight);
            }
        }
        
        /// <summary>
        /// 현재 재생 중인 몽타쥬 정보
        /// </summary>
        public bool IsPlaying => _currentMontage != null && _currentState != null && _currentState.IsPlaying;
        public AnimationMontage CurrentMontage => _currentMontage;
        public MontageSection CurrentSection => _currentSection;
        
        private void PlaySection(MontageSection section)
        {
            if (section?.Clip == null)
            {
                Debug.LogWarning("Section or clip is null!");
                return;
            }
            
            _currentSection = section;
            _triggeredNotifies.Clear();
            
            // 슬롯 정보 가져오기
            var slot = GetSlot(_currentMontage.SlotName);
            int layerIndex = slot?.LayerIndex ?? 0;
            
            // Animancer로 애니메이션 재생 (지정된 레이어에서)
            _currentState = _animancer.Layers[layerIndex].Play(section.Clip, section.FadeInDuration);
            _currentState.Speed = section.PlayRate;
            
            // 이벤트 설정 - Animancer v8 API 사용
            if (_currentState.Events(this, out var events))
            {
                // 루프 설정
                if (section.Loop)
                {
                    events.OnEnd = null; // 루프 시 OnEnd 제거
                }
                else
                {
                    events.OnEnd = () => OnSectionEnd(section);
                }
            }
            else
            {
                // 이벤트가 이미 설정된 경우
                var eventSequence = _currentState.Events(this);
                if (section.Loop)
                {
                    eventSequence.OnEnd = null;
                }
                else
                {
                    eventSequence.OnEnd = () => OnSectionEnd(section);
                }
            }
            
            // 노티파이 처리를 위한 코루틴 시작
            StartCoroutine(ProcessNotifies(section));
            
            OnSectionChanged?.Invoke(section);
            
            Debug.Log($"[MontagePlayer] Playing section '{section.SectionName}' on layer {layerIndex}");
        }
        
        private void OnSectionEnd(MontageSection section)
        {
            // 다음 섹션이 지정되어 있으면 재생
            if (!string.IsNullOrEmpty(section.NextSection))
            {
                JumpToSection(section.NextSection);
            }
            else
            {
                // 다음 섹션이 없으면 순차적으로 다음 섹션 재생
                int currentIndex = GetSectionIndex(section);
                if (currentIndex >= 0 && currentIndex < _currentMontage.Sections.Count - 1)
                {
                    PlaySection(_currentMontage.Sections[currentIndex + 1]);
                }
                else
                {
                    // 몽타쥬 종료
                    var montage = _currentMontage;
                    StopCurrentMontage();
                    OnMontageEnded?.Invoke(montage);
                }
            }
        }
        
        private int GetSectionIndex(MontageSection section)
        {
            for (int i = 0; i < _currentMontage.Sections.Count; i++)
            {
                if (_currentMontage.Sections[i] == section)
                {
                    return i;
                }
            }
            return -1;
        }
        
        private IEnumerator ProcessNotifies(MontageSection section)
        {
            if (section.Notifies == null || section.Notifies.Count == 0)
                yield break;
            
            while (_currentState != null && _currentState.IsPlaying && _currentSection == section)
            {
                float normalizedTime = _currentState.NormalizedTime % 1f;
                
                foreach (var notify in section.Notifies)
                {
                    string notifyKey = $"{section.SectionName}_{notify.NotifyName}_{notify.TriggerTime}";
                    
                    // 이미 트리거된 노티파이는 스킵
                    if (_triggeredNotifies.ContainsKey(notifyKey))
                        continue;
                    
                    // 노티파이 시간에 도달했으면 트리거
                    if (normalizedTime >= notify.TriggerTime)
                    {
                        _triggeredNotifies[notifyKey] = true;
                        OnNotifyTriggered?.Invoke(notify.NotifyName);
                        Debug.Log($"Montage Notify Triggered: {notify.NotifyName} at {notify.TriggerTime}");
                    }
                }
                
                yield return null;
            }
        }
        
        private void StopCurrentMontage(float fadeDuration = 0.25f)
        {
            if (_currentState != null)
            {
                _currentState.Stop();
                _currentState = null;
            }
            
            if (_currentMontage != null)
            {
                MontageSlotManager.Instance.UnregisterMontagePlayback(_currentMontage.SlotName);
            }
            
            _currentMontage = null;
            _currentSection = null;
            _triggeredNotifies.Clear();
            StopAllCoroutines();
        }
        
        private void OnValidate()
        {
            // 에디터에서 슬롯 설정이 변경되면 재초기화
            if (Application.isPlaying && _animancer != null)
            {
                InitializeSlots();
            }
        }
    }
}
