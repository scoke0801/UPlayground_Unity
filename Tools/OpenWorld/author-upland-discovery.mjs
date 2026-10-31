// 전망은 아이템 없이 지리와 귀환 동선을 알아보는 공간 탐색으로 남긴다.
import fs from 'node:fs';
import assert from 'node:assert/strict';

export function authorUplandDiscovery(recipe) {
  const props=recipe.landscape.props;
  const move=(id,x,z,yaw,scale)=>{
    const prop=props.find(p=>p.id===id);assert(prop,id);
    prop.position={x,y:0,z};prop.yaw=yaw;
    if(scale)prop.scale={x:scale,y:scale,z:scale};
    prop.alignBounds=true;
  };
  const loop=recipe.routes.find(q=>q.id==='upland-lake-loop');assert(loop);
  // 기존 남향 사면을 따라 내려가야 호수가 열리고, 동쪽 사면으로 본길에 돌아온다.
  loop.points=[[1040,190,1410],[1080,185,1370],[1145,176,1300],[1225,173,1265],
    [1310,176,1310],[1400,181,1365],[1360,190,1420]].map(([x,y,z])=>({x,y,z}));
  loop.experience='초지의 낮은 목책과 바람막이 수목 → 남향 사면 선택 → 낮은 어깨에서 호숫가 발견 → 전망에서 호수·생활권 위치 확인 → 동쪽 오르막으로 본길 복귀';
  // 생활 흔적은 분기와 귀환의 식별점이다. 연속 난간이나 통행 차단으로 쓰지 않는다.
  move('Explore_0630',1062,1398,135,1);
  move('Explore_0631',1380,1396,145,1);
  move('Explore_0579',1074,1401,30,1.2);
  const tree=props.find(p=>p.id==='Explore_0579');
  tree.alignBounds=false;tree.groundOffset=-.07;
  move('Explore_0582',1084,1397,70,1.1);
  move('Explore_0624',1090,1358,95,1.6);
  move('Explore_0625',1208,1250,20,1.6);
  move('Explore_0626',1328,1292,55,1.6);
  // 하강로의 바깥 어깨만 정리하고 기존 수관 군락과 넓은 이동면은 보존한다.
  move('Expansion_rock_345',1484,1314,100,2.6);
  move('Expansion_rock_353',1571,1201,80,2.8);
  for(const id of ['Explore_0624','Explore_0625','Explore_0626','Expansion_rock_345','Expansion_rock_353'])
    props.find(p=>p.id===id).slopeEmbedDepth=.5;
  const work=recipe.explorationWorkUnits.find(w=>w.id===4);assert(work);
  work.implementation='남향 사면의 선택 탐색 → 호숫가 공간 발견 → 호수·생활권 조망 → 동쪽 오르막 귀환; 기존 동쪽 숲 하강은 독립 선택';
  work.validation='최신 근거: output/OpenWorld/Exploration/Iteration05/RESULT.md';
  work.remaining='실제 입력 발견·조망 가독성·낙하 경계·파일 저장 재시작 사용자 인수';
  return recipe;
}

if(process.argv[1]?.endsWith('author-upland-discovery.mjs')) {
  const path=process.argv[2]||'Tools/OpenWorld/city-layout.json';
  fs.writeFileSync(path,JSON.stringify(authorUplandDiscovery(JSON.parse(fs.readFileSync(path,'utf8'))),null,2)+'\n');
}
