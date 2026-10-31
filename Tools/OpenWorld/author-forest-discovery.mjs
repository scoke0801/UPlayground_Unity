// 수로 옆 목재 회수는 기존 채집/벌목·소비 저장 계약을 사용한다.
import fs from 'node:fs';
import assert from 'node:assert/strict';
export function authorForestDiscovery(r) {
  const props=r.landscape.props;
  const move=(id,x,z,yaw,scale)=>{
    const p=props.find(p=>p.id===id); assert(p,id);
    p.position={x,y:0,z}; p.yaw=yaw;
    if(scale) p.scale={x:scale,y:scale,z:scale};
    p.alignBounds=true;
  };
  // 목재 흔적을 본길의 갈림점부터 수로까지 이어 놓고 본길 폭은 유지한다.
  move('Expansion_log_792',1687,737,105,1.8);
  move('Explore_0617',1674,754,105,.65);
  move('Explore_0618',1637,765,150,.65);
  move('Explore_0619',1619,791,150,.65);
  for(const id of ['Explore_0617','Explore_0618','Explore_0619']){
    const p=props.find(p=>p.id===id);
    p.pitch=0; p.scale={x:.85,y:.15,z:.85};
  }
  // 넓은 수관은 유지하고 갈림점의 낮은 관목만 샛길 바깥 경계로 옮긴다.
  move('Explore_0014',1697,724,0);
  move('Explore_0036',1656,735,0);
  r.landscape.props=props.filter(p=>p.id!=='Expansion_log_791');
  r.landscape.gatherables=[{
    id:'c42bb9f480ec4f3cacdf786ab0a59e21',
    model:'Assets/ExternalAssets/Environment/PolyartStudio/DreamscapeEastLands/Prefabs/Props/WoodenProps/PF_EastLands_Log_01.prefab',
    interaction:'Assets/10.Datas/Actor/Interaction/ForestWaterTimber.asset',
    position:{x:1605,y:0,z:805},scale:{x:2,y:2,z:2},yaw:55,
    colliderSize:{x:2.4,y:1.2,z:2.4}
  }];
  r.routes.find(q=>q.id==='forest-water-loop').experience='목재 흔적을 따라 수로 옆 목재 회수 → 북동쪽 열린 사면 → 밝은 빈터로 자유 복귀';
  return r;
}
if(process.argv[1]?.endsWith('author-forest-discovery.mjs')){
  const p='Tools/OpenWorld/city-layout.json';
  fs.writeFileSync(p,JSON.stringify(authorForestDiscovery(JSON.parse(fs.readFileSync(p,'utf8'))),null,2)+'\n');
}
