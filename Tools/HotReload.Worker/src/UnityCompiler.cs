using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using UPlayGround.HotReload.Protocol;
using Diagnostic = Microsoft.CodeAnalysis.Diagnostic;

namespace UPlayGround.HotReload.Worker
{
    /// <summary>
    /// Unity가 실제 csc에 넘긴 응답 파일(rsp)을 Roslyn 명령줄 파서로 그대로 해석해 같은 컴파일을 재현한다.
    /// 참조·define·옵션·source generator·additional file을 rsp에서 얻으므로 IDE csproj나 Worker 호스트 BCL이 섞이지 않는다.
    /// </summary>
    public sealed class UnityCompileContext
    {
        public string Name;
        public CSharpCommandLineArguments Args;
        public List<string> SourcePaths;
        public ImmutableArray<MetadataReference> References;
        public ImmutableArray<ISourceGenerator> Generators;
        public ImmutableArray<AdditionalText> AdditionalTexts;
        public string ContextHash;
        public List<string> GeneratorNames = new();

        static readonly ConcurrentDictionary<string, (string hash, MetadataReference reference)> s_refCache = new(StringComparer.OrdinalIgnoreCase);

        public static UnityCompileContext Create(AssemblyInput input, string projectRoot, string trustedAnalyzerRoot)
        {
            string rsp = Path.GetFullPath(Path.Combine(projectRoot, input.RspPath));
            if (!File.Exists(rsp)) throw new UnsupportedException(ResultCodes.BaselineMissing, $"rsp 없음: {input.RspPath}");
            var args = CSharpCommandLineParser.Default.Parse(new[] { "@" + rsp }, projectRoot, sdkDirectory: null);
            var errors = args.Errors.Where(e => e.Severity == DiagnosticSeverity.Error).ToList();
            if (errors.Count > 0) throw new UnsupportedException(ResultCodes.ContextChanged, "rsp 해석 오류: " + string.Join("; ", errors.Select(e => e.GetMessage())));

            var ctx = new UnityCompileContext { Name = input.Name, Args = args };
            ctx.SourcePaths = args.SourceFiles.Select(s => s.Path).ToList();

            var expected = new HashSet<string>(input.SourceFiles.Select(p => Norm(Path.Combine(projectRoot, p))), StringComparer.OrdinalIgnoreCase);
            var actual = new HashSet<string>(ctx.SourcePaths.Select(Norm), StringComparer.OrdinalIgnoreCase);
            if (!expected.SetEquals(actual))
                throw new UnsupportedException(ResultCodes.ContextChanged, $"{input.Name}: Unity 파이프라인 소스 목록과 rsp가 다릅니다(마지막 정상 컴파일 이후 파일 추가/삭제). 정상 컴파일이 필요합니다.");

            var hashParts = new List<string> { "rsp=" + ShapeModel.Hash(File.ReadAllBytes(rsp)) };
            var refs = ImmutableArray.CreateBuilder<MetadataReference>();
            foreach (var r in args.MetadataReferences)
            {
                string path = Path.GetFullPath(Path.Combine(projectRoot, r.Reference));
                if (!File.Exists(path)) throw new UnsupportedException(ResultCodes.ContextChanged, $"참조 없음: {r.Reference}");
                byte[] bytes = File.ReadAllBytes(path);
                string h = ShapeModel.Hash(bytes);
                hashParts.Add(Norm(path) + "=" + h);
                var cached = s_refCache.GetOrAdd(path, _ => (h, MetadataReference.CreateFromImage(bytes, r.Properties, filePath: path)));
                if (cached.hash != h)
                {
                    cached = (h, MetadataReference.CreateFromImage(bytes, r.Properties, filePath: path));
                    s_refCache[path] = cached;
                }
                refs.Add(cached.reference);
            }
            ctx.References = refs.ToImmutable();

            var gens = ImmutableArray.CreateBuilder<ISourceGenerator>();
            string trusted = Norm(trustedAnalyzerRoot ?? "");
            foreach (var a in args.AnalyzerReferences)
            {
                string path = Norm(Path.GetFullPath(Path.Combine(projectRoot, a.FilePath)));
                if (string.IsNullOrEmpty(trusted) || !path.StartsWith(trusted, StringComparison.OrdinalIgnoreCase))
                    throw new UnsupportedException(ResultCodes.UnsupportedGeneratedCode, $"신뢰 경로 밖 source generator/analyzer는 실행하지 않습니다: {a.FilePath}");
                hashParts.Add("gen:" + path + "=" + ShapeModel.Hash(File.ReadAllBytes(path)));
                var afr = new AnalyzerFileReference(path, AnalyzerLoader.Instance);
                foreach (var g in afr.GetGeneratorsForAllLanguages())
                {
                    gens.Add(g);
                    ctx.GeneratorNames.Add(Path.GetFileName(path) + ":" + g.GetGeneratorType().FullName);
                }
            }
            ctx.Generators = gens.ToImmutable();

            var texts = ImmutableArray.CreateBuilder<AdditionalText>();
            foreach (var f in args.AdditionalFiles)
            {
                string content = File.Exists(f.Path) ? File.ReadAllText(f.Path) : "";
                hashParts.Add("add:" + Norm(f.Path) + "=" + ShapeModel.Hash(Encoding.UTF8.GetBytes(content)));
                texts.Add(new MemoryAdditionalText(f.Path, content));
            }
            ctx.AdditionalTexts = texts.ToImmutable();
            ctx.ContextHash = "sha256:" + ShapeModel.Hash(Encoding.UTF8.GetBytes(string.Join("\n", hashParts)));
            return ctx;
        }

        public sealed class CompileResult
        {
            public byte[] Pe;
            public List<Protocol.Diagnostic> Errors = new();
            public bool Success => Pe != null;
        }

        /// <summary>불변 스냅샷 텍스트로 컴파일한다. 원본 경로를 SyntaxTree 경로로 유지해 진단·CallerFilePath·PDB 문서 경로가 Unity와 같게 한다.</summary>
        public CompileResult Compile(Func<string, byte[]> readSource, CancellationToken ct)
        {
            var trees = new List<SyntaxTree>(SourcePaths.Count);
            foreach (var path in SourcePaths)
            {
                var text = SourceText.From(new MemoryStream(readSource(path)), Args.Encoding ?? new UTF8Encoding(false), Args.ChecksumAlgorithm, throwIfBinaryDetected: true, canBeEmbedded: true);
                trees.Add(CSharpSyntaxTree.ParseText(text, Args.ParseOptions, path, ct));
            }

            string name = Args.CompilationName ?? Path.GetFileNameWithoutExtension(Args.OutputFileName);
            Compilation comp = CSharpCompilation.Create(name, trees, References, Args.CompilationOptions);
            if (Generators.Length > 0)
            {
                var driver = CSharpGeneratorDriver.Create(Generators, AdditionalTexts, (CSharpParseOptions)Args.ParseOptions);
                driver.RunGeneratorsAndUpdateCompilation(comp, out comp, out _, ct);
            }

            var result = new CompileResult();
            string outPath = Path.Combine(Args.OutputDirectory, Args.OutputFileName);
            string pdbPath = Args.PdbPath ?? Path.ChangeExtension(outPath, ".pdb");
            var emitOptions = Args.EmitOptions.WithPdbFilePath(pdbPath);
            using var pe = new MemoryStream();
            using var pdb = new MemoryStream();
            var emit = comp.Emit(pe, pdbStream: emitOptions.DebugInformationFormat == Microsoft.CodeAnalysis.Emit.DebugInformationFormat.Embedded ? null : pdb, options: emitOptions, cancellationToken: ct);
            foreach (var d in emit.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error))
            {
                var span = d.Location.GetMappedLineSpan();
                result.Errors.Add(new Protocol.Diagnostic
                {
                    Code = d.Id, Message = d.GetMessage(), File = span.Path,
                    Line = span.StartLinePosition.Line + 1, Column = span.StartLinePosition.Character + 1,
                });
            }
            if (emit.Success) result.Pe = pe.ToArray();
            return result;
        }

        public static string Norm(string p) => p.Replace('\\', '/');

        sealed class MemoryAdditionalText : AdditionalText
        {
            readonly SourceText _text;
            public MemoryAdditionalText(string path, string content) { Path = path; _text = SourceText.From(content); }
            public override string Path { get; }
            public override SourceText GetText(CancellationToken cancellationToken = default) => _text;
        }

        sealed class AnalyzerLoader : IAnalyzerAssemblyLoader
        {
            public static readonly AnalyzerLoader Instance = new();
            public void AddDependencyLocation(string fullPath) { }
            public Assembly LoadFromPath(string fullPath) => Assembly.LoadFrom(fullPath);
        }
    }
}
