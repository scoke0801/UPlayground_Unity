import fs from 'node:fs';
import assert from 'node:assert/strict';

// 실제 시간표의 SmoothStep 적분으로 10초·30초 이동을 계산한다. 화면 검수를 대신하지 않는다.
const schedule = JSON.parse(fs.readFileSync('Assets/10.Datas/World/LivingWorld/Environment/LakeOfLife_Weather_v1.json', 'utf8'));
function integrate(minute) {
  const cycles = Math.floor(minute / schedule.cycleMinutes);
  const phase = minute % schedule.cycleMinutes;
  const result = [0, 0];
  schedule.keys.forEach((key, index) => {
    const next = schedule.keys[(index + 1) % schedule.keys.length];
    const end = index === schedule.keys.length - 1 ? schedule.cycleMinutes : next.minute;
    const duration = end - key.minute;
    const u = Math.max(0, Math.min(1, (phase - key.minute) / duration));
    const integral = u ** 3 * (1 - u * 0.5);
    ['x', 'y'].forEach((axis, i) => {
      result[i] += duration * (key.wind[axis] * (cycles * 0.5 + u - integral)
        + next.wind[axis] * (cycles * 0.5 + integral));
    });
  });
  return result;
}
const records = [];
for (const startMinute of [0, 480, 540, 600, 720, 1080, 1435]) {
  for (const seconds of [10, 30]) {
    const a = integrate(startMinute), b = integrate(startMinute + seconds);
    const travel = [3 * seconds + 12 * (b[0] - a[0]), seconds + 12 * (b[1] - a[1])];
    for (const [region, scale] of [['LakeClear', 5000], ['CoastalHaze', 6500]]) {
      const before = travel.map(x => x / scale);
      const after = travel.map(x => x * 3 / 5000);
      records.push({ startMinute, seconds, region, before: {
        worldXZ: travel, worldDistance: Math.hypot(...travel), uvXZ: before, uvDistance: Math.hypot(...before)
      }, after: {
        worldXZ: after.map(x => x * scale), worldDistance: Math.hypot(...after) * scale,
        uvXZ: after, uvDistance: Math.hypot(...after),
        highWorldDistance: Math.hypot(...after) * scale * 0.45 / 0.65,
        highUVDistance: Math.hypot(...after) * 0.45
      } });
    }
  }
}
assert.ok(Math.abs(records.find(x => x.startMinute === 1080 && x.seconds === 10).before.worldDistance - Math.hypot(44.4, 19.6)) < 1e-8);
const output = { basis: '정상 속도 1게임 분/실제 초, CloudVelocity=(3,1), weatherCloudSpeed=12. 월드 거리는 투영면 환산 거리. 실루엣 이동 방향은 UV 가산의 반대.', records };
fs.mkdirSync('output/RegionalSky/CloudFlow', { recursive: true });
fs.writeFileSync('output/RegionalSky/CloudFlow/movement.json', JSON.stringify(output, null, 2));
console.log(JSON.stringify(records.filter(x => x.startMinute === 1080), null, 2));
