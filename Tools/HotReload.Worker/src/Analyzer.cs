using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Mono.Cecil;
using UPlayGround.HotReload.Protocol;

namespace UPlayGround.HotReload.Worker
{
    /// <summary>
    /// W0(현재 원본 소스의 Worker 컴파일)를 보관하고, 새 스냅샷(Sr)을 W0·A0와 비교해 본문 교체 가능 여부를 판정한다.
    /// 구조/연결 가능성은 A0 기준, 본문 변경은 W0 기준으로 계산하며 Eg와의 차이는 Editor 계획기가 계산한다.
    /// </summary>
    public sealed class Analyzer
    {
        sealed class AssemblyState
        {
            public AssemblyInput Input;
            public UnityCompileContext Ctx;
            public Dictionary<string, string> FileHashes;
            public ShapeModel W0;
            public bool Allowed;
            public HashSet<string> TargetKeys = new(StringComparer.Ordinal);
            public Dictionary<string, (string code, string why)> Ineligible = new(StringComparer.Ordinal);
            public AssemblyBaselineReport Report;
        }

        readonly List<AssemblyState> _assemblies = new();
        string _projectRoot;
        bool _requireNoInlining = true;
        public bool HasBaseline => _assemblies.Count > 0;

        public WorkerResponse PrepareBaseline(WorkerRequest req, CancellationToken ct)
        {
            _assemblies.Clear();
            _projectRoot = req.ProjectRoot;
            _requireNoInlining = req.RequireNoInlining;
            var resp = new WorkerResponse { ResultCode = ResultCodes.Ok };
            var snapshot = SnapshotIndex(req.Snapshot);
            var allowAsm = new HashSet<string>(req.AllowedAssemblies ?? new List<string>(), StringComparer.Ordinal);
            var allowMethods = new HashSet<string>(req.AllowedMethods ?? new List<string>(), StringComparer.Ordinal);
            var contextParts = new List<string>();

            foreach (var input in req.Assemblies)
            {
                var sw = Stopwatch.StartNew();
                var st = new AssemblyState { Input = input, Allowed = allowAsm.Contains(input.Name) };
                st.Ctx = UnityCompileContext.Create(input, req.ProjectRoot, req.TrustedAnalyzerRoot);
                st.FileHashes = HashesFor(st.Ctx, snapshot);
                var compiled = st.Ctx.Compile(p => ReadBlob(snapshot, p), ct);
                if (!compiled.Success)
                {
                    resp.ResultCode = ResultCodes.BaselineMissing;
                    resp.Diagnostics.AddRange(compiled.Errors);
                    return resp;
                }
                using var w0 = ModuleDefinition.ReadModule(new MemoryStream(compiled.Pe));
                st.W0 = ShapeModel.Build(w0);

                var report = new AssemblyBaselineReport { Name = input.Name, ContextHash = st.Ctx.ContextHash, W0Mvid = w0.Mvid.ToString(), LoadedMvid = input.LoadedMvid, Generators = st.Ctx.GeneratorNames };
                if (!string.IsNullOrEmpty(input.PreIlppDllPath) && File.Exists(Abs(input.PreIlppDllPath)))
                {
                    using var pre = ModuleDefinition.ReadModule(Abs(input.PreIlppDllPath));
                    report.PreIlppMvid = pre.Mvid.ToString();
                    report.W0MatchesUnityCompile = pre.Mvid == w0.Mvid;
                }

                using var a0 = ModuleDefinition.ReadModule(new MemoryStream(File.ReadAllBytes(Abs(input.LoadedDllPath))));
                if (!string.Equals(a0.Mvid.ToString(), input.LoadedMvid, StringComparison.OrdinalIgnoreCase))
                    throw new UnsupportedException(ResultCodes.ContextChanged, $"{input.Name}: 디스크 DLL MVID({a0.Mvid})와 로드된 모듈 MVID({input.LoadedMvid})가 다릅니다.");
                var a0Shape = ShapeModel.Build(a0);
                foreach (var key in st.W0.Surface.Keys.Union(a0Shape.Surface.Keys))
                {
                    st.W0.Surface.TryGetValue(key, out var x); a0Shape.Surface.TryGetValue(key, out var y);
                    if (x == y) continue;
                    report.SurfaceDiffs++;
                    if (report.DiffSamples.Count < 10) report.DiffSamples.Add("surface " + key);
                }
                foreach (var kv in st.W0.BodyFingerprints)
                {
                    if (a0Shape.BodyFingerprints.TryGetValue(kv.Key, out var y) && y == kv.Value) continue;
                    report.BodyDiffs++;
                    if (report.DiffSamples.Count < 10) report.DiffSamples.Add("body " + kv.Key);
                }

                if (st.Allowed)
                {
                    foreach (var kv in st.W0.Methods)
                    {
                        var m = kv.Value;
                        if (!IsAllowlisted(m, allowMethods)) continue;
                        var (code, why) = CheckEligibility(m);
                        if (code == null)
                        {
                            if (!a0Shape.BodyFingerprints.TryGetValue(kv.Key, out var a0Fp))
                                (code, why) = (ResultCodes.BaselineParityFailed, "로드된 원본(A0)에 같은 MethodKey가 없습니다.");
                            else if (a0Fp != st.W0.BodyFingerprints[kv.Key])
                                (code, why) = (ResultCodes.BaselineParityFailed, "W0 본문과 로드된 A0 본문(IL 후처리 포함)이 다릅니다.");
                        }
                        if (code == null) st.TargetKeys.Add(kv.Key);
                        else
                        {
                            st.Ineligible[kv.Key] = (code, why);
                            if (code == ResultCodes.BaselineParityFailed) report.ParityFailedTargets.Add(kv.Key);
                            resp.Diagnostics.Add(new Diagnostic { Code = code, Message = why, MethodKey = kv.Key });
                        }
                    }
                }
                report.TargetCount = st.TargetKeys.Count;
                st.Report = report;
                resp.Baseline.Add(report);
                resp.AllTargets.AddRange(st.TargetKeys);
                resp.TimingsMs["baseline:" + input.Name] = sw.ElapsedMilliseconds;
                contextParts.Add(st.Ctx.ContextHash);
                _assemblies.Add(st);
            }
            resp.ContextHash = "sha256:" + ShapeModel.Hash(System.Text.Encoding.UTF8.GetBytes(string.Join("|", contextParts)));
            return resp;
        }

        public WorkerResponse Analyze(WorkerRequest req, CancellationToken ct)
        {
            var resp = new WorkerResponse { ResultCode = ResultCodes.Ok, Revision = req.Revision };
            if (!HasBaseline) { resp.ResultCode = ResultCodes.BaselineMissing; return resp; }
            var snapshot = SnapshotIndex(req.Snapshot);
            var blocking = new List<Diagnostic>();
            var changed = new List<MethodBodyIr>();
            foreach (var st in _assemblies) resp.AllTargets.AddRange(st.TargetKeys);

            foreach (var st in _assemblies)
            {
                var sw = Stopwatch.StartNew();
                var hashes = HashesFor(st.Ctx, snapshot);
                var changedFiles = hashes.Where(kv => st.FileHashes[kv.Key] != kv.Value).Select(kv => kv.Key).ToList();
                if (changedFiles.Count == 0) continue;
                if (!st.Allowed)
                {
                    foreach (var f in changedFiles)
                        blocking.Add(new Diagnostic { Code = ResultCodes.TargetNotAllowlisted, File = f, Message = $"허용되지 않은 어셈블리 {st.Input.Name}의 소스 변경" });
                    continue;
                }

                var compiled = st.Ctx.Compile(p => ReadBlob(snapshot, p), ct);
                resp.TimingsMs["compile:" + st.Input.Name] = sw.ElapsedMilliseconds;
                if (!compiled.Success)
                {
                    resp.Outcome = ResultCodes.CompileError;
                    resp.Diagnostics.AddRange(compiled.Errors);
                    continue;
                }
                using var sr = ModuleDefinition.ReadModule(new MemoryStream(compiled.Pe));
                var srShape = ShapeModel.Build(sr);
                DiffShape(st, srShape, blocking);
                DiffBodies(st, srShape, blocking, changed);
                resp.TimingsMs["analyze:" + st.Input.Name] = sw.ElapsedMilliseconds;
            }

            if (resp.Outcome == ResultCodes.CompileError) return resp;
            if (blocking.Count > 0)
            {
                resp.Outcome = ResultCodes.FullCompileRequired;
                resp.Diagnostics.AddRange(blocking);
                return resp;
            }
            resp.Outcome = ResultCodes.Patchable;
            resp.ChangedTargets = changed;
            return resp;
        }

        static void DiffShape(AssemblyState st, ShapeModel sr, List<Diagnostic> blocking)
        {
            foreach (var key in st.W0.Surface.Keys.Union(sr.Surface.Keys))
            {
                st.W0.Surface.TryGetValue(key, out var a);
                sr.Surface.TryGetValue(key, out var b);
                if (a == b) continue;
                string what = a == null ? "추가" : b == null ? "삭제" : "변경";
                string code = key.Contains('<') ? ResultCodes.UnsupportedGeneratedCode : ResultCodes.UnsupportedShapeChange;
                blocking.Add(new Diagnostic { Code = code, Message = $"{what}: {key}", MethodKey = key.StartsWith("M:") ? key.Substring(2) : null });
            }
        }

        void DiffBodies(AssemblyState st, ShapeModel sr, List<Diagnostic> blocking, List<MethodBodyIr> changed)
        {
            foreach (var kv in sr.BodyFingerprints)
            {
                if (!st.W0.BodyFingerprints.TryGetValue(kv.Key, out var w0Fp) || w0Fp == kv.Value) continue;
                var m = sr.Methods[kv.Key];
                if (m.Name.Contains('<') || m.DeclaringType.FullName.Contains('<'))
                {
                    blocking.Add(new Diagnostic { Code = ResultCodes.UnsupportedGeneratedCode, MethodKey = kv.Key, Message = "컴파일러 생성 코드(람다·로컬 함수·async/iterator 상태 머신) 본문 변경" });
                    continue;
                }
                if (m.IsConstructor)
                {
                    blocking.Add(new Diagnostic { Code = ResultCodes.UnsupportedShapeChange, MethodKey = kv.Key, Message = "생성자/정적 초기화(필드 초기값 포함) 변경" });
                    continue;
                }
                if (!st.TargetKeys.Contains(kv.Key))
                {
                    if (st.Ineligible.TryGetValue(kv.Key, out var why))
                        blocking.Add(new Diagnostic { Code = why.code, MethodKey = kv.Key, Message = why.why });
                    else
                        blocking.Add(new Diagnostic { Code = ResultCodes.TargetNotAllowlisted, MethodKey = kv.Key, Message = "allowlist에 없는 메서드 본문 변경" });
                    continue;
                }
                try
                {
                    var ir = IlExtractor.Extract(m);
                    string gen = IlExtractor.FindGeneratedReference(ir);
                    if (gen != null) throw new UnsupportedException(ResultCodes.UnsupportedGeneratedCode, "컴파일러 생성 코드 참조: " + gen);
                    if (_requireNoInlining && (m.ImplAttributes & MethodImplAttributes.NoInlining) == 0)
                        throw new UnsupportedException(ResultCodes.InliningRisk, "NoInlining이 없는 대상");
                    StackValidator.Validate(m);
                    changed.Add(ir);
                }
                catch (UnsupportedException e)
                {
                    blocking.Add(new Diagnostic { Code = e.Code, MethodKey = kv.Key, Message = e.Message });
                }
            }
        }

        (string code, string why) CheckEligibility(MethodDefinition m)
        {
            if (m.Name.Contains('<') || m.DeclaringType.FullName.Contains('<')) return (ResultCodes.UnsupportedGeneratedCode, "컴파일러 생성 메서드");
            if (m.IsConstructor) return (ResultCodes.UnsupportedTarget, "생성자/정적 생성자는 대상이 아닙니다.");
            if (!m.HasBody || m.IsAbstract || m.IsPInvokeImpl || m.IsInternalCall) return (ResultCodes.UnsupportedTarget, "IL 본문이 없는 메서드");
            if (m.HasGenericParameters) return (ResultCodes.UnsupportedTarget, "제네릭 메서드");
            for (var t = m.DeclaringType; t != null; t = t.DeclaringType)
                if (t.HasGenericParameters) return (ResultCodes.UnsupportedTarget, "제네릭 선언 타입의 메서드");
            if (m.DeclaringType.IsValueType && !m.IsStatic) return (ResultCodes.UnsupportedTarget, "struct 인스턴스 메서드");
            if (m.ReturnType is ByReferenceType) return (ResultCodes.UnsupportedTarget, "ref 반환");
            if (m.Parameters.Any(p => p.ParameterType is ByReferenceType)) return (ResultCodes.UnsupportedTarget, "ref/out/in 매개변수");
            if (m.CustomAttributes.Any(a => a.AttributeType.Name is "AsyncStateMachineAttribute" or "IteratorStateMachineAttribute"))
                return (ResultCodes.UnsupportedGeneratedCode, "async/iterator 메서드");
            if (_requireNoInlining && (m.ImplAttributes & MethodImplAttributes.NoInlining) == 0)
                return (ResultCodes.InliningRisk, "사전 컴파일된 NoInlining이 없습니다. Editor 조건부 [MethodImpl(NoInlining)]을 추가하고 정상 컴파일하세요.");
            try
            {
                var ir = IlExtractor.Extract(m);
                string gen = IlExtractor.FindGeneratedReference(ir);
                if (gen != null) return (ResultCodes.UnsupportedGeneratedCode, "본문이 컴파일러 생성 코드를 포함: " + gen);
                StackValidator.Validate(m);
            }
            catch (UnsupportedException e) { return (e.Code, e.Message); }
            return (null, null);
        }

        static bool IsAllowlisted(MethodDefinition m, HashSet<string> allow)
        {
            string type = ReflectionTypeName(m.DeclaringType);
            return allow.Contains(type + "::" + m.Name) || allow.Contains(type + "::*");
        }

        static string ReflectionTypeName(TypeReference t) =>
            t.DeclaringType != null ? ReflectionTypeName(t.DeclaringType) + "+" + t.Name : (string.IsNullOrEmpty(t.Namespace) ? t.Name : t.Namespace + "." + t.Name);

        string Abs(string p) => Path.GetFullPath(Path.Combine(_projectRoot, p));

        Dictionary<string, SnapshotFile> SnapshotIndex(List<SnapshotFile> files)
        {
            var d = new Dictionary<string, SnapshotFile>(StringComparer.OrdinalIgnoreCase);
            foreach (var f in files ?? new List<SnapshotFile>()) d[UnityCompileContext.Norm(Path.GetFullPath(Path.Combine(_projectRoot, f.Path)))] = f;
            return d;
        }

        static Dictionary<string, string> HashesFor(UnityCompileContext ctx, Dictionary<string, SnapshotFile> snapshot)
        {
            var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var p in ctx.SourcePaths)
            {
                if (!snapshot.TryGetValue(UnityCompileContext.Norm(p), out var f))
                    throw new UnsupportedException(ResultCodes.ContextChanged, $"스냅샷에 소스가 없습니다: {p}");
                d[UnityCompileContext.Norm(p)] = f.Sha256;
            }
            return d;
        }

        static byte[] ReadBlob(Dictionary<string, SnapshotFile> snapshot, string path)
        {
            var f = snapshot[UnityCompileContext.Norm(path)];
            byte[] bytes = File.ReadAllBytes(f.BlobPath);
            if ("sha256:" + ShapeModel.Hash(bytes) != f.Sha256)
                throw new UnsupportedException(ResultCodes.StaleRevision, $"스냅샷 blob 해시 불일치: {f.Path}");
            return bytes;
        }
    }
}
