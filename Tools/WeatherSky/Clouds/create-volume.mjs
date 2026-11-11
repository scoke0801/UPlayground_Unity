import fs from 'node:fs';

// 주기적인 FBM·Worley 볼륨을 미리 계산해 프레임마다 절차 노이즈를 만들지 않는다.
const size = 128;
const hash = (x, y, z, seed) => {
    let h = (Math.imul(x, 374761393) + Math.imul(y, 668265263) + Math.imul(z, 1442695041) + seed) >>> 0;
    h = Math.imul(h ^ h >>> 13, 1274126177) >>> 0;
    return ((h ^ h >>> 16) >>> 0) / 4294967295;
};
const wrap = (x, p) => (x % p + p) % p;
const lerp = (a, b, t) => a + (b - a) * t;
function perlinNoise(x, y, z, period) {
    x *= period; y *= period; z *= period;
    const ix = Math.floor(x), iy = Math.floor(y), iz = Math.floor(z);
    let fx = x - ix, fy = y - iy, fz = z - iz;
    const smooth = t => t * t * t * (t * (t * 6 - 15) + 10);
    const at = (dx, dy, dz) => {
        const h = Math.floor(hash(wrap(ix + dx, period), wrap(iy + dy, period), wrap(iz + dz, period), 5913) * 256) & 15;
        const px = fx - dx, py = fy - dy, pz = fz - dz;
        const u = h < 8 ? px : py;
        const v = h < 4 ? py : h === 12 || h === 14 ? px : pz;
        return ((h & 1) ? -u : u) + ((h & 2) ? -v : v);
    };
    return lerp(lerp(lerp(at(0, 0, 0), at(1, 0, 0), smooth(fx)), lerp(at(0, 1, 0), at(1, 1, 0), smooth(fx)), smooth(fy)),
        lerp(lerp(at(0, 0, 1), at(1, 0, 1), smooth(fx)), lerp(at(0, 1, 1), at(1, 1, 1), smooth(fx)), smooth(fy)), smooth(fz));
}
function makeCells(period) {
    const data = new Float32Array(period ** 3 * 3);
    for (let z = 0; z < period; z++) for (let y = 0; y < period; y++) for (let x = 0; x < period; x++) {
        const i = ((z * period + y) * period + x) * 3;
        data[i] = hash(x, y, z, 71); data[i + 1] = hash(x, y, z, 1237); data[i + 2] = hash(x, y, z, 9157);
    }
    return { period, data };
}
const grids = [4, 8, 16].map(makeCells);
function worley(x, y, z, grid) {
    const p = grid.period;
    x *= p; y *= p; z *= p;
    const ix = Math.floor(x), iy = Math.floor(y), iz = Math.floor(z);
    let best = 4;
    for (let dz = -1; dz <= 1; dz++) for (let dy = -1; dy <= 1; dy++) for (let dx = -1; dx <= 1; dx++) {
        const i = ((wrap(iz + dz, p) * p + wrap(iy + dy, p)) * p + wrap(ix + dx, p)) * 3;
        const vx = ix + dx + grid.data[i] - x;
        const vy = iy + dy + grid.data[i + 1] - y;
        const vz = iz + dz + grid.data[i + 2] - z;
        best = Math.min(best, vx * vx + vy * vy + vz * vz);
    }
    return Math.max(0, 1 - Math.sqrt(best));
}
const raw = Buffer.alloc(size ** 3 * 4);
const ranges = [[1, 0], [1, 0], [1, 0], [1, 0]];
for (let z = 0; z < size; z++) for (let y = 0; y < size; y++) for (let x = 0; x < size; x++) {
    const u = (x + 0.5) / size, v = (y + 0.5) / size, w = (z + 0.5) / size;
    const fbm = 0.5 + (perlinNoise(u, v, w, 4) * 0.6 + perlinNoise(u, v, w, 8) * 0.28 + perlinNoise(u, v, w, 16) * 0.12) * 0.8;
    const low = worley(u, v, w, grids[0]);
    const values = [fbm, low, worley(u, v, w, grids[1]), worley(u, v, w, grids[2])];
    const index = ((z * size + y) * size + x) * 4;
    values.forEach((value, channel) => {
        ranges[channel][0] = Math.min(ranges[channel][0], value);
        ranges[channel][1] = Math.max(ranges[channel][1], value);
        raw[index + channel] = Math.round(Math.max(0, Math.min(1, value)) * 255);
    });
}
fs.mkdirSync('output/WeatherSky/Clouds', { recursive: true });
fs.writeFileSync('output/WeatherSky/Clouds/cloud-volume.rgba', raw);
fs.writeFileSync('output/WeatherSky/Clouds/volume-generation.json', JSON.stringify({ size, bytes: raw.length, ranges }, null, 2));
console.log(JSON.stringify({ size, bytes: raw.length, ranges }));
