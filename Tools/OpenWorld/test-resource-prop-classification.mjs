import fs from 'node:fs';
import vm from 'node:vm';
import assert from 'node:assert/strict';

// 실제 저작 스크립트를 실행하되 입출력을 메모리에 가둬 기존 계획을 보존한다.
const names=['Bakery_Worktable','Bakery_Flour','Bakery_Bread','Windmill_Sack_0','Windmill_Sack_1','Windmill_Sack_2','Village_Bakery','Village_Windmill'];
const input={units:names.map((name,i)=>({name,path:'Village/'+name,prefab:'',mesh:'',active:true,instanceId:i}))};
let result;
const source=fs.readFileSync('Tools/OpenWorld/plan-resource-overhaul.mjs','utf8').replace(/^import .*;\r?\n/gm,'');
vm.runInNewContext(source,{fs:{readFileSync:()=>JSON.stringify(input),existsSync:()=>true,writeFileSync:(p,s)=>{if(p.endsWith('resource-overhaul-plan.json'))result=JSON.parse(s);}},console:{log(){}}});
const suffixes=['TGV_Table_01A.prefab','TGV_Flour_01A.prefab','TGV_Bread_01A.prefab','TGV_Sack_01A.prefab','TGV_Sack_01A.prefab','TGV_Sack_01A.prefab','PoorHouse_Chogajip.prefab','PoorHouse_Chogajip.prefab'];
for(let i=0;i<names.length;i++){
 assert(result.units[i].target.endsWith(suffixes[i]),names[i]);
 assert.equal(result.units[i].mode,i<6?'prop':'building',names[i]);
 assert.equal(result.units[i].action,'replace');
}
console.log('PASS: 생활 소품 6개와 실제 건물 2개 분류');
