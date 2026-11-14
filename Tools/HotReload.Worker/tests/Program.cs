using System;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using UPlayGround.HotReload.Protocol;
using UPlayGround.HotReload.Worker;

// Unity를 시작하지 않고 실제 Cecil → IR 경로의 배열 opcode 호환성을 확인한다.
using var module = ModuleDefinition.CreateModule("ArrayOpcodeRegression", ModuleKind.Dll);
var value = new TypeDefinition("Regression", "Value", TypeAttributes.Public | TypeAttributes.Sealed,
    module.ImportReference(typeof(ValueType)));
module.Types.Add(value);
var emitOpcodes = typeof(System.Reflection.Emit.OpCodes).GetFields()
    .Where(field => field.FieldType == typeof(System.Reflection.Emit.OpCode))
    .Select(field => ((System.Reflection.Emit.OpCode)field.GetValue(null)!).Name).ToHashSet();

foreach (bool store in new[] { false, true })
{
    var method = new MethodDefinition(store ? "Write" : "Read", MethodAttributes.Public | MethodAttributes.Static,
        store ? module.TypeSystem.Void : value);
    value.Methods.Add(method);
    method.Parameters.Add(new ParameterDefinition(new ArrayType(value)));
    method.Parameters.Add(new ParameterDefinition(module.TypeSystem.Int32));
    if (store) method.Parameters.Add(new ParameterDefinition(value));
    var il = method.Body.GetILProcessor();
    il.Append(il.Create(OpCodes.Ldarg_0));
    il.Append(il.Create(OpCodes.Ldarg_1));
    if (store) il.Append(il.Create(OpCodes.Ldarg_2));
    il.Append(il.Create(store ? OpCodes.Stelem_Any : OpCodes.Ldelem_Any, value));
    il.Append(il.Create(OpCodes.Ret));
    var instruction = IlExtractor.Extract(method).Instructions.Single(item => item.OperandKind == IrInstruction.OpType);
    string expected = store ? "stelem" : "ldelem";
    if (instruction.Op != expected || !emitOpcodes.Contains(instruction.Op) || instruction.Type.Name != value.FullName)
        throw new InvalidOperationException($"배열 명령 정규화 실패: {instruction.Op}");
    Console.WriteLine($"PASS: {(store ? "쓰기" : "읽기")} {instruction.Op} / {instruction.Type.Name}");
}
Console.WriteLine("배열 opcode 회귀 검사 2/2 통과");
