using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Component;
using UPlayGround.MovementController;

namespace UPlayGround.BehaviorTree
{
    public class RuntimeBlackboard
    {
        // ── 컴포넌트 참조 ─────────────────────────────────────────────
        public BTRunner                Runner        { get; set; }
        public BTRunnerFlying          FlyingRunner  { get; set; }
        public EnemyDetection          Detection     { get; set; }
        public EnemyCombat             Combat        { get; set; }
        public EnemyTacticalMemory     Memory        { get; set; }
        public ActorMovementController  Movement      { get; set; }

        // ── 키-값 저장소 ───────────────────────────────────────────────
        private readonly Dictionary<string, bool>   _bools   = new();
        private readonly Dictionary<string, float>  _floats  = new();
        private readonly Dictionary<string, int>    _ints    = new();
        private readonly Dictionary<string, string> _strings = new();

        // ── 변화 알림 이벤트 (Phase 3) ─────────────────────────────────
        public event Action<string> OnBoolChanged;
        public event Action<string> OnFloatChanged;
        public event Action<string> OnIntChanged;
        public event Action<string> OnStringChanged;

        // ── Get API ────────────────────────────────────────────────────
        public bool   GetBool  (string key, bool   def = false) => _bools.TryGetValue(key,   out var v) ? v : def;
        public float  GetFloat (string key, float  def = 0f)    => _floats.TryGetValue(key,  out var v) ? v : def;
        public int    GetInt   (string key, int    def = 0)     => _ints.TryGetValue(key,    out var v) ? v : def;
        public string GetString(string key, string def = "")    => _strings.TryGetValue(key, out var v) ? v : def;

        // ── Set API — 값이 실제로 변경될 때만 이벤트 발생 ──────────────
        public void Set(string key, bool value)
        {
            bool changed = !_bools.TryGetValue(key, out var old) || old != value;
            _bools[key] = value;
            if (changed) OnBoolChanged?.Invoke(key);
        }

        public void Set(string key, float value)
        {
            bool changed = !_floats.TryGetValue(key, out var old) || !Mathf.Approximately(old, value);
            _floats[key] = value;
            if (changed) OnFloatChanged?.Invoke(key);
        }

        public void Set(string key, int value)
        {
            bool changed = !_ints.TryGetValue(key, out var old) || old != value;
            _ints[key] = value;
            if (changed) OnIntChanged?.Invoke(key);
        }

        public void Set(string key, string value)
        {
            bool changed = !_strings.TryGetValue(key, out var old) || old != value;
            _strings[key] = value;
            if (changed) OnStringChanged?.Invoke(key);
        }

        // ── 파생 프로퍼티 ──────────────────────────────────────────────
        public bool IsActionReady
            => Time.time - GetFloat(BBKey.LastActionTime, -999f)
               >= GetFloat(BBKey.NextActionDelay, 0.5f);

        // ── 에디터 접근 (BTBlackboardView용) ──────────────────────────
        public IReadOnlyDictionary<string, bool>   Bools   => _bools;
        public IReadOnlyDictionary<string, float>  Floats  => _floats;
        public IReadOnlyDictionary<string, int>    Ints    => _ints;
        public IReadOnlyDictionary<string, string> Strings => _strings;
    }
}
