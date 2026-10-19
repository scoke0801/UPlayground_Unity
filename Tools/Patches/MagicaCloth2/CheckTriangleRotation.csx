// Unity MCP execute_code의 메서드 본문으로 실행한다. 씬과 에셋을 수정하지 않는다.
var method=typeof(MagicaCloth2.VirtualMeshManager).GetMethod("SimulationPostProxyMeshUpdateTriangleSum",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
var normals=new Unity.Collections.NativeArray<Unity.Mathematics.float3>(1,Unity.Collections.Allocator.TempJob);
var tangents=new Unity.Collections.NativeArray<Unity.Mathematics.float3>(1,Unity.Collections.Allocator.TempJob);
var rotations=new Unity.Collections.NativeArray<Unity.Mathematics.quaternion>(1,Unity.Collections.Allocator.TempJob);
var adjustments=new Unity.Collections.NativeArray<Unity.Mathematics.quaternion>(1,Unity.Collections.Allocator.TempJob);
var links=new Unity.Collections.NativeArray<Unity.Collections.FixedList32Bytes<uint>>(1,Unity.Collections.Allocator.TempJob);
var results=new System.Collections.Generic.List<object>();
try{
var link=new Unity.Collections.FixedList32Bytes<uint>();uint triangle=0;link.Add(ref triangle);links[0]=link;
adjustments[0]=Unity.Mathematics.quaternion.identity;
var team=new MagicaCloth2.TeamManager.TeamData{proxyCommonChunk=new MagicaCloth2.DataChunk(0,1),proxyTriangleChunk=new MagicaCloth2.DataChunk(0,1),negativeScaleQuaternionValue=new Unity.Mathematics.float4(1)};
var names=new[]{"parallel","opposite","near_parallel","zero","infinite","valid"};
var n=new Unity.Mathematics.float3(1,1,1);
var ns=new[]{n,n,n,new Unity.Mathematics.float3(0),new Unity.Mathematics.float3(float.PositiveInfinity,1,1),new Unity.Mathematics.float3(0,1,0)};
var ts=new[]{n,-n,new Unity.Mathematics.float3(1,1,1.0000001f),n,n,new Unity.Mathematics.float3(0,0,1)};
for(int i=0;i<names.Length;i++){
normals[0]=ns[i];tangents[0]=ts[i];var original=Unity.Mathematics.quaternion.RotateY(0.4f);rotations[0]=original;
method.Invoke(null,new object[]{new MagicaCloth2.DataChunk(0,1),team,rotations,normals,tangents,links,adjustments});
var q=rotations[0];bool finite=Unity.Mathematics.math.all(Unity.Mathematics.math.isfinite(q.value));bool unchanged=Unity.Mathematics.math.all(q.value==original.value);
bool passed=finite&&(i==5?Unity.Mathematics.math.abs(Unity.Mathematics.math.dot(q.value,q.value)-1)<0.0001f:unchanged);
if(!passed)throw new System.InvalidOperationException("회전 퇴화 검사 실패: "+names[i]);
results.Add(new{name=names[i],finite=finite,unchanged=unchanged,validOutput=passed});
}
return results;
}finally{normals.Dispose();tangents.Dispose();rotations.Dispose();adjustments.Dispose();links.Dispose();}
