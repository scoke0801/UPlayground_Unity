import fs from 'node:fs';

// 큰 적운의 수직 윤곽을 먼저 만들고 런타임에서는 별도 노이즈로 미세 침식한다.
const width = 256, height = 128, depth = 256;
const span = 24000, verticalSpan = 4200, distanceRange = 2000;
let seed = 78137;
const random = () => { seed = (Math.imul(seed, 1664525) + 1013904223) >>> 0; return seed / 4294967296; };
const range = (a, b) => a + (b - a) * random();
const delta = (a, b) => { const d = a - b; return d - Math.round(d / span) * span; };
const groups = [];
for (let attempt = 0; attempt < 5000 && groups.length < 28; attempt++) {
    const x = range(0, span), z = range(0, span);
    if (groups.some(g => Math.hypot(delta(x, g.x), delta(z, g.z)) < 2600)) continue;
    const tall = random() > 0.72;
    const growth = tall ? range(2600, 3650) : range(1100, 2200);
    const radius = range(850, 1900);
    const base = range(70, 210);
    const lean = range(0, Math.PI * 2);
    const lobes = [];
    const add = (px, py, pz, rx, ry, rz) => lobes.push({ x: px, y: py, z: pz, rx, ry, rz, r: Math.min(rx, ry, rz) });
    add(0, base + growth * 0.22, 0, radius, growth * 0.3, radius * range(0.7, 1.1));
    add(Math.cos(lean) * growth * 0.08, base + growth * 0.43, Math.sin(lean) * growth * 0.08,
        radius * range(0.78, 1.05), growth * 0.45, radius * range(0.7, 0.95));
    const crowns = Math.floor(range(12, 23));
    for (let lobe = 0; lobe < crowns; lobe++) {
        const angle = range(0, Math.PI * 2), spread = radius * range(0.55, 1.08);
        const y = base + growth * range(0.3, 0.82);
        const puff = radius * range(0.22, 0.5);
        add(Math.cos(angle) * spread + Math.cos(lean) * growth * 0.1, y,
            Math.sin(angle) * spread + Math.sin(lean) * growth * 0.1,
            puff * range(0.8, 1.25), Math.min(puff * range(0.8, 1.35), growth * 0.32), puff * range(0.8, 1.2));
    }
    groups.push({ x, z, base, growth, radius, selection: range(0.12, 0.9), lobes });
}
function sample(x, y, z) {
    let best = -distanceRange, chosen = null;
    for (const group of groups) {
        const dx = delta(x, group.x), dz = delta(z, group.z);
        if (Math.abs(dx) > group.radius * 1.8 + distanceRange || Math.abs(dz) > group.radius * 1.8 + distanceRange) continue;
        let field = -10000;
        for (const lobe of group.lobes) {
            const px = (dx - lobe.x) / lobe.rx;
            const py = (y - lobe.y) / lobe.ry;
            const pz = (dz - lobe.z) / lobe.rz;
            const sphere = (1 - Math.sqrt(px * px + py * py + pz * pz)) * lobe.r;
            const h = Math.max(180 - Math.abs(field - sphere), 0) / 180;
            field = Math.max(field, sphere) + h * h * 45;
        }
        field = Math.min(field, y - group.base);
        if (field > best) { best = field; chosen = group; }
    }
    return [Math.max(0, Math.min(1, 0.5 + best / (distanceRange * 2))), chosen?.selection ?? 1,
        chosen ? Math.min(1, (chosen.base + chosen.growth) / verticalSpan) : 0, 1];
}
const raw = Buffer.alloc(width * height * depth * 2);
let occupied = 0;
const occupiedByHeight = new Array(height).fill(0);
for (let z = 0; z < depth; z++) for (let y = 0; y < height; y++) for (let x = 0; x < width; x++) {
    const values = sample((x + 0.5) / width * span, (y + 0.5) / height * verticalSpan, (z + 0.5) / depth * span);
    const index = ((z * height + y) * width + x) * 2;
    values.slice(0, 2).forEach((v, c) => raw[index + c] = Math.round(v * 255));
    if (values[0] > 0.5) { occupied++; occupiedByHeight[y]++; }
}
// 주기 경계 양쪽은 같은 세계 위치로 취급해야 한다.
for (const y of [300, 1500, 3000]) {
    const a = sample(0, y, 7341), b = sample(span, y, 7341);
    if (Math.abs(a[0] - b[0]) > 1e-8) throw new Error('구름 군락의 순환 경계 불일치');
}
fs.mkdirSync('output/WeatherSky/CloudHeight', { recursive: true });
fs.writeFileSync('output/WeatherSky/CloudHeight/formations.rg', raw);
fs.writeFileSync('output/WeatherSky/CloudHeight/formations.json', JSON.stringify({ width, height, depth, span, verticalSpan,
    distanceRange, occupied, occupiedByHeight, groups }, null, 2));
console.log(JSON.stringify({ groups: groups.length, bytes: raw.length, occupied, maxGrowth: Math.max(...groups.map(g => g.growth)) }));
