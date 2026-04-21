using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Component;
using UPlayGround.MovementController;

namespace UPlayGround.BehaviorTree
{
    public class RuntimeBlackboard
    {
        // ── 컴포넌트 참조 ─────────────────────────────────────────────
        public BTRunner                Runner    { get; set; }
        public EnemyDetection          Detection { get; set; }
        public EnemyCombat             Combat    { get; set; }
        public EnemyTacticalMemory     Memory    { get; set; }
        public ActorMovementController  Movement  { get; set; }

        // ── 키-값 저장소 ───────────────────────────────────────────────
        private readonly Dictionary<string, bool>   _bools   = new();
        private readonly Dictionary<string, float>  _floats  = new();
        private readonly Dictionary<string, int>    _ints    = new();
        private readonly Dictionary<string, string> _strings = new();

        // ── Get API ────────────────────────────────────────────────────
        public bool   GetBool  (string key, bool   def = false) => _bools.TryGetValue(key,   out var v) ? v : def;
        public float  GetFloat (string key, float  def = 0f)    => _floats.TryGetValue(key,  out var v) ? v : def;
        public int    GetInt   (string key, int    def = 0)     => _ints.TryGetValue(key,    out var v) ? v : def;
        public string GetString(string key, string def = "")    => _strings.TryGetValue(key, out var v) ? v : def;

        // ── Set API ────────────────────────────────────────────────────
        public void Set(string key, bool   value) => _bools[key]   = value;
        public void Set(string key, float  value) => _floats[key]  = value;
        public void Set(string key, int    value) => _ints[key]    = value;
        public void Set(string key, string value) => _strings[key] = value;

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
