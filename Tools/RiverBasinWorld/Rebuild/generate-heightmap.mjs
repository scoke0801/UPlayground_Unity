import assert from 'node:assert/strict';
import { createHash } from 'node:crypto';
import { readFileSync, writeFileSync } from 'node:fs';
import { deflateSync, crc32 } from 'node:zlib';

const file = (name) => new URL(name, import.meta.url);
const production = process.argv[2] === '--production';
const refined = production && process.argv.includes('--refined');
const bankFillet = refined && process.argv.includes('--bank-fillet');
const joinApproaches = bankFillet && process.argv.includes('--join-approaches');
const plan = JSON.parse(readFileSync(file(production ? 'terrain-plan-4km.json' : 'terrain-plan.json'), 'utf8'));
const { worldSize, originY, verticalRange, seed, lake } = plan;
const mapScale = worldSize / 2048;
const westernLakes = plan.westernLakes;
const lakes = [lake, ...westernLakes];
const size = worldSize + 1;
const pixels = size * size;
const clamp = (t) => Math.max(0, Math.min(1, t));
const mix = (a, b, t) => a + (b - a) * t;
function ramp(a, b, x) {
    const t = clamp((x - a) / (b - a));
    return t * t * (3 - 2 * t);
}

function lattice(x, z) {
    let h = Math.imul(x, 374761393) + Math.imul(z, 668265263) + seed;
    h = Math.imul(h ^ (h >>> 13), 1274126177);
    return ((h ^ (h >>> 16)) >>> 0) / 2147483648 - 1;
}

function noise(x, z, scale) {
    const ax = Math.floor(x / scale), az = Math.floor(z / scale);
    const sx = ramp(0, 1, x / scale - ax), sz = ramp(0, 1, z / scale - az);
    return mix(mix(lattice(ax, az), lattice(ax + 1, az), sx),
        mix(lattice(ax, az + 1), lattice(ax + 1, az + 1), sx), sz);
}

function spline(points) {
    const path = [points[0]];
    for (let i = 0; i < points.length - 1; i++) {
        const a = points[Math.max(0, i - 1)], b = points[i];
        const c = points[i + 1], d = points[Math.min(points.length - 1, i + 2)];
        const steps = Math.ceil(Math.hypot(c[0] - b[0], c[1] - b[1]) / 8);
        for (let j = 1; j <= steps; j++) {
            const t = j / steps;
            const point = [0, 1].map((axis) => {
                const [p, q, r, s] = [a[axis], b[axis], c[axis], d[axis]];
                return q + 0.5 * t * (r - p + t * (2 * p - 5 * q + 4 * r - s + t * (3 * q - 3 * r + s - p)));
            });
            path.push([...point, mix(b[2], c[2], t)]);
        }
    }
    return path;
}

function rasterize(definitions, radiusFor, blendJunctions = false) {
    const distance2 = new Float32Array(pixels).fill(Infinity);
    const level = new Float32Array(pixels);
    const width = blendJunctions ? new Float32Array(pixels) : new Uint8Array(pixels);
    const influence = blendJunctions ? new Float32Array(pixels) : new Uint8Array(pixels);
    const owner = blendJunctions ? new Uint8Array(pixels).fill(255) : null;
    const alternate = blendJunctions ? {
        distance2: new Float32Array(pixels).fill(Infinity), level: new Float32Array(pixels),
        width: new Uint8Array(pixels), influence: new Uint8Array(pixels)
    } : null;
    for (const [course, definition] of definitions.entries()) {
        const radius = radiusFor(definition);
        const path = spline(definition.points);
        const cap = bankFillet && definition.id === 'southwest-inlet' ? path[0] : null;
        const capDx = cap ? definition.points[1][0] - cap[0] : 0;
        const capDz = cap ? definition.points[1][1] - cap[1] : 0;
        const capLength = Math.hypot(capDx, capDz);
        for (let i = 1; i < path.length; i++) {
            const [ax, az, ay] = path[i - 1], [bx, bz, by] = path[i];
            const dx = bx - ax, dz = bz - az, length2 = dx * dx + dz * dz;
            const minX = Math.max(0, Math.floor(Math.min(ax, bx) - radius));
            const maxX = Math.min(worldSize, Math.ceil(Math.max(ax, bx) + radius));
            const minZ = Math.max(0, Math.floor(Math.min(az, bz) - radius));
            const maxZ = Math.min(worldSize, Math.ceil(Math.max(az, bz) + radius));
            for (let z = minZ; z <= maxZ; z++) {
                for (let x = minX; x <= maxX; x++) {
                    if (cap && ((x - cap[0]) * capDx + (z - cap[1]) * capDz) < -4 * capLength) continue;
                    const t = clamp(((x - ax) * dx + (z - az) * dz) / length2);
                    const offsetX = x - ax - t * dx, offsetZ = z - az - t * dz;
                    const d2 = offsetX * offsetX + offsetZ * offsetZ;
                    const index = z * size + x;
                    if (d2 > radius * radius) continue;
                    if (d2 >= distance2[index]) {
                        if (alternate && owner[index] !== course && d2 < alternate.distance2[index]) {
                            alternate.distance2[index] = d2;
                            alternate.level[index] = mix(ay, by, t);
                            alternate.width[index] = definition.width;
                            alternate.influence[index] = radius;
                        }
                        continue;
                    }
                    if (alternate && owner[index] !== course) {
                        alternate.distance2[index] = distance2[index];alternate.level[index] = level[index];
                        alternate.width[index] = width[index];alternate.influence[index] = influence[index];
                        owner[index] = course;
                    }
                    distance2[index] = d2;
                    level[index] = mix(ay, by, t);
                    width[index] = definition.width ?? 0;
                    influence[index] = radius;
                }
            }
        }
    }
    if (alternate) {
        for (let i = 0; i < pixels; i++) {
            if (alternate.distance2[i] === Infinity || Math.abs(level[i] - alternate.level[i]) > 8) continue;
            const nearest = Math.sqrt(distance2[i]), next = Math.sqrt(alternate.distance2[i]);
            const shoulder = 22 * (1 - ramp(27, 82, nearest));
            const blend = 0.5 * (1 - ramp(0, Math.max(0.01, shoulder), next - nearest));
            level[i] = mix(level[i], alternate.level[i], blend);
            width[i] = mix(width[i], alternate.width[i], blend);
            influence[i] = mix(influence[i], alternate.influence[i], blend);
        }
    }
    return { distance2, level, width, influence };
}

const river = rasterize(plan.watercourses, (course) => course.valley, refined);
const roadRadius = production ? 112 : 78;
const roads = plan.routes.map((route) => rasterize([route], () => roadRadius));
const masses = plan.relief.map(([x, z, rx, rz, angle, height]) =>
    ({ x, z, rx, rz, cos: Math.cos(angle), sin: Math.sin(angle), height }));

function landform(x, z) {
    if (production) return productionLandform(x, z);
    const warpX = x + 54 * noise(x + 281, z, 236);
    const warpZ = z + 49 * noise(x, z + 491, 208);
    const coast = 90 + 32 * Math.sin(x / 257) + 25 * noise(x, 977, 192);
    let height = mix(-13, 31 + 0.012 * z, ramp(-62, 198, z - coast));
    let relief = 0;
    for (const mass of masses) {
        const dx = warpX - mass.x, dz = warpZ - mass.z;
        const u = (mass.cos * dx + mass.sin * dz) / mass.rx;
        const v = (-mass.sin * dx + mass.cos * dz) / mass.rz;
        const contribution = mass.height * Math.exp(-0.5 * (u * u + v * v));
        const square = contribution * contribution;
        relief += square * square;
    }
    relief = Math.sqrt(Math.sqrt(relief));
    height += relief;
    const rugged = ramp(24, 144, relief);
    height += noise(x, z, 128) * (5 + 15 * rugged) + noise(x, z, 53) * (2 + 8 * rugged);
    height += noise(x, z, 21) * (0.6 + 3.1 * rugged) + noise(x, z, 8) * (0.16 + 0.69 * rugged);
    if (x > 1460 && z > 760) {
        const strata = Math.sin((height + 5 * noise(x, z, 120)) * 0.21);
        height += 2.2 * strata * ramp(1460, 1750, x) * ramp(58, 115, height);
    }
    return height;
}

function productionLandform(x, z) {
    const warpX = x + 135 * noise(x + 721, z - 319, 540) + 27 * noise(x, z, 104);
    const warpZ = z + 101 * noise(x - 457, z + 214, 470) + 31 * noise(x - 285, z, 126);
    const coast = 163 + 57 * Math.sin(x / 462) + 59 * noise(x, 1740, 411);
    const lowland = mix(-18, 31 + z * 0.006, ramp(-128, 354, z - coast));
    let massSum = 0;
    for (const mass of masses) {
        const dx = warpX - mass.x, dz = warpZ - mass.z;
        const u = (mass.cos * dx + mass.sin * dz) / mass.rx;
        const v = (-mass.sin * dx + mass.cos * dz) / mass.rz;
        const distance = Math.hypot(u, v);
        if (distance > 4) continue;
        // 뾰족한 윗단과 넓은 산기슭에 서로 다른 감쇠를 사용한다.
        const sharpness = ramp(160, 530, mass.height);
        const foot = Math.exp(-0.51 * distance * distance);
        const summit = Math.exp(-0.91 * Math.pow(distance, 1.26));
        const value = mass.height * mix(foot, summit, sharpness * 0.66);
        const square = value * value;
        massSum += square * square;
    }
    const mass = Math.sqrt(Math.sqrt(massSum));
    const upland = ramp(90, 370, mass);
    const folds = noise(warpX + warpZ * 0.43 + 222, warpZ * 0.62 + 897, 140);
    const drainage = noise(warpX - warpZ * 0.28 - 608, warpZ * 0.55 + 350, 110);
    let height = lowland + mass;
    height += (folds * 28 - 35 * ramp(0.20, 0.81, drainage)) * upland;
    height += noise(warpX, warpZ, 245) * (12 + 19 * upland);
    height += noise(x + 561, z, 84) * (4 + 12 * upland);
    height += noise(x, z + 444, 33) * (1.7 + 7 * upland);
    height += noise(x - 142, z, 12) * (0.45 + 2.8 * upland);
    const canyon = ramp(2990, 3640, x) * ramp(1500, 2370, z) * (1 - ramp(2820, 3210, z));
    const strata = (height + 11 * noise(x, z, 207)) / 11;
    height += Math.sin(strata * 2.1) * 3.4 * canyon * ramp(80, 180, height);
    return height;
}

function lakeDistance(x, z, basin = lake) {
    const u = (x - basin.x) / basin.radiusX, v = (z - basin.z) / basin.radiusZ;
    if (Math.abs(u) > 1.8 || Math.abs(v) > 1.8) return Infinity;
    const angle = Math.atan2(v, u);
    const shore = 1 + 0.067 * Math.cos(3 * angle + 0.5) + 0.044 * Math.sin(5 * angle + 0.8)
        + 0.026 * Math.cos(9 * angle - 0.7);
    const coves = production ? 0.029 * Math.sin(14 * angle + u * 2) + 0.016 * Math.cos(23 * angle + 1.3) : 0;
    return (Math.hypot(u, v) - shore - coves) * basin.radiusZ;
}

function carveRiver(x, z, height, index, distance, width) {
    const surface = river.level[index];
    const widthScale = 1 + 0.30 * noise(x + 709, z - 337, 53) + 0.12 * noise(x, z, 17);
    const depth = 2.65 + 0.75 * noise(x - 803, z + 419, 49);
    const shelf = width * widthScale;
    const bank = surface - depth + (depth + 1.4) * ramp(0.08, 1.25, distance / shelf);
    const roughness = (0.9 * noise(x, z, 21) + 0.37 * noise(x + 219, z - 142, 8))
        * ramp(0.42, 1.3, distance / shelf);
    const bankBlend = ramp(shelf * 0.96, river.influence[index], distance);
    return mix(bank + roughness, height, bankBlend);
}

const raw = Buffer.alloc(pixels * 2);
let min = Infinity, max = -Infinity;
for (let z = 0; z < size; z++) {
    for (let x = 0; x < size; x++) {
        const index = z * size + x;
        let height = landform(x, z);
        const lakeshore = Math.abs(x - lake.x) < 340 * mapScale && Math.abs(z - lake.z) < 290 * mapScale
            ? lakeDistance(x, z) : Infinity;
        let westernShore = Infinity;
        const channelDistance = Math.sqrt(river.distance2[index]);
        const channelWidth = river.width[index];
        if (channelDistance < river.influence[index]) height = carveRiver(x, z, height, index, channelDistance, channelWidth);
        if (lakeshore < 36 * mapScale) {
            const shoreHeight = lake.surface + 0.24 * lakeshore;
            const lakebed = Math.max(lake.surface - 14, shoreHeight);
            const shorelineWeight = 1 - ramp(1, 36 * mapScale, lakeshore);
            height = mix(height, Math.min(lakebed, lakeshore > 0 && channelDistance < channelWidth ? height : lakebed), shorelineWeight);
        }
        for (const basin of westernLakes) {
            const shore = lakeDistance(x, z, basin);
            westernShore = Math.min(westernShore, shore);
            if (shore >= basin.bank) continue;
            const level = basin.surface + Math.max(-basin.depth, shore * 0.16);
            const inlet = channelDistance < channelWidth && shore >= 0;
            height = mix(height, inlet ? Math.min(level, height) : level, 1 - ramp(0, basin.bank, shore));
        }
        const groundBeforeRoad = height;
        let bankWeight = 0, roadTarget = 0, totalPreference = 0, nearestRoad = Infinity;
        for (const road of roads) {
            if (road.distance2[index] >= roadRadius ** 2) continue;
            const distance = Math.sqrt(road.distance2[index]);
            const weight = 1 - ramp(production ? 5 : 3, roadRadius, distance);
            const preference = weight * weight / (1 + road.distance2[index] / 16);
            bankWeight = refined ? bankWeight + (1 - bankWeight) * weight : Math.max(bankWeight, weight);
            nearestRoad = Math.min(nearestRoad, distance);
            roadTarget += (road.level[index] - Math.min(distance, 3) * 0.022) * preference;
            totalPreference += preference;
        }
        if (totalPreference > 0) {
            const riverClearance = ramp(channelWidth * 0.55, channelWidth + (production ? 4 : 10), channelDistance);
            const bridgeFeather = joinApproaches ? 1 : plan.bridges.reduce((weight, bridge) =>
                Math.min(weight, ramp(bridge.gap, bridge.gap + (production ? 5 : 8), Math.hypot(x - bridge.x, z - bridge.z))), 1);
            const naturalShoulder = refined
                ? 1 - ramp(9, 64, nearestRoad) * (0.19 + 0.37 * (1 - ramp(18, 78, channelDistance)))
                : 1;
            const weight = naturalShoulder * bankWeight * riverClearance * bridgeFeather * ramp(0, 24, lakeshore) * ramp(0, 8, westernShore);
            height = mix(height, roadTarget / totalPreference, weight);
        }
        if (bankFillet && channelDistance < 62) {
            // 상판 아래에는 자연 하상을 유지하고, 양안 접속부의 좁은 도로 중앙은 보호한다.
            let fillet = 0;
            for (const bridge of plan.bridges) {
                const distance = Math.hypot(x - bridge.x, z - bridge.z);
                fillet = Math.max(fillet, 1 - ramp(48, 91, distance));
            }
            const shoulder = ramp(5, 28, nearestRoad) * (1 - ramp(24, 62, channelDistance));
            height = mix(height, groundBeforeRoad, fillet * shoulder);
        }
        for (const { position: [px, pz, py], radii: [rx, rz] } of plan.landmarks) {
            if (Math.abs(x - px) >= rx || Math.abs(z - pz) >= rz) continue;
            const radius = Math.hypot((x - px) / rx, (z - pz) / rz);
            height = mix(height, py, 1 - ramp(0.16, 1, radius));
        }
        // 합류부에서 호안이나 도로가 좁은 수로를 막지 않도록 중앙 하상을 보존한다.
        if (channelDistance < channelWidth * 0.55) {
            const water = river.level[index];
            const blend = 1 - ramp(channelWidth * 0.32, channelWidth * 0.55, channelDistance);
            height = mix(height, Math.min(height, water - 1.3), blend);
        }
        min = Math.min(min, height);
        max = Math.max(max, height);
        raw.writeUInt16LE(Math.round(clamp((height - originY) / verticalRange) * 65535), index * 2);
    }
}
assert(min >= originY && max <= originY + verticalRange, '높이 범위 초과');

function heightAt(x, z) {
    return originY + raw.readUInt16LE((z * size + x) * 2) / 65535 * verticalRange;
}

function surfaceAt(x, z, reach = 1) {
    for (const basin of lakes) {
        if (lakeDistance(x, z, basin) <= 0) return basin.surface;
    }
    const index = z * size + x;
    if (river.distance2[index] < (river.width[index] * reach) ** 2) return river.level[index];
    return z < 240 * mapScale ? 0 : -Infinity;
}

const bridges = plan.bridges.map(({ id, x, z, water, deck }) => {
    const bed = heightAt(x, z);
    assert(bed < water - 1 && deck > water + 3, id + ' 하상·상판 높이 충돌');
    return { id, bed: +bed.toFixed(2), water, deck };
});
const westernWater = westernLakes.map(({ id, x, z, surface, depth }) => {
    const bed = heightAt(x, z);
    assert(Math.abs(surface - bed - depth) < 0.01, id + ' 중앙 수심 불일치');
    return { id, x, z, surface, depth };
});
for (const { id, points } of plan.watercourses) {
    assert(points.every((point, index) => index === 0 || point[2] <= points[index - 1][2]), id + ' 수위 역전');
}
const watercourseStats = plan.watercourses.map(({ id, points }) => {
    const probes = spline(points);
    const dry = probes.filter(([x, z]) => {
        const ix = Math.round(x), iz = Math.round(z);
        return heightAt(ix, iz) >= surfaceAt(ix, iz, 0.64);
    });
    assert.equal(dry.length, 0, id + ' 중심 물길 단절 ' + JSON.stringify(dry));
    return { id, probes: probes.length, dry: dry.length };
});
for (const { id, position: [x, z, y] } of plan.landmarks) {
    assert(Math.abs(heightAt(x, z) - y) < 0.01, id + ' 거점 고도 불일치');
}
const routeStats = plan.routes.map(({ id, points }) => {
    const path = spline(points);
    let probes = 0;
    const flooded = [];
    let maxRoadError = 0;
    let steepest = null;
    for (let i = 1; i < path.length; i++) {
        const [x, z, designY] = path[i], [px, pz] = path[i - 1];
        if (plan.bridges.some((bridge) => Math.hypot(x - bridge.x, z - bridge.z) < 32 ||
            Math.hypot(px - bridge.x, pz - bridge.z) < 32)) continue;
        const xi = Math.round(x), zi = Math.round(z);
        const current = heightAt(xi, zi), before = heightAt(Math.round(px), Math.round(pz));
        const angle = Math.atan(Math.abs(current - before) / Math.hypot(x - px, z - pz)) * 180 / Math.PI;
        if (!steepest || angle > steepest.grade) steepest = { x: xi, z: zi, grade: angle };
        probes++;
        maxRoadError = Math.max(maxRoadError, Math.abs(current - designY));
        const index = zi * size + xi;
        if ((river.distance2[index] < river.width[index] ** 2 && current < river.level[index]) ||
            (lakes.some((basin) => lakeDistance(x, z, basin) < 0 && current < basin.surface))) flooded.push([xi, zi]);
    }
    assert.equal(flooded.length, 0, id + ' 교량 구간 외 침수 ' + JSON.stringify(flooded));
    return { id, probes, belowWater: flooded.length, maxGradeDeg: +steepest.grade.toFixed(2),
        steepestAt: [steepest.x, steepest.z],
        maxRoadError: +maxRoadError.toFixed(2) };
});

const sightlines = plan.sightlines.map(({ id, from: [x, z], target: [tx, tz, ty] }) => {
    const eyeHeight = heightAt(x, z) + 2;
    let obstruction = -Infinity;
    for (let i = 3; i <= 96; i++) {
        const t = i / 100;
        const ground = heightAt(Math.round(mix(x, tx, t)), Math.round(mix(z, tz, t)));
        obstruction = Math.max(obstruction, ground - mix(eyeHeight, ty, t));
    }
    return { id, terrainOccludes: obstruction >= 0, maxObstructionMeters: +obstruction.toFixed(2) };
});

function pngChunk(name, data) {
    const body = Buffer.concat([Buffer.from(name), data]);
    const result = Buffer.alloc(body.length + 8);
    result.writeUInt32BE(data.length);
    body.copy(result, 4);
    result.writeUInt32BE(crc32(body), body.length + 4);
    return result;
}

function overview() {
    const width = 1025, scan = 1 + width * 3;
    const data = Buffer.alloc(scan * width);
    for (let row = 0; row < width; row++) {
        const z = worldSize - row * 2 * mapScale;
        for (let col = 0; col < width; col++) {
            const x = col * 2 * mapScale, index = z * size + x, height = heightAt(x, z);
            const dx = (heightAt(Math.min(x + 1, worldSize), z) - heightAt(Math.max(x - 1, 0), z)) / 2;
            const dz = (heightAt(x, Math.min(z + 1, worldSize)) - heightAt(x, Math.max(z - 1, 0))) / 2;
            const shade = (0.74 - 0.46 * dx - 0.48 * dz) / Math.hypot(dx, dz, 1);
            const light = 0.54 + 0.67 * clamp(shade);
            const biome = (cx, cz, rx, rz) => 1 - ramp(0.38, 1.1, Math.hypot((x / mapScale - cx) / rx, (z / mapScale - cz) / rz));
            const vegetation = biome(300, 1140, 510, 570);
            const autumn = biome(420, 1720, 640, 370);
            const quarry = biome(1800, 1160, 470, 410);
            let color = [124, 145, 101].map((c, channel) => mix(mix(mix(c, [62, 102, 81][channel],
                vegetation), [151, 108, 61][channel], autumn), [163, 111, 77][channel], quarry));
            const ridge = production ? ramp(285, 460, height) : z > 1380 ? ramp(158, 284, height) : ramp(150, 330, height) * 0.7;
            color = color.map((c, channel) => mix(c, [222, 224, 214][channel], ridge));
            const water = height <= surfaceAt(x, z, 0.64);
            const trail = !water && roads.some((road) => road.distance2[index] < 10);
            const landmark = plan.landmarks.some(({ position: [px, pz] }) => Math.abs(x - px) + Math.abs(z - pz) < 12);
            if (water) color = [52, 116, 141];
            if (trail) color = [221, 191, 133];
            if (landmark) color = [255, 237, 179];
            const offset = row * scan + 1 + col * 3;
            color.forEach((channel, i) => { data[offset + i] = Math.round(Math.min(255, channel * (water || landmark ? 1 : light))); });
        }
    }
    const header = Buffer.alloc(13);
    header.writeUInt32BE(width, 0);
    header.writeUInt32BE(width, 4);
    header[8] = 8;
    header[9] = 2;
    return Buffer.concat([Buffer.from([137,80,78,71,13,10,26,10]), pngChunk('IHDR', header),
        pngChunk('IDAT', deflateSync(data)), pngChunk('IEND', Buffer.alloc(0))]);
}

function waterVertex(x, z) {
    const index = z * size + x;
    const ground = heightAt(x, z);
    let level = 0;
    let depth = z < 240 * mapScale ? Math.min(-ground, 240 * mapScale - z) : -2;
    const distance = Math.sqrt(river.distance2[index]), width = river.width[index];
    if (distance < width * 2) {
        level = river.level[index];
        for (const basin of lakes) {
            const shore = lakeDistance(x, z, basin);
            if (shore >= -8 && shore <= 8) level = mix(basin.surface, level, ramp(-2, 8, shore));
        }
        depth = Math.min(level - ground, width * 1.38 - distance);
    }
    for (const basin of lakes) {
        const shore = lakeDistance(x, z, basin);
        if (shore > 4) continue;
        const lakeDepth = Math.min(basin.surface - ground, 1.25 - shore);
        if (lakeDepth < depth) continue;
        level = basin.surface;
        depth = lakeDepth;
    }
    return [x, level, z, Math.max(-2, depth)];
}

function surfaceTriangles() {
    const step = 2;
    const points = worldSize / step + 1;
    const chunks = [];
    let bytes = Buffer.alloc(36 * 32768);
    let cursor = 0;
    const row = (z) => Array.from({ length: points }, (_, i) => waterVertex(i * step, z));
    function clip(a, b, c) {
        if (a[3] <= 0 && b[3] <= 0 && c[3] <= 0) return;
        const polygon = [];
        for (const [from, to] of [[c, a], [a, b], [b, c]]) {
            if ((from[3] > 0) !== (to[3] > 0)) {
                const t = from[3] / (from[3] - to[3]);
                const x = mix(from[0], to[0], t), z = mix(from[2], to[2], t);
                const wet = from[3] > 0 ? from : to, dry = from[3] > 0 ? to : from;
                const y = Math.abs(wet[1] - dry[1]) <= 2 ? mix(from[1], to[1], t) : wet[1];
                polygon.push([x, y, z]);
            }
            if (to[3] > 0) polygon.push(to);
        }
        for (let i = 1; i + 1 < polygon.length; i++) {
            if (cursor === bytes.length) {
                chunks.push(bytes);
                bytes = Buffer.alloc(bytes.length);
                cursor = 0;
            }
            for (const point of [polygon[0], polygon[i], polygon[i + 1]]) {
                for (let axis = 0; axis < 3; axis++) {
                    bytes.writeFloatLE(point[axis] + (axis === 1 ? 0.04 : 0), cursor);
                    cursor += 4;
                }
            }
        }
    }
    // 한 수위인 깊은 호수와 바다 내부만 큰 면으로 합친다.
    const stride = production ? 4 : 1;
    let rows = [row(0)];
    for (let z = 0; z < worldSize; z += step * stride) {
        for (let r = 1; r <= stride; r++) rows.push(row(z + r * step));
        for (let x = 0; x + stride < points; x += stride) {
            const level = rows[0][x][1];
            let isFlatWet = true, isDry = true;
            for (const line of rows) for (let column = x; column <= x + stride; column++) {
                const point = line[column];
                isDry &&= point[3] <= 0;
                isFlatWet &&= point[3] > 0 && Math.abs(point[1] - level) < 0.001;
            }
            if (isDry) continue;
            if (isFlatWet) {
                clip(rows[0][x], rows[stride][x], rows[stride][x + stride]);
                clip(rows[0][x], rows[stride][x + stride], rows[0][x + stride]);
                continue;
            }
            for (let r = 0; r < stride; r++) for (let i = x; i < x + stride; i++) {
                clip(rows[r][i], rows[r + 1][i], rows[r + 1][i + 1]);
                clip(rows[r][i], rows[r + 1][i + 1], rows[r][i + 1]);
            }
        }
        rows = [rows[stride]];
    }
    return Buffer.concat([...chunks, bytes.subarray(0, cursor)]);
}

function expandToFourKilometers() {
    const expandedSize = (size - 1) * 2 + 1;
    const expanded = Buffer.alloc(expandedSize * expandedSize * 2);
    for (let z = 0; z < expandedSize; z++) {
        const row = Math.floor(z / 2) * size;
        const nextRow = Math.min(size - 1, Math.ceil(z / 2)) * size;
        for (let x = 0; x < expandedSize; x++) {
            const left = Math.floor(x / 2), right = Math.ceil(x / 2);
            const a = raw.readUInt16LE((row + left) * 2);
            const b = raw.readUInt16LE((row + right) * 2);
            const c = raw.readUInt16LE((nextRow + left) * 2);
            const d = raw.readUInt16LE((nextRow + right) * 2);
            expanded.writeUInt16LE(Math.round((a + b + c + d) / 4), (z * expandedSize + x) * 2);
        }
    }
    for (const [x, z] of [[0, 0], [size - 1, 0], [0, size - 1], [size - 1, size - 1], [lake.x, lake.z]]) {
        assert.equal(expanded.readUInt16LE((2 * z * expandedSize + 2 * x) * 2), raw.readUInt16LE((z * size + x) * 2));
    }
    return expanded;
}

const report = {
    sha256: createHash('sha256').update(raw).digest('hex'), bytes: raw.length, size,
    min: +min.toFixed(2), max: +max.toFixed(2), bridges, westernWater, watercourseStats, routeStats, sightlines,
    cornersSouthFirst: [heightAt(0, 0), heightAt(worldSize, 0), heightAt(0, worldSize), heightAt(worldSize, worldSize)]
};
if (production) {
    const peaks = [[1340,2090,3450,4000],[2100,2950,3400,4096],[2960,3750,3300,4060]]
        .map(([minX, maxX, minZ, maxZ]) => {
            let elevation = -Infinity, position = null;
            for (let z = minZ; z <= maxZ; z += 8) for (let x = minX; x <= maxX; x += 8) {
                const value = heightAt(x, z);
                if (value > elevation) { elevation = value; position = [x, z]; }
            }
            return { position, elevation: +elevation.toFixed(2) };
        });
    report.northernPeaks = peaks;
    assert(report.northernPeaks.every((peak) => peak.elevation > 440), '북부 산맥 지형고 검증 실패');
    if (process.argv[4] !== '--mesh-only') {
        const variant = joinApproaches ? 'Joined' : bankFillet ? 'BankFillet' : refined ? 'Refined' : 'Production';
        writeFileSync(file('RiverBasin_4097_' + variant + '_R16LE_SouthFirst.raw'), raw);
        writeFileSync(file('RiverBasin_4km_' + variant + '_Relief.png'), overview());
        writeFileSync(file(joinApproaches ? 'joined-height-validation.json' : bankFillet ? 'bank-fillet-validation.json' : refined ? 'refined-height-validation.json' : 'production-height-validation.json'), JSON.stringify(report, null, 2) + '\n');
    }
    if (process.argv[3]) writeFileSync(process.argv[3], surfaceTriangles());
    console.log(JSON.stringify(report));
    process.exit(0);
}
writeFileSync(file('RiverBasin_2049_Shore_R16LE_SouthFirst.raw'), raw);
writeFileSync(file('RiverBasin_Shore_Relief.png'), overview());
writeFileSync(file('shore-validation.json'), JSON.stringify(report, null, 2) + '\n');
if (process.argv[2]) writeFileSync(process.argv[2], surfaceTriangles());
if (process.argv[3]) {
    const expanded = expandToFourKilometers();
    writeFileSync(process.argv[3], expanded);
    console.log('4km RAW SHA-256: ' + createHash('sha256').update(expanded).digest('hex'));
}
console.log(JSON.stringify(report));
