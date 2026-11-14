using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;
using UPlayGround.HotReload.Protocol;

namespace UPlayGround.HotReload.Worker
{
    /// <summary>지원하지 않는 IL·타입·생성 코드를 발견했을 때 정확한 코드와 함께 던진다.</summary>
    public sealed class UnsupportedException : Exception
    {
        public string Code { get; }
        public UnsupportedException(string code, string message) : base(message) { Code = code; }
    }

    /// <summary>Cecil 메서드 본문을 토큰 번호와 무관한 의미 기반 IR로 정규화한다. 게임 코드를 실행하지 않는다.</summary>
    public static class IlExtractor
    {
        static readonly Dictionary<string, string> s_longBranch = new()
        {
            ["br.s"] = "br", ["brfalse.s"] = "brfalse", ["brtrue.s"] = "brtrue", ["beq.s"] = "beq",
            ["bge.s"] = "bge", ["bgt.s"] = "bgt", ["ble.s"] = "ble", ["blt.s"] = "blt",
            ["bne.un.s"] = "bne.un", ["bge.un.s"] = "bge.un", ["bgt.un.s"] = "bgt.un",
            ["ble.un.s"] = "ble.un", ["blt.un.s"] = "blt.un", ["leave.s"] = "leave",
        };

        static readonly HashSet<string> s_rejectedOpcodes = new()
        {
            "calli", "jmp", "localloc", "cpblk", "initblk", "arglist", "mkrefany", "refanyval",
            "refanytype", "tail.", "no.", "endfilter", "cpobj",
        };

        public static string MethodKey(MethodReference m) => CanonMember(ToMemberRef(m));

        public static MethodBodyIr Extract(MethodDefinition method)
        {
            if (!method.HasBody)
                throw new UnsupportedException(ResultCodes.UnsupportedTarget, $"본문 없는 메서드: {method.FullName}");

            var body = method.Body;
            var ir = new MethodBodyIr { Method = ToMemberRef(method), InitLocals = body.InitLocals };
            ir.MethodKey = CanonMember(ir.Method);

            foreach (var v in body.Variables)
            {
                var vt = v.VariableType;
                bool pinned = vt is PinnedType;
                if (pinned) throw new UnsupportedException(ResultCodes.UnsupportedIL, $"pinned local: {method.FullName}");
                ir.Locals.Add(new IrLocal { Type = ToTypeRef(vt), Pinned = false });
            }

            var index = new Dictionary<Instruction, int>();
            for (int i = 0; i < body.Instructions.Count; i++) index[body.Instructions[i]] = i;
            int end = body.Instructions.Count;
            int IndexOf(Instruction ins) => ins == null ? end : index[ins];

            foreach (var ins in body.Instructions)
                ir.Instructions.Add(Normalize(method, ins, IndexOf));

            foreach (var h in body.ExceptionHandlers)
            {
                string kind = h.HandlerType switch
                {
                    ExceptionHandlerType.Catch => IrExceptionRegion.Catch,
                    ExceptionHandlerType.Finally => IrExceptionRegion.Finally,
                    _ => throw new UnsupportedException(ResultCodes.UnsupportedIL, $"{h.HandlerType} 예외 영역은 v1 미지원: {method.FullName}"),
                };
                ir.Regions.Add(new IrExceptionRegion
                {
                    Kind = kind,
                    TryStart = IndexOf(h.TryStart), TryEnd = IndexOf(h.TryEnd),
                    HandlerStart = IndexOf(h.HandlerStart), HandlerEnd = IndexOf(h.HandlerEnd),
                    CatchType = h.CatchType == null ? null : ToTypeRef(h.CatchType),
                });
            }
            ValidateRegionNesting(method, ir.Regions);
            ir.Fingerprint = Fingerprint(ir);
            return ir;
        }

        static IrInstruction Normalize(MethodDefinition method, Instruction ins, Func<Instruction, int> indexOf)
        {
            string op = ins.OpCode.Name;
            if (s_rejectedOpcodes.Contains(op))
                throw new UnsupportedException(ResultCodes.UnsupportedIL, $"opcode '{op}' v1 미지원: {method.FullName} IL_{ins.Offset:X4}");

            var r = new IrInstruction { OperandKind = IrInstruction.OpNone };
            if (s_longBranch.TryGetValue(op, out var lb)) op = lb;

            // 매크로 opcode는 의미가 같은 일반형으로 정규화해 지문이 인코딩 차이에 흔들리지 않게 한다.
            if (TryMacro(op, "ldloc", out int n) || TryMacro(op, "stloc", out n)) { r.Op = op.Substring(0, 5); r.OperandKind = IrInstruction.OpLocal; r.Index = n; return r; }
            if (TryMacro(op, "ldarg", out n)) { r.Op = "ldarg"; r.OperandKind = IrInstruction.OpArg; r.Index = n; return r; }
            if (op.StartsWith("ldc.i4.") && op != "ldc.i4.s")
            {
                r.Op = "ldc.i4"; r.OperandKind = IrInstruction.OpInt;
                r.Value = op == "ldc.i4.m1" ? -1 : int.Parse(op.Substring(7));
                return r;
            }

            switch (op)
            {
                case "ldloc.s": case "stloc.s": case "ldloca.s": op = op.Substring(0, op.Length - 2); break;
                case "ldarg.s": case "starg.s": case "ldarga.s": op = op.Substring(0, op.Length - 2); break;
                case "ldc.i4.s": op = "ldc.i4"; break;
                // Cecil의 형식 지정 배열 명령 이름을 Reflection.Emit의 표준 이름으로 맞춘다.
                case "ldelem.any": op = "ldelem"; break;
                case "stelem.any": op = "stelem"; break;
            }
            r.Op = op;

            object operand = ins.Operand;
            switch (ins.OpCode.OperandType)
            {
                case OperandType.InlineNone:
                    break;
                case OperandType.InlineBrTarget:
                case OperandType.ShortInlineBrTarget:
                    r.OperandKind = IrInstruction.OpBranch; r.Target = indexOf((Instruction)operand); break;
                case OperandType.InlineSwitch:
                    r.OperandKind = IrInstruction.OpSwitch; r.Targets = ((Instruction[])operand).Select(indexOf).ToArray(); break;
                case OperandType.InlineI:
                    r.OperandKind = IrInstruction.OpInt; r.Value = (int)operand; break;
                case OperandType.ShortInlineI:
                    r.OperandKind = IrInstruction.OpInt; r.Value = operand is sbyte sb ? sb : Convert.ToInt64(operand); break;
                case OperandType.InlineI8:
                    r.OperandKind = IrInstruction.OpLong; r.Value = (long)operand; break;
                case OperandType.ShortInlineR:
                    r.OperandKind = IrInstruction.OpR4; r.Value = BitConverter.SingleToInt32Bits((float)operand); break;
                case OperandType.InlineR:
                    r.OperandKind = IrInstruction.OpR8; r.Value = BitConverter.DoubleToInt64Bits((double)operand); break;
                case OperandType.InlineString:
                    r.OperandKind = IrInstruction.OpString; r.Text = (string)operand; break;
                case OperandType.InlineVar:
                case OperandType.ShortInlineVar:
                    if (operand is VariableDefinition vd) { r.OperandKind = IrInstruction.OpLocal; r.Index = vd.Index; }
                    else { r.OperandKind = IrInstruction.OpArg; r.Index = ArgNumber(method, (ParameterDefinition)operand); }
                    break;
                case OperandType.InlineArg:
                case OperandType.ShortInlineArg:
                    r.OperandKind = IrInstruction.OpArg; r.Index = ArgNumber(method, (ParameterDefinition)operand); break;
                case OperandType.InlineType:
                    r.OperandKind = IrInstruction.OpType; r.Type = ToTypeRef((TypeReference)operand); break;
                case OperandType.InlineField:
                case OperandType.InlineMethod:
                    r.OperandKind = IrInstruction.OpMember; r.Member = ToMemberRef((MemberReference)operand); break;
                case OperandType.InlineTok:
                    if (operand is TypeReference tr) { r.OperandKind = IrInstruction.OpType; r.Type = ToTypeRef(tr); }
                    else { r.OperandKind = IrInstruction.OpMember; r.Member = ToMemberRef((MemberReference)operand); }
                    break;
                default:
                    throw new UnsupportedException(ResultCodes.UnsupportedIL, $"피연산자 형식 {ins.OpCode.OperandType} 미지원: {method.FullName} IL_{ins.Offset:X4}");
            }
            return r;
        }

        static bool TryMacro(string op, string prefix, out int n)
        {
            n = 0;
            if (op.Length != prefix.Length + 2 || !op.StartsWith(prefix + ".")) return false;
            char c = op[^1];
            if (c < '0' || c > '3') return false;
            n = c - '0';
            return true;
        }

        static int ArgNumber(MethodDefinition method, ParameterDefinition p)
        {
            if (p == method.Body.ThisParameter) return 0;
            return p.Index + (method.HasThis ? 1 : 0);
        }

        static void ValidateRegionNesting(MethodDefinition method, List<IrExceptionRegion> regions)
        {
            static bool Disjoint(int a0, int a1, int b0, int b1) => a1 <= b0 || b1 <= a0;
            static bool Inside(int a0, int a1, int b0, int b1) => b0 <= a0 && a1 <= b1;
            for (int i = 0; i < regions.Count; i++)
            {
                var a = regions[i];
                if (a.TryEnd != a.HandlerStart)
                    throw new UnsupportedException(ResultCodes.UnsupportedIL, $"try와 handler가 인접하지 않은 예외 영역: {method.FullName}");
                for (int j = 0; j < regions.Count; j++)
                {
                    if (i == j) continue;
                    var b = regions[j];
                    bool sameTry = a.TryStart == b.TryStart && a.TryEnd == b.TryEnd;
                    int a0 = a.TryStart, a1 = a.HandlerEnd, b0 = b.TryStart, b1 = b.HandlerEnd;
                    if (sameTry || Disjoint(a0, a1, b0, b1) || Inside(a0, a1, b0, b1) || Inside(b0, b1, a0, a1)) continue;
                    throw new UnsupportedException(ResultCodes.UnsupportedIL, $"올바르게 중첩되지 않은 예외 영역: {method.FullName}");
                }
            }
        }

        public static TypeRef ToTypeRef(TypeReference t)
        {
            switch (t)
            {
                case RequiredModifierType rm: return ToTypeRef(rm.ElementType);
                case OptionalModifierType om: return ToTypeRef(om.ElementType);
                case GenericInstanceType gi:
                    return new TypeRef { Kind = TypeRef.GenericInstance, Element = ToTypeRef(gi.ElementType), Args = gi.GenericArguments.Select(ToTypeRef).ToList() };
                case ArrayType at:
                    return new TypeRef { Kind = TypeRef.Array, Rank = at.Rank, Element = ToTypeRef(at.ElementType) };
                case ByReferenceType br:
                    return new TypeRef { Kind = TypeRef.ByRef, Element = ToTypeRef(br.ElementType) };
                case GenericParameter gp:
                    return new TypeRef { Kind = TypeRef.GenericParam, Position = gp.Position, MethodOwned = gp.Type == GenericParameterType.Method };
                case PointerType:
                case FunctionPointerType:
                case PinnedType:
                case SentinelType:
                    throw new UnsupportedException(ResultCodes.UnsupportedIL, $"포인터/함수 포인터 형식 미지원: {t.FullName}");
            }
            return new TypeRef { Kind = TypeRef.Named, Assembly = AssemblyIdentity(t), Name = ReflectionName(t) };
        }

        static string ReflectionName(TypeReference t)
        {
            if (t.DeclaringType != null) return ReflectionName(t.DeclaringType) + "+" + t.Name;
            return string.IsNullOrEmpty(t.Namespace) ? t.Name : t.Namespace + "." + t.Name;
        }

        static string AssemblyIdentity(TypeReference t)
        {
            while (t.DeclaringType != null) t = t.DeclaringType;
            return t.Scope switch
            {
                AssemblyNameReference anr => anr.FullName,
                ModuleDefinition md => md.Assembly.Name.FullName,
                ModuleReference mr => throw new UnsupportedException(ResultCodes.UnsupportedIL, $"다중 모듈 참조 미지원: {mr.Name}"),
                _ => throw new UnsupportedException(ResultCodes.UnsupportedIL, $"알 수 없는 타입 scope: {t.FullName}"),
            };
        }

        public static MemberRef ToMemberRef(MemberReference m)
        {
            if (m is FieldReference f)
                return new MemberRef { Kind = MemberRef.Field, DeclaringType = ToTypeRef(f.DeclaringType), Name = f.Name, FieldType = ToTypeRef(f.FieldType) };

            var mr = (MethodReference)m;
            List<TypeRef> methodArgs = null;
            if (mr is GenericInstanceMethod gim)
            {
                methodArgs = gim.GenericArguments.Select(ToTypeRef).ToList();
                mr = gim.ElementMethod;
            }
            return new MemberRef
            {
                Kind = mr.Name == ".ctor" || mr.Name == ".cctor" ? MemberRef.Ctor : MemberRef.Method,
                DeclaringType = ToTypeRef(mr.DeclaringType),
                Name = mr.Name,
                Parameters = mr.Parameters.Select(p => ToTypeRef(p.ParameterType)).ToList(),
                ReturnType = ToTypeRef(mr.ReturnType),
                GenericArity = mr.GenericParameters.Count,
                MethodArgs = methodArgs,
                HasThis = mr.HasThis,
            };
        }

        public static string CanonType(TypeRef t)
        {
            if (t == null) return "null";
            switch (t.Kind)
            {
                case TypeRef.Named:
                    int comma = t.Assembly.IndexOf(',');
                    return "[" + (comma < 0 ? t.Assembly : t.Assembly.Substring(0, comma)) + "]" + t.Name;
                case TypeRef.Array: return CanonType(t.Element) + "[" + new string(',', Math.Max(0, t.Rank - 1)) + "]";
                case TypeRef.ByRef: return CanonType(t.Element) + "&";
                case TypeRef.GenericInstance: return CanonType(t.Element) + "<" + string.Join(",", t.Args.Select(CanonType)) + ">";
                case TypeRef.GenericParam: return (t.MethodOwned ? "!!" : "!") + t.Position;
            }
            return "?" + t.Kind;
        }

        public static string CanonMember(MemberRef m)
        {
            if (m == null) return "null";
            if (m.Kind == MemberRef.Field) return "F:" + CanonType(m.DeclaringType) + "::" + m.Name + ":" + CanonType(m.FieldType);
            var sb = new StringBuilder();
            sb.Append(m.HasThis ? "M:" : "S:").Append(CanonType(m.DeclaringType)).Append("::").Append(m.Name);
            if (m.GenericArity > 0) sb.Append('`').Append(m.GenericArity);
            if (m.MethodArgs != null) sb.Append('<').Append(string.Join(",", m.MethodArgs.Select(CanonType))).Append('>');
            sb.Append('(').Append(string.Join(",", m.Parameters.Select(CanonType))).Append(")->").Append(CanonType(m.ReturnType));
            return sb.ToString();
        }

        public static string Fingerprint(MethodBodyIr ir)
        {
            var sb = new StringBuilder();
            sb.Append(ir.InitLocals).Append('|');
            foreach (var l in ir.Locals) sb.Append(CanonType(l.Type)).Append(l.Pinned ? "*" : "").Append(';');
            sb.Append('|');
            foreach (var i in ir.Instructions)
            {
                sb.Append(i.Op).Append(' ').Append(i.OperandKind).Append(' ');
                switch (i.OperandKind)
                {
                    case IrInstruction.OpInt: case IrInstruction.OpLong: case IrInstruction.OpR4: case IrInstruction.OpR8: sb.Append(i.Value); break;
                    case IrInstruction.OpString: sb.Append(i.Text.Length).Append(':').Append(i.Text); break;
                    case IrInstruction.OpBranch: sb.Append(i.Target); break;
                    case IrInstruction.OpSwitch: sb.Append(string.Join(",", i.Targets)); break;
                    case IrInstruction.OpLocal: case IrInstruction.OpArg: sb.Append(i.Index); break;
                    case IrInstruction.OpType: sb.Append(CanonType(i.Type)); break;
                    case IrInstruction.OpMember: sb.Append(CanonMember(i.Member)); break;
                }
                sb.Append('\n');
            }
            foreach (var r in ir.Regions)
                sb.Append(r.Kind).Append(r.TryStart).Append(',').Append(r.TryEnd).Append(',').Append(r.HandlerStart).Append(',').Append(r.HandlerEnd).Append(CanonType(r.CatchType)).Append(';');
            return "sha256:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString()))).ToLowerInvariant();
        }

        /// <summary>본문이 컴파일러 생성 타입·멤버(람다 display class, 상태 머신, PrivateImplementationDetails 등)를 참조하는지 검사한다.</summary>
        public static string FindGeneratedReference(MethodBodyIr ir)
        {
            foreach (var l in ir.Locals) if (IsGenerated(l.Type)) return "local " + CanonType(l.Type);
            foreach (var i in ir.Instructions)
            {
                if (i.Type != null && IsGenerated(i.Type)) return i.Op + " " + CanonType(i.Type);
                if (i.Member != null && (i.Member.Name.Contains('<') || IsGenerated(i.Member.DeclaringType))) return i.Op + " " + CanonMember(i.Member);
            }
            return null;
        }

        static bool IsGenerated(TypeRef t)
        {
            if (t == null) return false;
            if (t.Kind == TypeRef.Named) return t.Name.Contains('<');
            if (IsGenerated(t.Element)) return true;
            return t.Args != null && t.Args.Any(IsGenerated);
        }
    }
}
