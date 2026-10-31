// 기존 운송 쉼터를 본길에서 읽히는 짧은 발견 고리로 재저작한다.
import fs from 'node:fs';
import assert from 'node:assert/strict';

export function authorTradeDiscovery(r) {
  const L = r.landscape;
  const route = r.routes.find(q => q.id === 'trade-shelter-loop');
  assert(route, '기존 쉼터 동선 필요');
  route.points = [[800,64,220],[824,64,202],[842,64,185],[862,63,178],[900,62,190],[950,61,195],[1000,59,180]]
    .map(([x,y,z]) => ({x,y,z}));
  route.experience = '본길의 수레 실루엣 → 열린 북쪽 진입 / 동쪽 완만한 우회 → 짐 회수 → 동쪽 출구로 교역로 합류';
  // 기존 네 소품의 ID를 유지하며 북쪽 진입과 남쪽 작업 경계를 분리한다.
  const find = name => {
    const p = L.props.find(p => p.prefab.includes(name) && p.position.x > 840 && p.position.x < 865 && p.position.z < 200);
    assert(p, `운송 쉼터 소품 누락: ${name}`);
    return p;
  };
  const cart = find('Carriage_01');
  const log = find('Log_02');
  const fences = L.props.filter(p => p.prefab.includes('WoodenFence_01') && p.position.x >= 835 && p.position.x <= 850 && p.position.z >= 160 && p.position.z <= 178);
  assert.equal(fences.length, 2);
  cart.position = {x:844,y:0,z:192}; cart.yaw = 100;
  log.position = {x:850,y:0,z:175}; log.yaw = 90;
  fences[0].position = {x:836,y:0,z:177}; fences[0].yaw = 90;
  fences[1].position = {x:840,y:0,z:175}; fences[1].yaw = 90;
  const pickup = L.pickups.find(p => p.id === 'c42bb9f480ec4f3cacdf786ab0a59e04');
  assert(pickup, '기존 보상 ID 필요');
  pickup.position = {x:846,y:0,z:184};
  // 짐 옆 수관이 활동 공간과 복귀 시야를 가려 남서쪽 군락 경계로 옮긴다.
  const bush = L.props.find(p => p.id === 'Explore_0424');
  assert(bush, '기존 쉼터 관목 필요');
  bush.position = {x:815,y:0,z:158};
  // 수레보다 낮은 짐은 접근 후에 보이고, 회수 후에는 시각적으로 사라진다.
  assert.equal(pickup.count, 1);
  return r;
}

if (process.argv[1]?.endsWith('author-trade-discovery.mjs')) {
  const path = 'Tools/OpenWorld/city-layout.json';
  const r = JSON.parse(fs.readFileSync(path, 'utf8'));
  // 반복 실행은 변경 결과를 그대로 유지한다.
  authorTradeDiscovery(r);
  fs.writeFileSync(path, JSON.stringify(r,null,2)+'\n');
}
