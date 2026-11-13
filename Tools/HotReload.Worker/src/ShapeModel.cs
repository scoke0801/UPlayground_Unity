using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Mono.Cecil;
using UPlayGround.HotReload.Protocol;

namespace UPlayGround.HotReload.Worker
{
    /// <summary>
    /// 어셈블리의 런타임 표면(타입·필드·메서드 시그니처·attribute·const·초기값·리소스)과 메서드 본문 지문.
    /// 표면 차이는 본문 교체로 표현할 수 없으므로 전체 배치 거부 사유가 된다.
    /// </summary>
    public sealed class ShapeModel
    {
        public readonly SortedDictionary<string, string> Surface = new(StringComparer.Ordinal);
        public readonly Dictionary<string, string> BodyFingerprints = new(StringComparer.Ordinal);
        public readonly Dictionary<string, MethodDefinition> Methods = new(StringComparer.Ordinal);

        public static ShapeModel Build(ModuleDefinition module)
        {
            var s = new ShapeModel();
            foreach (var a in module.Assembly.CustomAttributes) s.Surface["A:" + Attr(a)] = "";
            foreach (var r in module.Resources)
                s.Surface["R:" + r.Name] = r is EmbeddedResource er ? Hash(er.GetResourceData()) : r.ResourceType.ToString();

            foreach (var t in AllTypes(module))
            {
                string tn = t.FullName;
                s.Surface["T:" + tn] = $"{t.Attributes}|base={t.BaseType?.FullName}|gp={t.GenericParameters.Count}|if={string.Join(",", t.Interfaces.Select(i => i.InterfaceType.FullName).OrderBy(x => x))}|ca={Attrs(t.CustomAttributes)}|layout={t.PackingSize},{t.ClassSize}";
                foreach (var f in t.Fields)
                    s.Surface[$"F:{tn}::{f.Name}"] = $"{f.Attributes}|{f.FieldType.FullName}|const={(f.HasConstant ? Convert.ToString(f.Constant, System.Globalization.CultureInfo.InvariantCulture) : "-")}|init={(f.InitialValue?.Length > 0 ? Hash(f.InitialValue) : "-")}|off={f.Offset}|ca={Attrs(f.CustomAttributes)}";
                foreach (var p in t.Properties)
                    s.Surface[$"P:{tn}::{p.Name}({string.Join(",", p.Parameters.Select(x => x.ParameterType.FullName))})"] = $"{p.PropertyType.FullName}|get={p.GetMethod?.Name}|set={p.SetMethod?.Name}|ca={Attrs(p.CustomAttributes)}";
                foreach (var e in t.Events)
                    s.Surface[$"E:{tn}::{e.Name}"] = $"{e.EventType.FullName}|ca={Attrs(e.CustomAttributes)}";
                foreach (var m in t.Methods)
                {
                    string key = IlExtractor.MethodKey(m);
                    s.Methods[key] = m;
                    s.Surface["M:" + key] = $"{m.Attributes}|{m.ImplAttributes}|ca={Attrs(m.CustomAttributes)}|ret={Attrs(m.MethodReturnType.CustomAttributes)}|p={string.Join(";", m.Parameters.Select(p => $"{p.Name}:{p.Attributes}:{(p.HasConstant ? Convert.ToString(p.Constant, System.Globalization.CultureInfo.InvariantCulture) : "-")}:{Attrs(p.CustomAttributes)}"))}|ovr={string.Join(",", m.Overrides.Select(o => o.FullName))}";
                    if (m.HasBody) s.BodyFingerprints[key] = BodyFingerprint(m);
                }
            }
            return s;
        }

        /// <summary>v1 IR로 표현 가능한 본문은 IR 지문, 아니면 Cecil 문자열 기반 변경 감지용 지문을 쓴다.</summary>
        static string BodyFingerprint(MethodDefinition m)
        {
            try { return IlExtractor.Extract(m).Fingerprint; }
            catch (UnsupportedException)
            {
                var sb = new StringBuilder("raw|");
                foreach (var v in m.Body.Variables) sb.Append(v.VariableType.FullName).Append(';');
                foreach (var i in m.Body.Instructions) sb.Append(i.OpCode.Name).Append(' ').Append(OperandText(i.Operand)).Append('\n');
                foreach (var h in m.Body.ExceptionHandlers) sb.Append(h.HandlerType).Append(h.CatchType?.FullName).Append(';');
                return "raw:" + Hash(Encoding.UTF8.GetBytes(sb.ToString()));
            }
        }

        static string OperandText(object o) => o switch
        {
            null => "",
            Mono.Cecil.Cil.Instruction i => "IL_" + i.Offset,
            Mono.Cecil.Cil.Instruction[] a => string.Join(",", a.Select(x => "IL_" + x.Offset)),
            MemberReference mr => mr.FullName,
            _ => Convert.ToString(o, System.Globalization.CultureInfo.InvariantCulture),
        };

        public static IEnumerable<TypeDefinition> AllTypes(ModuleDefinition module)
        {
            var stack = new Stack<TypeDefinition>(module.Types);
            while (stack.Count > 0)
            {
                var t = stack.Pop();
                yield return t;
                foreach (var n in t.NestedTypes) stack.Push(n);
            }
        }

        static string Attrs(IEnumerable<CustomAttribute> attrs) => string.Join(",", attrs.Select(Attr).OrderBy(x => x, StringComparer.Ordinal));
        static string Attr(CustomAttribute a) => a.Constructor.FullName + "#" + Convert.ToHexString(a.GetBlob());
        public static string Hash(byte[] data) => Convert.ToHexString(SHA256.HashData(data)).ToLowerInvariant();
    }
}
