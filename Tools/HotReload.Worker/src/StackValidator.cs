using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;
using UPlayGround.HotReload.Protocol;

namespace UPlayGround.HotReload.Worker
{
    /// <summary>
    /// 지원 opcode 하위 집합에 대해 스택 높이·분기 합류·반환 경로를 확인한다.
    /// 완전한 CLI verifier가 아니며, 해석할 수 없는 조합은 거부한다.
    /// </summary>
    public static class StackValidator
    {
        public static void Validate(MethodDefinition method)
        {
            var body = method.Body;
            var ins = body.Instructions;
            var index = new Dictionary<Instruction, int>();
            for (int i = 0; i < ins.Count; i++) index[ins[i]] = i;

            var depth = new int?[ins.Count];
            var work = new Stack<int>();
            void Enqueue(int at, int d)
            {
                if (at < 0 || at >= ins.Count) Fail(method, $"분기 대상 범위 밖 ({at})");
                if (depth[at] == null) { depth[at] = d; work.Push(at); }
                else if (depth[at] != d) Fail(method, $"합류 지점 스택 높이 불일치 IL_{ins[at].Offset:X4} ({depth[at]} vs {d})");
            }

            Enqueue(0, 0);
            foreach (var h in body.ExceptionHandlers)
            {
                if (h.HandlerType == ExceptionHandlerType.Catch) Enqueue(index[h.HandlerStart], 1);
                else if (h.HandlerType == ExceptionHandlerType.Finally) Enqueue(index[h.HandlerStart], 0);
                else Fail(method, $"{h.HandlerType} 미지원");
            }

            bool returnsValue = method.ReturnType.MetadataType != MetadataType.Void;
            while (work.Count > 0)
            {
                int at = work.Pop();
                var i = ins[at];
                int d = depth[at].Value - Pop(method, i);
                if (d < 0) Fail(method, $"스택 underflow IL_{i.Offset:X4} {i.OpCode.Name}");
                d += Push(i);

                var flow = i.OpCode.FlowControl;
                string name = i.OpCode.Name;
                if (name == "ret")
                {
                    if (d != 0) Fail(method, $"ret 후 스택 잔여 {d}");
                    continue;
                }
                if (name == "throw" || name == "rethrow" || name == "endfinally") continue;
                if (name.StartsWith("leave"))
                {
                    Enqueue(index[(Instruction)i.Operand], 0);
                    continue;
                }
                if (i.Operand is Instruction target) Enqueue(index[target], d);
                if (i.Operand is Instruction[] targets) foreach (var t in targets) Enqueue(index[t], d);
                if (flow == FlowControl.Branch) continue;
                if (at + 1 >= ins.Count) Fail(method, "메서드 끝을 넘어서는 실행 경로");
                Enqueue(at + 1, d);
            }
            _ = returnsValue;
        }

        static int Pop(MethodDefinition owner, Instruction i)
        {
            if (i.Operand is MethodReference m && i.OpCode.StackBehaviourPop == StackBehaviour.Varpop)
            {
                int n = m.Parameters.Count;
                if (m.HasThis && i.OpCode.Code != Code.Newobj) n++;
                return n;
            }
            if (i.OpCode.Code == Code.Ret) return owner.ReturnType.MetadataType == MetadataType.Void ? 0 : 1;
            return i.OpCode.StackBehaviourPop switch
            {
                StackBehaviour.Pop0 => 0,
                StackBehaviour.Pop1 or StackBehaviour.Popi or StackBehaviour.Popref => 1,
                StackBehaviour.Pop1_pop1 or StackBehaviour.Popi_pop1 or StackBehaviour.Popi_popi or StackBehaviour.Popi_popi8
                    or StackBehaviour.Popi_popr4 or StackBehaviour.Popi_popr8 or StackBehaviour.Popref_pop1 or StackBehaviour.Popref_popi => 2,
                StackBehaviour.Popi_popi_popi or StackBehaviour.Popref_popi_popi or StackBehaviour.Popref_popi_popi8
                    or StackBehaviour.Popref_popi_popr4 or StackBehaviour.Popref_popi_popr8 or StackBehaviour.Popref_popi_popref => 3,
                StackBehaviour.PopAll => 0,
                _ => throw new UnsupportedException(ResultCodes.UnsupportedIL, $"스택 동작 해석 불가 {i.OpCode.Name}"),
            };
        }

        static int Push(Instruction i)
        {
            if (i.Operand is MethodReference m && i.OpCode.StackBehaviourPush == StackBehaviour.Varpush)
                return m.ReturnType.MetadataType == MetadataType.Void ? 0 : 1;
            return i.OpCode.StackBehaviourPush switch
            {
                StackBehaviour.Push0 => 0,
                StackBehaviour.Push1 or StackBehaviour.Pushi or StackBehaviour.Pushi8 or StackBehaviour.Pushr4
                    or StackBehaviour.Pushr8 or StackBehaviour.Pushref => 1,
                StackBehaviour.Push1_push1 => 2,
                _ => throw new UnsupportedException(ResultCodes.UnsupportedIL, $"스택 동작 해석 불가 {i.OpCode.Name}"),
            };
        }

        static void Fail(MethodDefinition m, string why) =>
            throw new UnsupportedException(ResultCodes.UnsupportedIL, $"스택 검증 실패 {m.FullName}: {why}");
    }
}
