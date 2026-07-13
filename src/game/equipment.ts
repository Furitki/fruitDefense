import { EXPANSION_CANDIDATES, PATH_POINTS, PLANT_CONFIG, PLANTING_CELLS } from './config'
import type {
  GameCommand,
  GameState,
  GridCoordinate,
  Inventory,
  Plant,
  PlantKind,
  Point,
  Pot,
  WeaponKind,
  Zombie,
} from './types'

export const GATLING_ATTACK_SPEED_MULTIPLIER = 1.8
export const GATLING_DAMAGE_MULTIPLIER = 0.75
export const ICE_SLOW_MULTIPLIER = 0.7
export const ICE_SLOW_DURATION = 2
export const ICE_HITS_TO_FREEZE = 5
export const ICE_FREEZE_DURATION = 1
export const CHILI_BURN_DAMAGE_MULTIPLIER = 0.2
export const CHILI_BURN_DURATION = 3
export const CHILI_MAX_STACKS = 3
export const SUNFLOWER_CHILI_BONUS_SUN = 1

export interface EquipmentMilestoneReward {
  weapons: Partial<Record<WeaponKind, number>>
  pots: number
}

/**
 * Milestones deliberately grant a pot alongside each first weapon so every
 * equipment mechanic can be tried during one run. Wave 12 is a final refill.
 */
export const EQUIPMENT_MILESTONE_REWARDS: Readonly<Record<number, EquipmentMilestoneReward>> = {
  3: { weapons: { gatling: 1 }, pots: 1 },
  6: { weapons: { ice: 1 }, pots: 1 },
  9: { weapons: { chili: 1 }, pots: 1 },
  12: { weapons: { gatling: 1, ice: 1, chili: 1 }, pots: 1 },
}

export interface AttackModifiers {
  damageMultiplier: number
  intervalMultiplier: number
}

export interface SunflowerModifiers {
  productionIntervalMultiplier: number
  bonusSunPerProduction: number
  waveStartSlowMultiplier: number
  waveStartSlowDuration: number
}

export interface CoveragePreview {
  origin: Point
  radiusInGrid: number
  direction: Point | null
}

const cloneInventory = (inventory: Inventory): Inventory => ({
  weapons: { ...inventory.weapons },
  pots: inventory.pots,
})

const samePoint = (left: Point, right: Point) => left.x === right.x && left.y === right.y

const appendFeedback = (
  state: GameState,
  text: string,
  tone: 'sun' | 'damage' | 'info' | 'danger',
  position?: Point,
): GameState => ({
  ...state,
  feedback: [
    ...state.feedback,
    { id: `equipment-feedback-${state.nextId}`, text, tone, position, ttl: 1.8 },
  ],
  nextId: state.nextId + 1,
})

export const getAttackModifiers = (plant: Plant): AttackModifiers => {
  if (plant.kind !== 'sunflower' && plant.weapon === 'gatling') {
    return {
      damageMultiplier: GATLING_DAMAGE_MULTIPLIER,
      intervalMultiplier: 1 / GATLING_ATTACK_SPEED_MULTIPLIER,
    }
  }
  return { damageMultiplier: 1, intervalMultiplier: 1 }
}

export const getSunflowerModifiers = (plant: Plant): SunflowerModifiers => {
  if (plant.kind !== 'sunflower') {
    return {
      productionIntervalMultiplier: 1,
      bonusSunPerProduction: 0,
      waveStartSlowMultiplier: 1,
      waveStartSlowDuration: 0,
    }
  }

  if (plant.weapon === 'gatling') {
    return {
      productionIntervalMultiplier: 1 / GATLING_ATTACK_SPEED_MULTIPLIER,
      bonusSunPerProduction: 0,
      waveStartSlowMultiplier: 1,
      waveStartSlowDuration: 0,
    }
  }
  if (plant.weapon === 'ice') {
    return {
      productionIntervalMultiplier: 1,
      bonusSunPerProduction: 0,
      waveStartSlowMultiplier: ICE_SLOW_MULTIPLIER,
      waveStartSlowDuration: ICE_SLOW_DURATION,
    }
  }
  if (plant.weapon === 'chili') {
    return {
      productionIntervalMultiplier: 1,
      bonusSunPerProduction: SUNFLOWER_CHILI_BONUS_SUN,
      waveStartSlowMultiplier: 1,
      waveStartSlowDuration: 0,
    }
  }
  return {
    productionIntervalMultiplier: 1,
    bonusSunPerProduction: 0,
    waveStartSlowMultiplier: 1,
    waveStartSlowDuration: 0,
  }
}

/**
 * Single hit entry point shared by every attack shape. The caller passes the
 * already star/weapon-adjusted hit damage; this function owns damage plus the
 * weapon status mutation so area attacks cannot accidentally use other rules.
 */
export const applyWeaponHit = (
  zombie: Zombie,
  plant: Plant,
  damage: number,
  elapsed: number,
): Zombie => {
  const next: Zombie = { ...zombie, hp: Math.max(0, zombie.hp - Math.max(0, damage)) }

  if (plant.weapon === 'ice' && plant.kind !== 'sunflower') {
    const iceHits = zombie.status.iceHits + 1
    const freezes = iceHits >= ICE_HITS_TO_FREEZE
    return {
      ...next,
      status: {
        ...zombie.status,
        iceHits: freezes ? 0 : iceHits,
        slowUntil: Math.max(zombie.status.slowUntil, elapsed + ICE_SLOW_DURATION),
        freezeUntil: freezes
          ? Math.max(zombie.status.freezeUntil, elapsed + ICE_FREEZE_DURATION)
          : zombie.status.freezeUntil,
        burns: zombie.status.burns.map((burn) => ({ ...burn })),
      },
    }
  }

  if (plant.weapon === 'chili' && plant.kind !== 'sunflower') {
    const burn = {
      remaining: CHILI_BURN_DURATION,
      damagePerSecond: Math.max(0, damage) * CHILI_BURN_DAMAGE_MULTIPLIER,
    }
    return {
      ...next,
      status: {
        ...zombie.status,
        burns: [...zombie.status.burns, burn].slice(-CHILI_MAX_STACKS),
      },
    }
  }

  return next
}

/** Call exactly once when a wave begins. */
export const applySunflowerWaveStartEffects = (state: GameState): GameState => {
  const iceSunflowers = state.plants.filter(
    (plant) => plant.kind === 'sunflower' && plant.weapon === 'ice' && plant.potId !== null,
  ).length
  if (iceSunflowers === 0 || state.zombies.length === 0) return state

  return {
    ...state,
    zombies: state.zombies.map((zombie) => ({
      ...zombie,
      status: {
        ...zombie.status,
        slowUntil: Math.max(zombie.status.slowUntil, state.elapsed + ICE_SLOW_DURATION),
      },
    })),
  }
}

export const describeWeaponEffect = (weapon: WeaponKind, plantKind: PlantKind): string => {
  if (plantKind === 'sunflower') {
    if (weapon === 'gatling') return '生产速度 +80%'
    if (weapon === 'ice') return '波次开始时全场减速 30%，持续 2 秒'
    return `每次生产额外获得 ${SUNFLOWER_CHILI_BONUS_SUN} 阳光`
  }
  if (weapon === 'gatling') return '攻速 +80%，单次伤害 -25%，范围不变'
  if (weapon === 'ice') return '命中减速 30%（2 秒），5 次命中冻结 1 秒'
  return '燃烧每秒造成本次伤害 20%，持续 3 秒，最多 3 层'
}

export const canInstallWeapon = (state: GameState, weapon: WeaponKind, plantId: string): boolean => {
  const plant = state.plants.find((candidate) => candidate.id === plantId)
  return Boolean(plant && plant.weapon === null && state.inventory.weapons[weapon] > 0)
}

export const installWeapon = (state: GameState, weapon: WeaponKind, plantId: string): GameState => {
  const plant = state.plants.find((candidate) => candidate.id === plantId)
  if (!plant) return appendFeedback(state, '找不到这株植物', 'danger')
  if (plant.weapon !== null) return appendFeedback(state, '每株植物只能安装一种武器', 'danger')
  if (state.inventory.weapons[weapon] <= 0) return appendFeedback(state, '武器库存不足', 'danger')

  const inventory = cloneInventory(state.inventory)
  inventory.weapons[weapon] -= 1
  return appendFeedback({
    ...state,
    inventory,
    plants: state.plants.map((candidate) => (
      candidate.id === plantId ? { ...candidate, weapon } : candidate
    )),
    selection: { type: 'plant', id: plantId },
  }, `${describeWeaponEffect(weapon, plant.kind)}，立即生效`, 'info')
}

export const grantMilestoneReward = (state: GameState, completedWave: number): GameState => {
  const reward = EQUIPMENT_MILESTONE_REWARDS[completedWave]
  if (!reward) return state
  const inventory = cloneInventory(state.inventory)
  for (const weapon of ['gatling', 'ice', 'chili'] as const) {
    inventory.weapons[weapon] += reward.weapons[weapon] ?? 0
  }
  inventory.pots += reward.pots

  const rewards = (['gatling', 'ice', 'chili'] as const)
    .filter((weapon) => (reward.weapons[weapon] ?? 0) > 0)
    .map((weapon) => `${weapon === 'gatling' ? '机枪' : weapon === 'ice' ? '冰块' : '辣椒'}×${reward.weapons[weapon]}`)
  if (reward.pots > 0) rewards.push(`花盆×${reward.pots}`)
  return appendFeedback({ ...state, inventory }, `里程碑奖励：${rewards.join('、')}`, 'sun')
}

export const isExpansionCandidate = (point: Point): boolean => (
  EXPANSION_CANDIDATES.some((candidate) => samePoint(candidate, point))
)

const expansionCellFor = (point: Point) => (
  EXPANSION_CANDIDATES.find((candidate) => samePoint(candidate, point)) ?? null
)

const gridCoordinateOf = (point: Point & Partial<GridCoordinate>): GridCoordinate | null => {
  if (typeof point.column === 'number' && typeof point.row === 'number') {
    return { column: point.column, row: point.row }
  }
  const cell = PLANTING_CELLS.find((candidate) => samePoint(candidate, point))
  return cell ? { column: cell.column, row: cell.row } : null
}

export const isOrthogonallyAdjacent = (
  left: Point & Partial<GridCoordinate>,
  right: Point & Partial<GridCoordinate>,
): boolean => {
  const leftGrid = gridCoordinateOf(left)
  const rightGrid = gridCoordinateOf(right)
  if (!leftGrid || !rightGrid) return false
  return Math.abs(leftGrid.column - rightGrid.column) + Math.abs(leftGrid.row - rightGrid.row) === 1
}

export const canExpandPot = (state: GameState, point: Point): boolean => {
  const cell = expansionCellFor(point)
  if (state.inventory.pots <= 0 || !cell) return false
  if (state.pots.some((pot) => pot.active && samePoint(pot, cell))) return false
  return state.pots.some((pot) => pot.active && isOrthogonallyAdjacent(pot, cell))
}

export const getLegalExpansionCandidates = (state: GameState): Point[] => (
  EXPANSION_CANDIDATES.filter((candidate) => canExpandPot(state, candidate))
)

export const expandPot = (state: GameState, point: Point): GameState => {
  const cell = expansionCellFor(point)
  if (state.inventory.pots <= 0) return appendFeedback(state, '没有可用花盆', 'danger', point)
  if (!cell) return appendFeedback(state, '道路、核心和非候选地块不能扩建', 'danger', point)
  if (state.pots.some((pot) => pot.active && samePoint(pot, cell))) {
    return appendFeedback(state, '这里已经有花盆', 'danger', cell)
  }
  if (!state.pots.some((pot) => pot.active && isOrthogonallyAdjacent(pot, cell))) {
    return appendFeedback(state, '只能扩建到现有花盆的上下左右', 'danger', cell)
  }

  const inventory = cloneInventory(state.inventory)
  inventory.pots -= 1
  const existing = state.pots.find((pot) => samePoint(pot, cell))
  const id = existing?.id ?? `pot-expansion-${cell.column}-${cell.row}`
  const pots: Pot[] = existing
    ? state.pots.map((pot) => samePoint(pot, cell) ? { ...pot, active: true } : pot)
    : [...state.pots, { id, ...cell, active: true }]

  return appendFeedback({
    ...state,
    inventory,
    pots,
    selection: { type: 'pot', id },
  }, '花盆扩建完成，可立即种植', 'info', cell)
}

export const getEstimatedBananaDirection = (origin: Point): Point => {
  const closest = PATH_POINTS.reduce((best, point) => {
    const distance = (point.x - origin.x) ** 2 + (point.y - origin.y) ** 2
    return distance < best.distance ? { point, distance } : best
  }, { point: PATH_POINTS[0], distance: Number.POSITIVE_INFINITY })
  const dx = closest.point.x - origin.x
  const dy = closest.point.y - origin.y
  const length = Math.hypot(dx, dy) || 1
  return { x: dx / length, y: dy / length }
}

export const getCoveragePreview = (
  kind: PlantKind,
  origin: Point,
  facing?: Point,
): CoveragePreview | null => {
  if (kind === 'sunflower') return null
  const radiusInGrid = kind === 'banana' ? 3.5 : kind === 'durian' ? 1.3 : 4
  return {
    origin: { ...origin },
    radiusInGrid,
    direction: kind === 'banana' ? (facing ?? getEstimatedBananaDirection(origin)) : null,
  }
}

/** Convert the grid-space design preview to the board's percentage radius. */
export const getCoverageRadiusPercent = (kind: PlantKind): number => {
  const configuredRange = PLANT_CONFIG[kind].range
  if (kind === 'sunflower') return 0
  // Existing combat ranges already use board percentages; keeping one scale
  // prevents preview and actual targeting from drifting apart.
  return configuredRange
}

export const stepEquipment = (state: GameState, _deltaSeconds: number): GameState => state

export const reduceEquipment = (state: GameState, command: GameCommand): GameState => {
  if (command.type === 'install-weapon') return installWeapon(state, command.weapon, command.plantId)
  if (command.type === 'expand-pot') return expandPot(state, { x: command.x, y: command.y })
  return state
}
