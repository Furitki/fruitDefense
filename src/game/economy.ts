import {
  PLANT_CONFIG,
  PLANT_KINDS,
  STAR_DAMAGE,
  STAR_RANGE,
  STAR_SPEED,
  SUNFLOWER_INTERVAL,
  SUNFLOWER_YIELD,
  refreshCost,
} from './config'
import { getSunflowerModifiers } from './equipment'
import { nextRandom } from './state'
import type { Feedback, GameCommand, GameState, Plant, PlantKind, Point, Star } from './types'

export const NURSERY_SIZE = 5
export const MAX_PLANT_STAR: Star = 4
export const MOVE_COOLDOWN_SECONDS = 2
export const DEFAULT_WAVE_REWARD = 5

const ATTACKING_PLANT_KINDS = PLANT_KINDS.filter((kind) => PLANT_CONFIG[kind].damage > 0)

export type EconomyEvent =
  | { type: 'zombie-killed'; reward: number; position?: Point }
  | { type: 'wave-completed'; reward?: number; wave?: number }
  | { type: 'sunflower-produced'; amount: number; plantId: string }

export type PlantDropAction = 'plant' | 'move' | 'merge' | 'cancel' | 'invalid'

export interface PlantDropStatus {
  legal: boolean
  action: PlantDropAction
  reason: string
}

const addFeedback = (
  state: GameState,
  text: string,
  tone: Feedback['tone'] = 'info',
  position?: Point,
): GameState => ({
  ...state,
  nextId: state.nextId + 1,
  feedback: [
    ...state.feedback,
    { id: `feedback-${state.nextId}`, text, tone, position, ttl: 2 },
  ],
})

const createPlant = (id: string, kind: PlantKind, nurseryIndex: number): Plant => ({
  id,
  kind,
  star: 1,
  potId: null,
  nurseryIndex,
  weapon: null,
  attackCooldown: 0,
  productionProgress: 0,
  moveCooldown: 0,
  facing: { x: 0, y: -1 },
})

const drawKind = (seed: number, kinds: PlantKind[]): [PlantKind, number] => {
  const [value, nextSeed] = nextRandom(seed)
  return [kinds[Math.floor(value * kinds.length)] ?? kinds[0], nextSeed]
}

const createNurseryBatch = (
  seed: number,
  firstBatch: boolean,
): { kinds: PlantKind[]; seed: number } => {
  const kinds: PlantKind[] = []
  let cursor = seed
  let sunflowerCount = 0

  while (kinds.length < NURSERY_SIZE) {
    const forceAttacker = firstBatch && kinds.length < 2
    const pool = forceAttacker || sunflowerCount >= 2
      ? ATTACKING_PLANT_KINDS
      : PLANT_KINDS
    const [kind, nextSeed] = drawKind(cursor, pool)
    cursor = nextSeed
    kinds.push(kind)
    if (kind === 'sunflower') sunflowerCount += 1
  }

  // Keep the guaranteed attackers from always occupying the first two slots.
  for (let index = kinds.length - 1; index > 0; index -= 1) {
    const [value, nextSeed] = nextRandom(cursor)
    cursor = nextSeed
    const target = Math.floor(value * (index + 1))
    ;[kinds[index], kinds[target]] = [kinds[target], kinds[index]]
  }

  return { kinds, seed: cursor }
}

export const nurseryPlants = (state: GameState) => state.plants
  .filter((plant) => plant.nurseryIndex !== null)
  .sort((left, right) => (left.nurseryIndex ?? 0) - (right.nurseryIndex ?? 0))

export const currentRefreshCost = (state: Pick<GameState, 'refreshCount'>) => refreshCost(state.refreshCount)

export const getRefreshBlockReason = (state: GameState): string | null => {
  if (nurseryPlants(state).length > 0) return '苗圃还有植物，请先种植或合成'
  const cost = currentRefreshCost(state)
  if (state.sun < cost) return `阳光不足，还需要 ${cost - state.sun}`
  return null
}

export const getPlantStats = (plant: Plant) => {
  const config = PLANT_CONFIG[plant.kind]
  if (plant.kind === 'sunflower') {
    const modifiers = getSunflowerModifiers(plant)
    return {
      damage: 0,
      interval: SUNFLOWER_INTERVAL[plant.star] * modifiers.productionIntervalMultiplier,
      range: 0,
      production: SUNFLOWER_YIELD[plant.star] + modifiers.bonusSunPerProduction,
    }
  }
  return {
    damage: config.damage * STAR_DAMAGE[plant.star],
    interval: config.interval / STAR_SPEED[plant.star],
    range: config.range * STAR_RANGE[plant.star],
    production: 0,
  }
}

export const getPlantDropStatus = (
  state: GameState,
  plantId: string,
  potId: string,
): PlantDropStatus => {
  const plant = state.plants.find((candidate) => candidate.id === plantId)
  if (!plant) return { legal: false, action: 'invalid', reason: '植物不存在' }

  const pot = state.pots.find((candidate) => candidate.id === potId)
  if (!pot?.active) return { legal: false, action: 'invalid', reason: '这里不是可用花盆' }
  if (plant.potId === potId) return { legal: false, action: 'cancel', reason: '植物已在这个花盆中' }
  if (plant.potId !== null && state.phase === 'playing' && plant.moveCooldown > 0) {
    return { legal: false, action: 'invalid', reason: `移动冷却 ${plant.moveCooldown.toFixed(1)} 秒` }
  }

  const target = state.plants.find((candidate) => candidate.potId === potId)
  if (!target) {
    return {
      legal: true,
      action: plant.nurseryIndex !== null ? 'plant' : 'move',
      reason: plant.nurseryIndex !== null ? '可种植' : '可移动',
    }
  }
  if (target.kind !== plant.kind || target.star !== plant.star) {
    return { legal: false, action: 'invalid', reason: '只能合成同种类、同星级植物' }
  }
  if (target.star >= MAX_PLANT_STAR) {
    return { legal: false, action: 'invalid', reason: '植物已达到四星' }
  }
  return { legal: true, action: 'merge', reason: `可合成为 ${target.star + 1} 星` }
}

export const createPlantDropCommand = (
  state: GameState,
  plantId: string,
  potId: string,
): GameCommand | null => {
  const status = getPlantDropStatus(state, plantId, potId)
  if (!status.legal) return null
  return status.action === 'plant'
    ? { type: 'place-plant', plantId, potId }
    : { type: 'move-or-merge', plantId, potId }
}

const refreshNursery = (state: GameState): GameState => {
  const reason = getRefreshBlockReason(state)
  if (reason) return addFeedback(state, reason, 'danger')

  const cost = currentRefreshCost(state)
  const batch = createNurseryBatch(state.randomSeed, state.refreshCount === 0)
  const plants = batch.kinds.map((kind, index) => createPlant(`plant-${state.nextId + index}`, kind, index))
  const refreshed: GameState = {
    ...state,
    sun: state.sun - cost,
    refreshCount: state.refreshCount + 1,
    plants: [...state.plants, ...plants],
    randomSeed: batch.seed,
    nextId: state.nextId + plants.length,
    selection: null,
  }
  return addFeedback(refreshed, `刷新植物 -${cost} 阳光`, 'sun')
}

const placePlant = (state: GameState, plantId: string, potId: string): GameState => {
  const plant = state.plants.find((candidate) => candidate.id === plantId)
  if (!plant || plant.nurseryIndex === null) return addFeedback(state, '只能从苗圃种植植物', 'danger')

  const status = getPlantDropStatus(state, plantId, potId)
  if (!status.legal || status.action !== 'plant') return addFeedback(state, status.reason, 'danger')

  return {
    ...state,
    selection: null,
    plants: state.plants.map((candidate) => candidate.id === plantId
      ? { ...candidate, nurseryIndex: null, potId }
      : candidate),
  }
}

const moveOrMergePlant = (state: GameState, plantId: string, potId: string): GameState => {
  const plant = state.plants.find((candidate) => candidate.id === plantId)
  if (!plant) return addFeedback(state, '植物不存在', 'danger')

  const status = getPlantDropStatus(state, plantId, potId)
  if (!status.legal) return addFeedback(state, status.reason, status.action === 'cancel' ? 'info' : 'danger')

  if (status.action === 'plant' || status.action === 'move') {
    const movingOnBattlefield = plant.nurseryIndex === null && state.phase === 'playing'
    return {
      ...state,
      selection: null,
      plants: state.plants.map((candidate) => candidate.id === plantId
        ? {
            ...candidate,
            potId,
            nurseryIndex: null,
            attackCooldown: 0,
            moveCooldown: movingOnBattlefield ? MOVE_COOLDOWN_SECONDS : 0,
          }
        : candidate),
    }
  }

  const target = state.plants.find((candidate) => candidate.potId === potId)
  if (!target || target.star >= MAX_PLANT_STAR) return addFeedback(state, '无法合成植物', 'danger')
  const nextStar = (target.star + 1) as Star
  const inventory = plant.weapon
    ? {
        ...state.inventory,
        weapons: {
          ...state.inventory.weapons,
          [plant.weapon]: state.inventory.weapons[plant.weapon] + 1,
        },
      }
    : state.inventory
  const merged: GameState = {
    ...state,
    inventory,
    selection: null,
    plants: state.plants
      .filter((candidate) => candidate.id !== plantId)
      .map((candidate) => candidate.id === target.id
        ? {
            ...candidate,
            star: nextStar,
            attackCooldown: 0,
            productionProgress: 0,
            facing: { x: 0, y: -1 },
          }
        : candidate),
  }
  let result = addFeedback(merged, `${PLANT_CONFIG[target.kind].name} 升至 ${nextStar} 星`, 'info')
  if (plant.weapon) result = addFeedback(result, '来源植物的武器已回收', 'info')
  return result
}

export const applyEconomyEvent = (state: GameState, event: EconomyEvent): GameState => {
  if (event.type === 'zombie-killed') {
    if (event.reward <= 0) return state
    return addFeedback({ ...state, sun: state.sun + event.reward }, `击杀 +${event.reward} 阳光`, 'sun', event.position)
  }
  if (event.type === 'wave-completed') {
    const reward = event.reward ?? DEFAULT_WAVE_REWARD
    if (reward <= 0) return state
    return addFeedback({ ...state, sun: state.sun + reward }, `波次奖励 +${reward} 阳光`, 'sun')
  }
  if (event.amount <= 0) return state
  return addFeedback({ ...state, sun: state.sun + event.amount }, `向日葵 +${event.amount} 阳光`, 'sun')
}

export const stepEconomy = (state: GameState, deltaSeconds: number): GameState => {
  if (deltaSeconds <= 0) return state

  const productions: Array<{ plantId: string; amount: number }> = []
  const plants = state.plants.map((plant) => {
    if (plant.kind !== 'sunflower' || plant.potId === null) return plant
    const modifiers = getSunflowerModifiers(plant)
    const interval = SUNFLOWER_INTERVAL[plant.star] * modifiers.productionIntervalMultiplier
    const progress = plant.productionProgress + deltaSeconds
    const cycles = Math.floor(progress / interval)
    if (cycles === 0) return { ...plant, productionProgress: progress }
    const sunPerCycle = SUNFLOWER_YIELD[plant.star] + modifiers.bonusSunPerProduction
    productions.push({ plantId: plant.id, amount: cycles * sunPerCycle })
    return { ...plant, productionProgress: progress - cycles * interval }
  })

  let next = plants.some((plant, index) => plant !== state.plants[index]) ? { ...state, plants } : state
  for (const production of productions) {
    next = applyEconomyEvent(next, { type: 'sunflower-produced', ...production })
  }
  return next
}

export const reduceEconomy = (state: GameState, command: GameCommand): GameState => {
  if (command.type === 'refresh') return refreshNursery(state)
  if (command.type === 'place-plant') return placePlant(state, command.plantId, command.potId)
  if (command.type === 'move-or-merge') return moveOrMergePlant(state, command.plantId, command.potId)
  return state
}
