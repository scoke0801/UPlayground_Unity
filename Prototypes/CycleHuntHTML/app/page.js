"use client";

import { useCallback, useEffect, useRef, useState } from "react";

const WORLD = { w: 1900, h: 1180 };
const PLAYER_MAX_HP = 100;
const bossCatalog = [
  { name: "녹슨 포식자", epithet: "폐허를 긁는 송곳니", color: "#ef775f", hp: 155, role: "파쇄" },
  { name: "청람의 감시자", epithet: "폭풍을 두른 눈", color: "#55bcd6", hp: 170, role: "제압" },
  { name: "백화의 묘지기", epithet: "꽃 아래 잠든 칼", color: "#dca2e8", hp: 185, role: "회복" },
  { name: "황혼의 심장", epithet: "사이클의 문지기", color: "#f3c66b", hp: 360, role: "중앙" },
];

const defaultHud = {
  hp: 100,
  bossHp: 0,
  bossMaxHp: 1,
  bossName: "",
  sigils: 0,
  materials: 0,
  time: 0,
  assist: null,
  assistCooldown: 0,
  message: "",
  phase: "준비",
  compass: "",
  remains: false,
};

function mulberry32(seed) {
  return () => {
    let t = (seed += 0x6d2b79f5);
    t = Math.imul(t ^ (t >>> 15), t | 1);
    t ^= t + Math.imul(t ^ (t >>> 7), t | 61);
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}

function distance(a, b) {
  return Math.hypot(a.x - b.x, a.y - b.y);
}

function formatTime(value) {
  const minutes = Math.floor(value / 60).toString().padStart(2, "0");
  const seconds = Math.floor(value % 60).toString().padStart(2, "0");
  return `${minutes}:${seconds}`;
}

function createRun(seed) {
  const random = mulberry32(seed);
  const shuffled = bossCatalog.slice(0, 3).sort(() => random() - 0.5);
  const points = [
    { x: 360, y: 320 },
    { x: 1530, y: 300 },
    { x: 1490, y: 920 },
  ];

  return {
    seed,
    random,
    time: 0,
    phase: "Active",
    player: { x: 330, y: 940, hp: PLAYER_MAX_HP, invuln: 0, attackCd: 0, dashCd: 0 },
    camera: { x: 330, y: 940 },
    bosses: shuffled.map((data, i) => ({
      ...data,
      ...points[i],
      index: i,
      maxHp: data.hp,
      currentHp: data.hp,
      alive: true,
      discovered: false,
      engaged: false,
      attackCd: 1.3,
      pulse: random() * 6,
      isCentral: false,
    })).concat({
      ...bossCatalog[3],
      x: 950,
      y: 565,
      index: 3,
      maxHp: bossCatalog[3].hp,
      currentHp: bossCatalog[3].hp,
      alive: true,
      discovered: false,
      engaged: false,
      attackCd: 1.7,
      pulse: 0,
      isCentral: true,
      locked: true,
    }),
    sigils: 0,
    materials: 0,
    bankedMaterials: 0,
    defeated: [],
    particles: [],
    projectiles: [],
    remains: null,
    rest: { x: 330, y: 940 },
    assist: null,
    assistCooldown: 0,
    portal: null,
    message: "세 개의 외곽 봉인을 추적하세요",
    messageTime: 4,
    shake: 0,
    hitFlash: 0,
    finished: false,
  };
}

function addBurst(run, x, y, color, count = 14, power = 120) {
  for (let i = 0; i < count; i += 1) {
    const angle = run.random() * Math.PI * 2;
    const speed = power * (0.35 + run.random() * 0.8);
    run.particles.push({
      x,
      y,
      vx: Math.cos(angle) * speed,
      vy: Math.sin(angle) * speed,
      life: 0.45 + run.random() * 0.5,
      maxLife: 1,
      size: 2 + run.random() * 5,
      color,
    });
  }
}

function drawWorld(ctx, run, width, height, pixelRatio) {
  const scale = Math.min(width / 1060, height / 680);
  const viewW = width / scale;
  const viewH = height / scale;
  const targetX = Math.max(viewW / 2, Math.min(WORLD.w - viewW / 2, run.player.x));
  const targetY = Math.max(viewH / 2, Math.min(WORLD.h - viewH / 2, run.player.y));
  run.camera.x += (targetX - run.camera.x) * 0.08;
  run.camera.y += (targetY - run.camera.y) * 0.08;

  const shakeX = run.shake > 0 ? (run.random() - 0.5) * run.shake * 12 : 0;
  const shakeY = run.shake > 0 ? (run.random() - 0.5) * run.shake * 12 : 0;
  const offsetX = width / 2 - run.camera.x * scale + shakeX;
  const offsetY = height / 2 - run.camera.y * scale + shakeY;

  ctx.setTransform(scale * pixelRatio, 0, 0, scale * pixelRatio, offsetX * pixelRatio, offsetY * pixelRatio);
  const bg = ctx.createLinearGradient(0, 0, WORLD.w, WORLD.h);
  bg.addColorStop(0, "#14272b");
  bg.addColorStop(0.52, "#101d25");
  bg.addColorStop(1, "#211b29");
  ctx.fillStyle = bg;
  ctx.fillRect(0, 0, WORLD.w, WORLD.h);

  ctx.globalAlpha = 0.18;
  for (let x = 0; x < WORLD.w; x += 92) {
    for (let y = 0; y < WORLD.h; y += 92) {
      const n = ((x * 17 + y * 31 + run.seed) % 97) / 97;
      ctx.fillStyle = n > 0.55 ? "#8dd6bd" : "#6f829a";
      ctx.beginPath();
      ctx.arc(x + n * 42, y + (1 - n) * 38, 1.5 + n * 2.8, 0, Math.PI * 2);
      ctx.fill();
    }
  }
  ctx.globalAlpha = 1;

  const roads = [
    [run.rest, { x: 950, y: 565 }],
    [{ x: 950, y: 565 }, { x: 360, y: 320 }],
    [{ x: 950, y: 565 }, { x: 1530, y: 300 }],
    [{ x: 950, y: 565 }, { x: 1490, y: 920 }],
  ];
  ctx.lineCap = "round";
  roads.forEach(([a, b]) => {
    ctx.strokeStyle = "rgba(210, 226, 207, .06)";
    ctx.lineWidth = 58;
    ctx.beginPath();
    ctx.moveTo(a.x, a.y);
    ctx.lineTo(b.x, b.y);
    ctx.stroke();
    ctx.strokeStyle = "rgba(203, 226, 216, .12)";
    ctx.lineWidth = 2;
    ctx.setLineDash([10, 18]);
    ctx.stroke();
    ctx.setLineDash([]);
  });

  run.bosses.forEach((boss) => {
    const unlocked = !boss.isCentral || run.sigils === 3;
    const radius = boss.isCentral ? 126 : 96;
    ctx.fillStyle = unlocked ? `${boss.color}12` : "rgba(13, 19, 25, .48)";
    ctx.strokeStyle = unlocked ? `${boss.color}55` : "rgba(125, 137, 149, .2)";
    ctx.lineWidth = boss.engaged ? 4 : 2;
    ctx.beginPath();
    ctx.arc(boss.x, boss.y, radius, 0, Math.PI * 2);
    ctx.fill();
    ctx.stroke();
    for (let i = 0; i < 8; i += 1) {
      const angle = (i / 8) * Math.PI * 2 + run.time * 0.06;
      ctx.fillStyle = unlocked ? `${boss.color}66` : "#65717b44";
      ctx.beginPath();
      ctx.arc(boss.x + Math.cos(angle) * radius, boss.y + Math.sin(angle) * radius, 4, 0, Math.PI * 2);
      ctx.fill();
    }

    if (boss.alive) {
      const breathe = 1 + Math.sin(run.time * 2.4 + boss.pulse) * 0.08;
      ctx.save();
      ctx.translate(boss.x, boss.y);
      ctx.scale(breathe, breathe);
      ctx.rotate(run.time * 0.12 * (boss.index % 2 ? -1 : 1));
      ctx.fillStyle = unlocked ? boss.color : "#53616b";
      ctx.shadowColor = unlocked ? boss.color : "transparent";
      ctx.shadowBlur = boss.engaged ? 25 : 10;
      ctx.beginPath();
      const sides = boss.isCentral ? 8 : 6;
      for (let i = 0; i < sides; i += 1) {
        const angle = (i / sides) * Math.PI * 2 - Math.PI / 2;
        const rr = boss.isCentral && i % 2 ? 32 : 43;
        const px = Math.cos(angle) * rr;
        const py = Math.sin(angle) * rr;
        if (i === 0) ctx.moveTo(px, py);
        else ctx.lineTo(px, py);
      }
      ctx.closePath();
      ctx.fill();
      ctx.shadowBlur = 0;
      ctx.fillStyle = "#10171d";
      ctx.beginPath();
      ctx.arc(-12, -3, 4, 0, Math.PI * 2);
      ctx.arc(12, -3, 4, 0, Math.PI * 2);
      ctx.fill();
      ctx.restore();
    } else {
      ctx.strokeStyle = `${boss.color}66`;
      ctx.lineWidth = 3;
      ctx.beginPath();
      ctx.arc(boss.x, boss.y, 34 + Math.sin(run.time * 3) * 4, 0, Math.PI * 2);
      ctx.stroke();
    }
  });

  ctx.fillStyle = "#80d8c1";
  ctx.shadowColor = "#80d8c1";
  ctx.shadowBlur = 18;
  ctx.beginPath();
  ctx.moveTo(run.rest.x, run.rest.y - 22);
  ctx.lineTo(run.rest.x + 16, run.rest.y + 16);
  ctx.lineTo(run.rest.x - 16, run.rest.y + 16);
  ctx.closePath();
  ctx.fill();
  ctx.shadowBlur = 0;

  if (run.remains) {
    ctx.strokeStyle = "#f4e5bf";
    ctx.fillStyle = "rgba(244, 229, 191, .16)";
    ctx.lineWidth = 3;
    ctx.beginPath();
    ctx.arc(run.remains.x, run.remains.y, 24 + Math.sin(run.time * 4) * 5, 0, Math.PI * 2);
    ctx.fill();
    ctx.stroke();
    ctx.fillStyle = "#f4e5bf";
    ctx.font = "600 13px sans-serif";
    ctx.textAlign = "center";
    ctx.fillText("유해 · E", run.remains.x, run.remains.y - 38);
  }

  if (run.portal) {
    const pulse = 42 + Math.sin(run.time * 3.4) * 7;
    const portalGradient = ctx.createRadialGradient(run.portal.x, run.portal.y, 4, run.portal.x, run.portal.y, pulse);
    portalGradient.addColorStop(0, "rgba(255, 245, 208, .9)");
    portalGradient.addColorStop(0.35, "rgba(235, 190, 102, .5)");
    portalGradient.addColorStop(1, "rgba(235, 190, 102, 0)");
    ctx.fillStyle = portalGradient;
    ctx.beginPath();
    ctx.arc(run.portal.x, run.portal.y, pulse, 0, Math.PI * 2);
    ctx.fill();
    ctx.strokeStyle = "#f3d28c";
    ctx.lineWidth = 4;
    ctx.beginPath();
    ctx.ellipse(run.portal.x, run.portal.y, 24, 45, 0, 0, Math.PI * 2);
    ctx.stroke();
    ctx.fillStyle = "#f6e7c2";
    ctx.font = "700 14px sans-serif";
    ctx.textAlign = "center";
    ctx.fillText("정산 포털 · E", run.portal.x, run.portal.y - 62);
  }

  run.projectiles.forEach((p) => {
    ctx.fillStyle = p.color;
    ctx.shadowColor = p.color;
    ctx.shadowBlur = 12;
    ctx.beginPath();
    ctx.arc(p.x, p.y, p.size, 0, Math.PI * 2);
    ctx.fill();
    ctx.shadowBlur = 0;
  });

  run.particles.forEach((p) => {
    ctx.globalAlpha = Math.max(0, p.life / p.maxLife);
    ctx.fillStyle = p.color;
    ctx.beginPath();
    ctx.arc(p.x, p.y, p.size, 0, Math.PI * 2);
    ctx.fill();
  });
  ctx.globalAlpha = 1;

  const player = run.player;
  ctx.save();
  ctx.translate(player.x, player.y);
  if (player.invuln > 0) ctx.globalAlpha = Math.sin(run.time * 32) > 0 ? 0.4 : 1;
  ctx.fillStyle = run.hitFlash > 0 ? "#ffffff" : "#d8f1e9";
  ctx.shadowColor = "#8ce4ca";
  ctx.shadowBlur = 16;
  ctx.beginPath();
  ctx.arc(0, 0, 15, 0, Math.PI * 2);
  ctx.fill();
  ctx.shadowBlur = 0;
  ctx.fillStyle = "#1c3136";
  ctx.beginPath();
  ctx.arc(4, -3, 5, 0, Math.PI * 2);
  ctx.fill();
  ctx.strokeStyle = "#d8f1e9";
  ctx.lineWidth = 3;
  ctx.beginPath();
  ctx.moveTo(-11, 10);
  ctx.lineTo(-20, 24);
  ctx.stroke();
  ctx.restore();

  ctx.setTransform(pixelRatio, 0, 0, pixelRatio, 0, 0);
}

export default function CycleHuntPage() {
  const canvasRef = useRef(null);
  const runRef = useRef(null);
  const keysRef = useRef(new Set());
  const frameRef = useRef(0);
  const lastTimeRef = useRef(0);
  const hudTickRef = useRef(0);
  const [screen, setScreen] = useState("title");
  const [seedInput, setSeedInput] = useState(() => Math.floor(100000 + Math.random() * 899999).toString());
  const [hud, setHud] = useState(defaultHud);
  const [summary, setSummary] = useState(null);
  const [showHelp, setShowHelp] = useState(false);

  const showMessage = useCallback((run, message, duration = 2.8) => {
    run.message = message;
    run.messageTime = duration;
  }, []);

  const finishRun = useCallback((run) => {
    run.finished = true;
    run.phase = "Completed";
    run.bankedMaterials += run.materials;
    const grade = run.time < 180 ? "S" : run.time < 300 ? "A" : "B";
    setSummary({
      seed: run.seed,
      time: formatTime(run.time),
      bosses: run.defeated.length,
      materials: run.bankedMaterials,
      assist: run.assist?.name ?? "영입 실패",
      grade,
    });
    setScreen("summary");
  }, []);

  const interact = useCallback(() => {
    const run = runRef.current;
    if (!run || run.finished || screen !== "game") return;
    if (run.remains && distance(run.player, run.remains) < 68) {
      run.materials += run.remains.materials;
      run.player.hp = Math.min(PLAYER_MAX_HP, run.player.hp + run.remains.hp);
      addBurst(run, run.remains.x, run.remains.y, "#f5e4bd", 22, 150);
      run.remains = null;
      showMessage(run, "유해 회수 · 미정산 재료와 생명력이 복구되었습니다", 3.4);
      return;
    }
    if (run.portal && distance(run.player, run.portal) < 82) {
      finishRun(run);
      return;
    }
    const nearBoss = run.bosses.find((boss) => boss.alive && distance(run.player, boss) < 130);
    if (nearBoss?.locked) {
      showMessage(run, `중앙 봉인이 잠겨 있습니다 · 외곽 인장 ${run.sigils}/3`);
    }
  }, [finishRun, screen, showMessage]);

  const useAssist = useCallback(() => {
    const run = runRef.current;
    if (!run || !run.assist || run.assistCooldown > 0 || run.finished || screen !== "game") return;
    const target = run.bosses
      .filter((boss) => boss.alive && !boss.locked)
      .sort((a, b) => distance(run.player, a) - distance(run.player, b))[0];
    if (!target || distance(run.player, target) > 340) {
      showMessage(run, "어시스트가 포착할 보스가 없습니다");
      return;
    }
    const damage = 55;
    target.currentHp -= damage;
    run.assistCooldown = 24;
    run.shake = 0.8;
    addBurst(run, target.x, target.y, run.assist.color, 34, 240);
    showMessage(run, `${run.assist.name} 어시스트 · ${run.assist.role} 일격!`, 2.6);
  }, [screen, showMessage]);

  const startRun = useCallback(() => {
    const parsed = Number.parseInt(seedInput, 10);
    const seed = Number.isFinite(parsed) ? Math.abs(parsed) % 1000000 : 427201;
    runRef.current = createRun(seed);
    setHud(defaultHud);
    setSummary(null);
    setScreen("game");
  }, [seedInput]);

  useEffect(() => {
    const onKeyDown = (event) => {
      keysRef.current.add(event.code);
      if (["Space", "ArrowUp", "ArrowDown", "ArrowLeft", "ArrowRight"].includes(event.code)) event.preventDefault();
      if (event.code === "KeyE" && !event.repeat) interact();
      if (event.code === "KeyQ" && !event.repeat) useAssist();
      if (event.code === "KeyH" && !event.repeat) setShowHelp((value) => !value);
    };
    const onKeyUp = (event) => keysRef.current.delete(event.code);
    window.addEventListener("keydown", onKeyDown);
    window.addEventListener("keyup", onKeyUp);
    return () => {
      window.removeEventListener("keydown", onKeyDown);
      window.removeEventListener("keyup", onKeyUp);
    };
  }, [interact, useAssist]);

  useEffect(() => {
    if (screen !== "game") return undefined;
    const canvas = canvasRef.current;
    const ctx = canvas.getContext("2d");
    let alive = true;

    const resize = () => {
      const ratio = Math.min(window.devicePixelRatio || 1, 2);
      const rect = canvas.getBoundingClientRect();
      canvas.width = Math.floor(rect.width * ratio);
      canvas.height = Math.floor(rect.height * ratio);
      ctx.setTransform(ratio, 0, 0, ratio, 0, 0);
    };
    resize();
    window.addEventListener("resize", resize);

    const loop = (time) => {
      if (!alive) return;
      const run = runRef.current;
      const dt = Math.min(0.033, (time - (lastTimeRef.current || time)) / 1000);
      lastTimeRef.current = time;
      const ratio = Math.min(window.devicePixelRatio || 1, 2);
      const width = canvas.width / ratio;
      const height = canvas.height / ratio;

      if (run && !run.finished) {
        run.time += dt;
        run.messageTime = Math.max(0, run.messageTime - dt);
        run.assistCooldown = Math.max(0, run.assistCooldown - dt);
        run.shake = Math.max(0, run.shake - dt * 3);
        run.hitFlash = Math.max(0, run.hitFlash - dt);
        const player = run.player;
        player.invuln = Math.max(0, player.invuln - dt);
        player.attackCd = Math.max(0, player.attackCd - dt);
        player.dashCd = Math.max(0, player.dashCd - dt);

        let dx = 0;
        let dy = 0;
        const keys = keysRef.current;
        if (keys.has("KeyW") || keys.has("ArrowUp")) dy -= 1;
        if (keys.has("KeyS") || keys.has("ArrowDown")) dy += 1;
        if (keys.has("KeyA") || keys.has("ArrowLeft")) dx -= 1;
        if (keys.has("KeyD") || keys.has("ArrowRight")) dx += 1;
        const length = Math.hypot(dx, dy) || 1;
        const dashing = (keys.has("ShiftLeft") || keys.has("ShiftRight")) && player.dashCd <= 0 && (dx || dy);
        const speed = dashing ? 620 : 235;
        if (dashing) {
          player.dashCd = 1.25;
          player.invuln = 0.28;
          addBurst(run, player.x, player.y, "#93e3cc", 9, 80);
        }
        player.x = Math.max(35, Math.min(WORLD.w - 35, player.x + (dx / length) * speed * dt));
        player.y = Math.max(35, Math.min(WORLD.h - 35, player.y + (dy / length) * speed * dt));

        const central = run.bosses[3];
        if (run.sigils === 3 && central.locked) {
          central.locked = false;
          showMessage(run, "세 인장이 공명합니다 · 중앙 봉인 해제", 4);
          addBurst(run, central.x, central.y, central.color, 40, 250);
        }

        let activeBoss = null;
        run.bosses.forEach((boss) => {
          if (!boss.alive || boss.locked) return;
          const dist = distance(player, boss);
          if (dist < 310) {
            activeBoss = boss;
            if (!boss.discovered) {
              boss.discovered = true;
              boss.engaged = true;
              showMessage(run, `${boss.name} · ${boss.epithet}`, 3.5);
            }
          }
          if (!boss.engaged) return;
          boss.attackCd -= dt;
          if (dist > 48 && dist < 390) {
            const chase = boss.isCentral ? 32 : 24;
            boss.x += ((player.x - boss.x) / Math.max(dist, 1)) * chase * dt;
            boss.y += ((player.y - boss.y) / Math.max(dist, 1)) * chase * dt;
          }
          if (boss.attackCd <= 0 && dist < 390) {
            const angle = Math.atan2(player.y - boss.y, player.x - boss.x);
            const count = boss.isCentral ? 5 : 3;
            for (let i = 0; i < count; i += 1) {
              const spread = (i - (count - 1) / 2) * 0.18;
              run.projectiles.push({
                x: boss.x,
                y: boss.y,
                vx: Math.cos(angle + spread) * (boss.isCentral ? 205 : 175),
                vy: Math.sin(angle + spread) * (boss.isCentral ? 205 : 175),
                life: 3.2,
                size: boss.isCentral ? 8 : 6,
                damage: boss.isCentral ? 14 : 10,
                color: boss.color,
              });
            }
            boss.attackCd = boss.isCentral ? 1.15 : 1.65;
          }
        });

        if ((keys.has("Space") || keys.has("KeyJ")) && player.attackCd <= 0) {
          player.attackCd = 0.34;
          const targets = run.bosses.filter((boss) => boss.alive && !boss.locked && distance(player, boss) < 105);
          if (targets.length) {
            targets.forEach((boss) => {
              boss.currentHp -= boss.isCentral ? 13 : 17;
              boss.engaged = true;
              addBurst(run, boss.x, boss.y, "#dff7ee", 8, 100);
            });
            run.shake = 0.25;
          } else {
            addBurst(run, player.x + 22, player.y, "#b6eadb", 4, 55);
          }
        }

        run.projectiles = run.projectiles.filter((p) => {
          p.x += p.vx * dt;
          p.y += p.vy * dt;
          p.life -= dt;
          if (p.life > 0 && distance(player, p) < p.size + 14 && player.invuln <= 0) {
            player.hp -= p.damage;
            player.invuln = 0.65;
            run.hitFlash = 0.12;
            run.shake = 0.55;
            addBurst(run, player.x, player.y, "#ef7f72", 13, 135);
            return false;
          }
          return p.life > 0;
        });

        run.bosses.forEach((boss) => {
          if (!boss.alive || boss.currentHp > 0) return;
          boss.alive = false;
          boss.engaged = false;
          run.defeated.push(boss.name);
          run.materials += boss.isCentral ? 12 : 5 + Math.floor(run.random() * 4);
          addBurst(run, boss.x, boss.y, boss.color, boss.isCentral ? 60 : 38, 260);
          if (boss.isCentral) {
            run.phase = "BossDefeated";
            run.portal = { x: 950, y: 565 };
            showMessage(run, "중앙 보스 처치 · 전리품은 아직 미정산 상태입니다", 4.5);
          } else {
            run.sigils += 1;
            const recruitChance = 0.4 + (player.hp > 78 ? 0.15 : 0) + (run.sigils === 3 ? 0.15 : 0);
            if (!run.assist && run.random() < recruitChance) {
              run.assist = { name: boss.name, role: boss.role, color: boss.color };
              showMessage(run, `${boss.name} 어시스트 영입 · Q로 호출`, 4);
            } else {
              showMessage(run, `외곽 인장 획득 ${run.sigils}/3 · 미정산 재료 +${run.materials}`, 3);
            }
          }
        });

        if (player.hp <= 0) {
          const lost = Math.floor(run.materials * 0.3);
          run.remains = { x: player.x, y: player.y, materials: lost, hp: 30 };
          run.materials -= lost;
          player.x = run.rest.x;
          player.y = run.rest.y;
          player.hp = PLAYER_MAX_HP;
          player.invuln = 2;
          run.projectiles = [];
          showMessage(run, `파티 전멸 · 유해에 미정산 재료 ${lost}개가 남았습니다`, 4.5);
        }

        run.particles = run.particles.filter((p) => {
          p.x += p.vx * dt;
          p.y += p.vy * dt;
          p.vx *= 0.95;
          p.vy *= 0.95;
          p.life -= dt;
          return p.life > 0;
        });

        hudTickRef.current += dt;
        if (hudTickRef.current > 0.08) {
          hudTickRef.current = 0;
          let compass = "외곽 인장";
          if (run.sigils === 3) compass = run.phase === "BossDefeated" ? "정산 포털" : "중앙 보스";
          if (run.remains) compass = "유해 회수";
          setHud({
            hp: Math.max(0, player.hp),
            bossHp: Math.max(0, activeBoss?.currentHp ?? 0),
            bossMaxHp: activeBoss?.maxHp ?? 1,
            bossName: activeBoss?.name ?? "",
            sigils: run.sigils,
            materials: run.materials,
            time: run.time,
            assist: run.assist,
            assistCooldown: run.assistCooldown,
            message: run.messageTime > 0 ? run.message : "",
            phase: run.phase,
            compass,
            remains: Boolean(run.remains),
          });
        }
      }

      ctx.setTransform(ratio, 0, 0, ratio, 0, 0);
      ctx.clearRect(0, 0, width, height);
      if (run) drawWorld(ctx, run, width, height, ratio);
      frameRef.current = requestAnimationFrame(loop);
    };

    frameRef.current = requestAnimationFrame(loop);
    return () => {
      alive = false;
      cancelAnimationFrame(frameRef.current);
      window.removeEventListener("resize", resize);
      lastTimeRef.current = 0;
    };
  }, [screen, showMessage]);

  return (
    <main className="game-shell">
      {screen === "title" && (
        <section className="title-screen">
          <div className="ambient-orb orb-a" />
          <div className="ambient-orb orb-b" />
          <div className="title-grid" />
          <div className="eyebrow"><span>U</span>PLAYGROUND / FIELD TEST 01</div>
          <div className="title-copy">
            <p className="kicker">A SEED-BORN HUNT</p>
            <h1>CYCLE<br /><em>HUNT</em></h1>
            <p className="lede">
              외곽의 세 인장을 깨우고, 심장부의 보스를 쓰러뜨려라.<br />
              돌아오는 것까지가 하나의 사이클이다.
            </p>
          </div>
          <div className="launch-card">
            <div className="launch-label">RUN CONFIGURATION</div>
            <label htmlFor="seed">월드 시드</label>
            <div className="seed-row">
              <span>#</span>
              <input id="seed" value={seedInput} maxLength={6} inputMode="numeric" onChange={(e) => setSeedInput(e.target.value.replace(/\D/g, ""))} />
              <button className="dice-button" onClick={() => setSeedInput(Math.floor(100000 + Math.random() * 899999).toString())} aria-label="시드 무작위 생성">↻</button>
            </div>
            <div className="run-facts">
              <div><b>01</b><span>사이클</span></div>
              <div><b>03+1</b><span>보스</span></div>
              <div><b>∞</b><span>재시도</span></div>
            </div>
            <button className="primary-button" onClick={startRun}><span>사이클 진입</span><i>→</i></button>
            <p className="microcopy">키보드 플레이 · 약 3–6분</p>
          </div>
          <div className="title-footer">
            <span>PROTOTYPE BUILD</span>
            <span>WASD 이동 · SPACE 공격 · SHIFT 회피</span>
          </div>
        </section>
      )}

      {screen === "game" && (
        <section className="play-screen">
          <canvas ref={canvasRef} className="game-canvas" />
          <div className="top-hud">
            <div className="brand-mark"><b>U</b><span>CYCLE<br />HUNT</span></div>
            <div className="objective-strip">
              <span className="objective-label">현재 목표</span>
              <strong>{hud.compass}</strong>
              <div className="sigils">
                {[0, 1, 2].map((i) => <i key={i} className={i < hud.sigils ? "filled" : ""} />)}
              </div>
            </div>
            <div className="run-clock"><span>RUN 01</span><b>{formatTime(hud.time)}</b><small>SEED {runRef.current?.seed}</small></div>
          </div>

          {hud.bossName && (
            <div className="boss-hud">
              <div className="boss-name"><span>ENCOUNTER</span><strong>{hud.bossName}</strong></div>
              <div className="boss-bar"><i style={{ width: `${(hud.bossHp / hud.bossMaxHp) * 100}%` }} /></div>
              <span>{Math.ceil(hud.bossHp)} / {hud.bossMaxHp}</span>
            </div>
          )}

          {hud.message && <div className="toast-message">{hud.message}</div>}

          <div className="bottom-hud">
            <div className="player-vitals">
              <div className="portrait">B</div>
              <div className="vital-copy">
                <span>BOKUSEI <small>LV.24</small></span>
                <div className="hp-bar"><i style={{ width: `${hud.hp}%` }} /></div>
                <b>{Math.ceil(hud.hp)} <small>/ {PLAYER_MAX_HP}</small></b>
              </div>
            </div>
            <div className="loot-ledger">
              <span>미정산 원장</span>
              <b>◈ {hud.materials}</b>
              <small>{hud.remains ? "유해가 필드에 남아 있음" : "포털 정산 전까지 손실 가능"}</small>
            </div>
            <div className={`assist-slot ${hud.assist ? "ready" : ""}`}>
              <div className="keycap">Q</div>
              <div>
                <span>BOSS ASSIST</span>
                <b>{hud.assist?.name ?? "미영입"}</b>
              </div>
              {hud.assistCooldown > 0 && <i style={{ "--cooldown": `${hud.assistCooldown / 24}` }}><span>{Math.ceil(hud.assistCooldown)}</span></i>}
            </div>
            <div className="controls">
              <span><kbd>WASD</kbd> 이동</span>
              <span><kbd>SPACE</kbd> 공격</span>
              <span><kbd>SHIFT</kbd> 회피</span>
              <span><kbd>E</kbd> 상호작용</span>
              <button onClick={() => setShowHelp(true)}>H 도움말</button>
            </div>
          </div>
        </section>
      )}

      {screen === "summary" && summary && (
        <section className="summary-screen">
          <div className="summary-glow" />
          <div className="summary-panel">
            <div className="summary-kicker">CYCLE SETTLED</div>
            <div className="grade">{summary.grade}</div>
            <h2>귀환 완료</h2>
            <p>중앙 보스 처치 후 포털 정산이 완료되었습니다.</p>
            <div className="summary-stats">
              <div><span>클리어 타임</span><b>{summary.time}</b></div>
              <div><span>보스 격파</span><b>{summary.bosses} / 4</b></div>
              <div><span>영구 재료</span><b>◈ {summary.materials}</b></div>
              <div><span>BossAssist</span><b>{summary.assist}</b></div>
            </div>
            <div className="settlement-note">
              <span>SETTLEMENT ID</span>
              <b>{summary.seed}-01-{summary.time.replace(":", "")}</b>
              <small>중복 정산 방지를 위한 이번 런의 고유 기록</small>
            </div>
            <button className="primary-button" onClick={() => {
              setSeedInput(Math.floor(100000 + Math.random() * 899999).toString());
              setScreen("title");
            }}><span>새 사이클 준비</span><i>↻</i></button>
          </div>
        </section>
      )}

      {showHelp && (
        <div className="modal-backdrop" onClick={() => setShowHelp(false)}>
          <div className="help-modal" onClick={(e) => e.stopPropagation()}>
            <button className="close-button" onClick={() => setShowHelp(false)}>×</button>
            <span className="modal-kicker">FIELD MANUAL</span>
            <h2>사이클 헌트 가이드</h2>
            <div className="help-grid">
              <div><b>01</b><h3>외곽 보스 3체</h3><p>지도 곳곳의 외곽 보스를 격파해 인장을 모으세요.</p></div>
              <div><b>02</b><h3>BossAssist</h3><p>영입에 성공하면 Q로 보스를 한 번 소환해 강력한 일격을 실행합니다.</p></div>
              <div><b>03</b><h3>유해 회수</h3><p>전멸하면 미정산 재료 일부가 유해에 남습니다. E로 회수하세요.</p></div>
              <div><b>04</b><h3>포털 정산</h3><p>중앙 보스 처치만으로 끝나지 않습니다. 포털에서 E를 눌러 귀환하세요.</p></div>
            </div>
            <button className="secondary-button" onClick={() => setShowHelp(false)}>필드로 돌아가기</button>
          </div>
        </div>
      )}
    </main>
  );
}
