import {
  MAX_WAVES,
  PATH_POINTS,
  PLANT_CONFIG,
  STAR_DAMAGE,
  STAR_RANGE,
  STAR_SPEED,
  ZOMBIE_META,
} from './config'
import { applySunflowerWaveStartEffects, applyWeaponHit, getAttackModifiers, grantMilestoneReward } from './equipment'
import type {
  Feedback,
  GameCommand,
  GameState,
  Plant,
  Point,
  Pot,
  Projectile,
  Zombie,
  ZombieKind,
} from './types'

const BETWEEN_WAVE_SECONDS = 3
const PROJECTILE_SPEED = 65
const BANANA_SPEED = 48
const WATERMELON_FLIGHT_SECONDS = 0.4
const WATERMELON_BLAST_RADIUS = 7
const ZOMBIE_HIT_RADIUS = 2.25

export interface WaveDefinition {
  index: number
  normal: number
  runner: number
  armored: number
  boss: number
  hpMultiplier: number
  spawnInterval: number
  reward: number
  sequence: ZombieKind[]
}

export interface DamageEvent {
  zombieId: string
  plantId: string | null
  damage: number
  kind: 'pea' | 'watermelon' | 'banana' | 'durian' | 'burn'
}

interface PathSegment {
  from: Point
  to: Point
  start: number
  length: number
}

const distance = (a: Point, b: Point) => Math.hypot(a.x - b.x, a.y - b.y)
const clamp = (value: number, min: number, max: number) => Math.max(min, Math.min(max, value))

export const PATH_SEGMENTS: PathSegment[] = PATH_POINTS.slice(0, -1).map((from, index) => {
  const start = PATH_POINTS.slice(0, index).reduce((sum, point, pointIndex) => sum + distance(point, PATH_POINTS[pointIndex + 1]), 0)
  const to = PATH_POINTS[index + 1]
  return { from, to, start, length: distance(from, to) }
})

export const PATH_LENGTH = PATH_SEGMENTS.reduce((sum, segment) => sum + segment.length, 0)

export const samplePath = (progress: number): Point => {
  const target = clamp(progress, 0, PATH_LENGTH)
  const segment = PATH_SEGMENTS.find((candidate) => target <= candidate.start + candidate.length) ?? PATH_SEGMENTS.at(-1)
  if (!segment || segment.length === 0) return { ...PATH_POINTS[0] }
  const ratio = clamp((target - segment.start) / segment.length, 0, 1)
  return {
    x: segment.from.x + (segment.to.x - segment.from.x) * ratio,
    y: segment.from.y + (segment.to.y - segment.from.y) * ratio,
  }
}

const interleaveKinds = (counts: Omit<WaveDefinition, 'index' | 'hpMultiplier' | 'spawnInterval' | 'reward' | 'sequence'>): ZombieKind[] => {
  const pools: Array<[ZombieKind, number]> = [
    ['normal', counts.normal],
    ['runner', counts.runner],
    ['armored', counts.armored],
    ['boss', counts.boss],
  ]
  const sequence: ZombieKind[] = []
  while (pools.some(([, count]) => count > 0)) {
    pools.forEach((pool) => {
      if (pool[1] > 0) {
        sequence.push(pool[0])
        pool[1] -= 1
      }
    })
  }
  return sequence
}

const WAVE_COUNTS: Array<[number, number, number, number]> = [
  [5, 0, 0, 0],
  [6, 2, 0, 0],
  [7, 3, 0, 0],
  [8, 3, 1, 0],
  [8, 4, 2, 0],
  [9, 5, 2, 0],
  [9, 6, 3, 0],
  [10, 6, 4, 0],
  [10, 7, 5, 0],
  [11, 7, 5, 1],
  [11, 8, 6, 0],
  [12, 8, 7, 0],
  [12, 9, 8, 0],
  [13, 10, 9, 1],
  [14, 11, 10, 2],
]

export const WAVE_TABLE: WaveDefinition[] = WAVE_COUNTS.map(([normal, runner, armored, boss], offset) => {
  const index = offset + 1
  const counts = { normal, runner, armored, boss }
  return {
    index,
    ...counts,
    hpMultiplier: 1 + offset * 0.11,
    spawnInterval: Math.max(0.38, 1.05 - offset * 0.045),
    reward: 5 + Math.ceil(index / 3),
    sequence: interleaveKinds(counts),
  }
})

export const getWaveDefinition = (index: number) => WAVE_TABLE[clamp(Math.floor(index), 1, MAX_WAVES) - 1]

export const getPlantPosition = (plant: Plant, pots: Pot[]): Point | null => {
  if (!plant.potId) return null
  const pot = pots.find((candidate) => candidate.id === plant.potId && candidate.active)
  return pot ? { x: pot.x, y: pot.y } : null
}

export const getPlantRange = (plant: Plant) => PLANT_CONFIG[plant.kind].range * STAR_RANGE[plant.star]
export const getPlantDamage = (plant: Plant) => PLANT_CONFIG[plant.kind].damage * STAR_DAMAGE[plant.star]
export const getPlantAttackInterval = (plant: Plant) => PLANT_CONFIG[plant.kind].interval / STAR_SPEED[plant.star]

export const selectTarget = (zombies: Zombie[], origin: Point, range: number): Zombie | null => {
  const candidates = zombies.filter((zombie) => (
    zombie.hp > 0
    && zombie.pathProgress < PATH_LENGTH
    && distance(origin, samplePath(zombie.pathProgress)) <= range
  ))
  candidates.sort((left, right) => (
    (PATH_LENGTH - left.pathProgress) - (PATH_LENGTH - right.pathProgress)
    || left.hp - right.hp
    || left.spawnOrder - right.spawnOrder
  ))
  return candidates[0] ?? null
}

const pointToSegmentDistance = (point: Point, from: Point, to: Point) => {
  const dx = to.x - from.x
  const dy = to.y - from.y
  if (dx === 0 && dy === 0) return distance(point, from)
  const ratio = clamp(((point.x - from.x) * dx + (point.y - from.y) * dy) / (dx * dx + dy * dy), 0, 1)
  return distance(point, { x: from.x + dx * ratio, y: from.y + dy * ratio })
}

const createZombie = (kind: ZombieKind, wave: WaveDefinition, id: string, spawnOrder: number): Zombie => {
  const meta = ZOMBIE_META[kind]
  const hp = Math.round(meta.hp * wave.hpMultiplier)
  return {
    id,
    kind,
    hp,
    maxHp: hp,
    speed: meta.speed,
    pathProgress: 0,
    reward: meta.reward,
    threat: meta.threat,
    spawnOrder,
    status: { slowUntil: 0, freezeUntil: 0, iceHits: 0, burns: [] },
  }
}

const beginWave = (state: GameState, index: number): GameState => {
  const wave = getWaveDefinition(index)
  return {
    ...state,
    phase: 'playing',
    wave: { index, spawned: 0, total: wave.sequence.length, spawnCooldown: 0, betweenTimer: 0, started: true },
    feedback: [...state.feedback, {
      id: `feedback-${state.nextId}`,
      text: `第 ${index} 波来袭`,
      tone: index === MAX_WAVES ? 'danger' : 'info',
      ttl: 1.8,
    }],
    nextId: state.nextId + 1,
  }
}

const advanceStatusesAndMovement = (zombies: Zombie[], elapsed: number, deltaSeconds: number): Zombie[] => zombies.map((zombie) => {
  const burns = zombie.status.burns
    .map((burn) => ({ ...burn, remaining: burn.remaining - deltaSeconds }))
    .filter((burn) => burn.remaining > 0)
  const burnDamage = zombie.status.burns.reduce((sum, burn) => sum + burn.damagePerSecond * Math.min(deltaSeconds, burn.remaining), 0)
  const frozen = zombie.status.freezeUntil > elapsed
  const slowed = zombie.status.slowUntil > elapsed
  const movement = frozen ? 0 : zombie.speed * (slowed ? 0.55 : 1) * deltaSeconds
  return {
    ...zombie,
    hp: zombie.hp - burnDamage,
    pathProgress: zombie.pathProgress + movement,
    status: { ...zombie.status, burns },
  }
})

const spawnZombies = (state: GameState, deltaSeconds: number) => {
  if (state.wave.spawned >= state.wave.total) return state
  const definition = getWaveDefinition(state.wave.index)
  let cooldown = state.wave.spawnCooldown - deltaSeconds
  let spawned = state.wave.spawned
  let nextId = state.nextId
  const zombies = [...state.zombies]
  while (spawned < state.wave.total && cooldown <= 0) {
    const kind = definition.sequence[spawned]
    zombies.push(createZombie(kind, definition, `zombie-${nextId}`, nextId))
    nextId += 1
    spawned += 1
    cooldown += definition.spawnInterval
  }
  const spawnedState = { ...state, zombies, nextId, wave: { ...state.wave, spawned, spawnCooldown: cooldown } }
  return state.wave.spawned === 0 && spawned > 0
    ? applySunflowerWaveStartEffects(spawnedState)
    : spawnedState
}

const createProjectile = (
  id: string,
  kind: Projectile['kind'],
  plant: Plant,
  origin: Point,
  target: Zombie,
  damage: number,
  range: number,
): Projectile => {
  const targetPoint = samplePath(target.pathProgress)
  const targetDistance = Math.max(0.001, distance(origin, targetPoint))
  const direction = { x: (targetPoint.x - origin.x) / targetDistance, y: (targetPoint.y - origin.y) / targetDistance }
  return {
    id,
    kind,
    plantId: plant.id,
    targetId: kind === 'watermelon' ? null : target.id,
    position: { ...origin },
    targetPoint: kind === 'banana' ? { ...origin } : targetPoint,
    direction,
    progress: 0,
    returning: false,
    damage,
    hitIds: [],
    ttl: kind === 'watermelon' ? WATERMELON_FLIGHT_SECONDS : kind === 'banana' ? (range * 2) / BANANA_SPEED + 0.3 : 3,
  }
}

interface PlantActionResult {
  plants: Plant[]
  projectiles: Projectile[]
  hits: DamageEvent[]
  sun: number
  feedback: Feedback[]
  nextId: number
}

const runPlantActions = (state: GameState, deltaSeconds: number): PlantActionResult => {
  let nextId = state.nextId
  let sun = state.sun
  const projectiles = [...state.projectiles]
  const hits: DamageEvent[] = []
  const feedback: Feedback[] = []
  const plants = state.plants.map((plant) => {
    const origin = getPlantPosition(plant, state.pots)
    if (!origin) return plant

    if (plant.kind === 'sunflower') {
      // The economy module owns production ticks so one frame cannot award twice.
      return plant
    }

    const attackCooldown = Math.max(0, plant.attackCooldown - deltaSeconds)
    if (attackCooldown > 0) return { ...plant, attackCooldown }
    const range = getPlantRange(plant)
    const target = selectTarget(state.zombies, origin, range)
    if (!target) return { ...plant, attackCooldown: 0 }

    const modifiers = getAttackModifiers(plant)
    const damage = getPlantDamage(plant) * modifiers.damageMultiplier
    const interval = getPlantAttackInterval(plant) * modifiers.intervalMultiplier
    if (plant.kind === 'durian') {
      state.zombies.forEach((zombie) => {
        if (zombie.hp > 0 && distance(origin, samplePath(zombie.pathProgress)) <= range) {
          hits.push({ zombieId: zombie.id, plantId: plant.id, damage, kind: 'durian' })
        }
      })
      feedback.push({ id: `feedback-${nextId++}`, text: '重击！', tone: 'damage', position: origin, ttl: 0.5 })
    } else {
      projectiles.push(createProjectile(`projectile-${nextId++}`, plant.kind, plant, origin, target, damage, range))
    }
    return { ...plant, attackCooldown: interval }
  })
  return { plants, projectiles, hits, sun, feedback, nextId }
}

interface ProjectileResult {
  projectiles: Projectile[]
  hits: DamageEvent[]
}

const advanceProjectiles = (projectiles: Projectile[], state: GameState, deltaSeconds: number): ProjectileResult => {
  const hits: DamageEvent[] = []
  const survivors: Projectile[] = []
  projectiles.forEach((projectile) => {
    if (projectile.kind === 'pea') {
      const target = state.zombies.find((zombie) => zombie.id === projectile.targetId && zombie.hp > 0)
      if (!target) return
      const targetPoint = samplePath(target.pathProgress)
      const gap = distance(projectile.position, targetPoint)
      const travel = PROJECTILE_SPEED * deltaSeconds
      if (gap <= travel + ZOMBIE_HIT_RADIUS) {
        hits.push({ zombieId: target.id, plantId: projectile.plantId, damage: projectile.damage, kind: 'pea' })
        return
      }
      const ratio = travel / gap
      survivors.push({
        ...projectile,
        position: {
          x: projectile.position.x + (targetPoint.x - projectile.position.x) * ratio,
          y: projectile.position.y + (targetPoint.y - projectile.position.y) * ratio,
        },
        targetPoint,
        ttl: projectile.ttl - deltaSeconds,
      })
      return
    }

    if (projectile.kind === 'watermelon') {
      const ttl = projectile.ttl - deltaSeconds
      if (ttl <= 0) {
        state.zombies.forEach((zombie) => {
          if (zombie.hp > 0 && distance(projectile.targetPoint, samplePath(zombie.pathProgress)) <= WATERMELON_BLAST_RADIUS) {
            hits.push({ zombieId: zombie.id, plantId: projectile.plantId, damage: projectile.damage, kind: 'watermelon' })
          }
        })
        return
      }
      const ratio = clamp(deltaSeconds / projectile.ttl, 0, 1)
      survivors.push({
        ...projectile,
        position: {
          x: projectile.position.x + (projectile.targetPoint.x - projectile.position.x) * ratio,
          y: projectile.position.y + (projectile.targetPoint.y - projectile.position.y) * ratio,
        },
        progress: projectile.progress + deltaSeconds / WATERMELON_FLIGHT_SECONDS,
        ttl,
      })
      return
    }

    const plant = state.plants.find((candidate) => candidate.id === projectile.plantId)
    const range = plant ? getPlantRange(plant) : Math.max(projectile.progress, 1)
    const previousPosition = projectile.position
    let progress = projectile.progress
    let returning = projectile.returning
    if (returning) progress = Math.max(0, progress - BANANA_SPEED * deltaSeconds)
    else {
      progress = Math.min(range, progress + BANANA_SPEED * deltaSeconds)
      if (progress >= range) returning = true
    }
    const position = {
      x: projectile.targetPoint.x + projectile.direction.x * progress,
      y: projectile.targetPoint.y + projectile.direction.y * progress,
    }
    const hitIds = [...projectile.hitIds]
    state.zombies.forEach((zombie) => {
      if (zombie.hp <= 0) return
      const hitCount = hitIds.filter((id) => id === zombie.id).length
      const canHit = returning ? hitCount < 2 : hitCount === 0
      if (canHit && pointToSegmentDistance(samplePath(zombie.pathProgress), previousPosition, position) <= ZOMBIE_HIT_RADIUS) {
        hits.push({ zombieId: zombie.id, plantId: projectile.plantId, damage: projectile.damage, kind: 'banana' })
        if (returning) {
          while (hitIds.filter((id) => id === zombie.id).length < 2) hitIds.push(zombie.id)
        } else hitIds.push(zombie.id)
      }
    })
    if (returning && progress <= 0) return
    survivors.push({ ...projectile, position, progress, returning, hitIds, ttl: projectile.ttl - deltaSeconds })
  })
  return { projectiles: survivors.filter((projectile) => projectile.ttl > 0), hits }
}

const applyDamageAndSettle = (state: GameState, hits: DamageEvent[]) => {
  const hitMap = new Map<string, DamageEvent[]>()
  hits.forEach((hit) => hitMap.set(hit.zombieId, [...(hitMap.get(hit.zombieId) ?? []), hit]))
  let nextId = state.nextId
  let sun = state.sun
  let lives = state.lives
  const feedback: Feedback[] = []
  const surviving: Zombie[] = []

  state.zombies.forEach((zombie) => {
    let nextZombie = zombie
    for (const hit of hitMap.get(zombie.id) ?? []) {
      const plant = hit.plantId ? state.plants.find((candidate) => candidate.id === hit.plantId) : null
      nextZombie = plant
        ? applyWeaponHit(nextZombie, plant, hit.damage, state.elapsed)
        : { ...nextZombie, hp: Math.max(0, nextZombie.hp - hit.damage) }
      feedback.push({
        id: `feedback-${nextId++}`,
        text: `-${Math.round(hit.damage)}`,
        tone: 'damage',
        position: samplePath(nextZombie.pathProgress),
        ttl: 0.55,
      })
    }

    if (nextZombie.hp <= 0) {
      sun += nextZombie.reward
      feedback.push({
        id: `feedback-${nextId++}`,
        text: `击杀 +${nextZombie.reward} 阳光`,
        tone: 'sun',
        position: samplePath(nextZombie.pathProgress),
        ttl: 0.9,
      })
    } else if (nextZombie.pathProgress >= PATH_LENGTH) {
      lives = Math.max(0, lives - nextZombie.threat)
      feedback.push({ id: `feedback-${nextId++}`, text: `核心 -${nextZombie.threat}`, tone: 'danger', ttl: 1.2 })
    } else surviving.push(nextZombie)
  })
  return { ...state, zombies: surviving, sun, lives, feedback: [...state.feedback, ...feedback], nextId }
}

const settleWave = (state: GameState): GameState => {
  if (state.lives <= 0) {
    return {
      ...state,
      phase: 'defeat',
      paused: false,
      zombies: [],
      projectiles: [],
      feedback: [...state.feedback, { id: `feedback-${state.nextId}`, text: '果园核心失守', tone: 'danger', ttl: 3 }],
      nextId: state.nextId + 1,
    }
  }
  if (state.wave.spawned < state.wave.total || state.zombies.length > 0) return state
  if (state.wave.index >= MAX_WAVES) {
    return {
      ...state,
      phase: 'victory',
      paused: false,
      projectiles: [],
      feedback: [...state.feedback, { id: `feedback-${state.nextId}`, text: '十五波全部守住！', tone: 'sun', ttl: 3 }],
      nextId: state.nextId + 1,
    }
  }
  const definition = getWaveDefinition(state.wave.index)
  const completed: GameState = {
    ...state,
    phase: 'between-waves',
    sun: state.sun + definition.reward,
    projectiles: [],
    wave: { ...state.wave, betweenTimer: BETWEEN_WAVE_SECONDS, started: false },
    feedback: [...state.feedback, {
      id: `feedback-${state.nextId}`,
      text: `波次完成 +${definition.reward} 阳光`,
      tone: 'sun',
      ttl: 1.8,
    }],
    nextId: state.nextId + 1,
  }
  return grantMilestoneReward(completed, state.wave.index)
}

export const stepBattle = (state: GameState, deltaSeconds: number): GameState => {
  if (state.phase === 'between-waves') {
    const betweenTimer = Math.max(0, state.wave.betweenTimer - deltaSeconds)
    const waiting = { ...state, wave: { ...state.wave, betweenTimer } }
    return betweenTimer <= 0 ? beginWave(waiting, state.wave.index + 1) : waiting
  }
  if (state.phase !== 'playing') return state

  let next: GameState = {
    ...state,
    zombies: advanceStatusesAndMovement(state.zombies, state.elapsed, deltaSeconds),
  }
  next = spawnZombies(next, deltaSeconds)

  const actions = runPlantActions(next, deltaSeconds)
  next = {
    ...next,
    plants: actions.plants,
    projectiles: actions.projectiles,
    sun: actions.sun,
    feedback: [...next.feedback, ...actions.feedback],
    nextId: actions.nextId,
  }
  const projectileResult = advanceProjectiles(next.projectiles, next, deltaSeconds)
  next = { ...next, projectiles: projectileResult.projectiles }
  next = applyDamageAndSettle(next, [...actions.hits, ...projectileResult.hits])
  return settleWave(next)
}

export const reduceBattle = (state: GameState, command: GameCommand): GameState => {
  if (command.type !== 'start-wave') return state
  if (state.phase === 'ready') return beginWave(state, 1)
  if (state.phase === 'between-waves') return beginWave(state, state.wave.index + 1)
  return state
}
