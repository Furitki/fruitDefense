import { describe, expect, it } from 'vitest'
import {
  DEFAULT_WAVE_REWARD,
  MOVE_COOLDOWN_SECONDS,
  applyEconomyEvent,
  createPlantDropCommand,
  currentRefreshCost,
  getPlantDropStatus,
  getPlantStats,
  getRefreshBlockReason,
  reduceEconomy,
  stepEconomy,
} from './economy'
import { createInitialState } from './state'
import type { GameState, Plant, PlantKind, Star, WeaponKind } from './types'

const makePlant = ({
  id,
  kind = 'pea',
  star = 1,
  potId = null,
  nurseryIndex = null,
  weapon = null,
  moveCooldown = 0,
  productionProgress = 0,
}: {
  id: string
  kind?: PlantKind
  star?: Star
  potId?: string | null
  nurseryIndex?: number | null
  weapon?: WeaponKind | null
  moveCooldown?: number
  productionProgress?: number
}): Plant => ({
  id,
  kind,
  star,
  potId,
  nurseryIndex,
  weapon,
  moveCooldown,
  productionProgress,
  attackCooldown: 1,
  facing: { x: 1, y: 0 },
})

describe('plant economy refresh', () => {
  it('creates a deterministic constrained batch of five plants', () => {
    const initial = createInitialState(42)
    const first = reduceEconomy(initial, { type: 'refresh' })
    const replay = reduceEconomy(createInitialState(42), { type: 'refresh' })

    expect(first.sun).toBe(initial.sun - 10)
    expect(first.refreshCount).toBe(1)
    expect(first.plants).toHaveLength(5)
    expect(first.plants.map((plant) => plant.kind)).toEqual(replay.plants.map((plant) => plant.kind))
    expect(first.plants.filter((plant) => plant.kind !== 'sunflower').length).toBeGreaterThanOrEqual(2)
    expect(first.plants.filter((plant) => plant.kind === 'sunflower').length).toBeLessThanOrEqual(2)
    expect(first.plants.map((plant) => plant.nurseryIndex)).toEqual([0, 1, 2, 3, 4])
    expect(currentRefreshCost(first)).toBe(15)
  })

  it('charges 10, 15, 20, 25 and 30 sun on consecutive successful refreshes', () => {
    let state = { ...createInitialState(7), sun: 200 }
    const costs: number[] = []
    for (let refresh = 0; refresh < 5; refresh += 1) {
      state = { ...state, plants: [] }
      const before = state.sun
      state = reduceEconomy(state, { type: 'refresh' })
      costs.push(before - state.sun)
    }
    expect(costs).toEqual([10, 15, 20, 25, 30])
  })

  it('protects refresh while the nursery is occupied or sun is insufficient', () => {
    const filled = reduceEconomy(createInitialState(3), { type: 'refresh' })
    const blockedByNursery = reduceEconomy(filled, { type: 'refresh' })
    expect(blockedByNursery.sun).toBe(filled.sun)
    expect(blockedByNursery.refreshCount).toBe(filled.refreshCount)
    expect(blockedByNursery.plants).toEqual(filled.plants)
    expect(blockedByNursery.feedback.at(-1)?.text).toContain('苗圃还有植物')
    expect(getRefreshBlockReason(filled)).toBe('苗圃还有植物，请先种植或合成')
    expect(getRefreshBlockReason(filled)).not.toContain('出售')

    const poor = { ...createInitialState(), sun: 9 }
    const blockedBySun = reduceEconomy(poor, { type: 'refresh' })
    expect(blockedBySun.plants).toHaveLength(0)
    expect(blockedBySun.sun).toBe(9)
    expect(blockedBySun.feedback.at(-1)?.text).toContain('阳光不足')
  })
})

describe('renewable sun income', () => {
  it('pays sunflower production and retains interval overflow', () => {
    const sunflower = makePlant({
      id: 'sunflower-1',
      kind: 'sunflower',
      potId: 'pot-1',
      productionProgress: 9.75,
    })
    const state = { ...createInitialState(), sun: 0, plants: [sunflower] }
    const produced = stepEconomy(state, 0.5)

    expect(produced.sun).toBe(1)
    expect(produced.plants[0].productionProgress).toBeCloseTo(0.25)
    expect(produced.feedback.at(-1)?.text).toContain('向日葵 +1')
  })

  it('applies installed weapon conversions to sunflower production', () => {
    const gatlingSunflower = makePlant({
      id: 'gatling-sunflower',
      kind: 'sunflower',
      potId: 'pot-1',
      weapon: 'gatling',
    })
    const chiliSunflower = makePlant({
      id: 'chili-sunflower',
      kind: 'sunflower',
      potId: 'pot-2',
      weapon: 'chili',
      productionProgress: 9.9,
    })
    const state = { ...createInitialState(), sun: 0, plants: [gatlingSunflower, chiliSunflower] }
    const produced = stepEconomy(state, 5.6)

    expect(produced.sun).toBe(3)
    expect(produced.plants.find((plant) => plant.id === gatlingSunflower.id)?.productionProgress).toBeLessThan(0.1)
  })

  it('applies kill and wave rewards as explicit economy events', () => {
    const initial = { ...createInitialState(), sun: 0 }
    const killed = applyEconomyEvent(initial, { type: 'zombie-killed', reward: 4 })
    const completed = applyEconomyEvent(killed, { type: 'wave-completed' })
    expect(killed.sun).toBe(4)
    expect(completed.sun).toBe(4 + DEFAULT_WAVE_REWARD)
    expect(completed.feedback.at(-1)?.text).toContain('波次奖励')
  })
})

describe('plant placement and manipulation', () => {
  it('plants from the nursery into an empty pot and keeps invalid drops intact', () => {
    const nurseryPlant = makePlant({ id: 'pea-1', nurseryIndex: 0 })
    const initial = { ...createInitialState(), plants: [nurseryPlant] }
    expect(createPlantDropCommand(initial, nurseryPlant.id, 'pot-1')).toEqual({
      type: 'place-plant',
      plantId: nurseryPlant.id,
      potId: 'pot-1',
    })

    const planted = reduceEconomy(initial, { type: 'place-plant', plantId: nurseryPlant.id, potId: 'pot-1' })
    expect(planted.plants[0]).toMatchObject({ id: nurseryPlant.id, potId: 'pot-1', nurseryIndex: null })

    const cancelled = reduceEconomy(planted, { type: 'move-or-merge', plantId: nurseryPlant.id, potId: 'pot-1' })
    expect(cancelled.plants[0]).toMatchObject({ id: nurseryPlant.id, potId: 'pot-1' })
  })

  it('moves a battlefield plant and prevents another move during its two-second cooldown', () => {
    const pea = makePlant({ id: 'pea-1', potId: 'pot-1', productionProgress: 3 })
    const initial: GameState = { ...createInitialState(), phase: 'playing', plants: [pea] }
    const moved = reduceEconomy(initial, { type: 'move-or-merge', plantId: pea.id, potId: 'pot-2' })

    expect(moved.plants[0]).toMatchObject({
      potId: 'pot-2',
      moveCooldown: MOVE_COOLDOWN_SECONDS,
      productionProgress: 3,
      attackCooldown: 0,
    })
    expect(getPlantDropStatus(moved, pea.id, 'pot-3')).toMatchObject({ legal: false, action: 'invalid' })

    const blocked = reduceEconomy(moved, { type: 'move-or-merge', plantId: pea.id, potId: 'pot-3' })
    expect(blocked.plants[0].potId).toBe('pot-2')
    expect(blocked.feedback.at(-1)?.text).toContain('移动冷却')
  })

  it('merges matching plants at the target and enforces the four-star cap', () => {
    const source = makePlant({ id: 'pea-source', nurseryIndex: 0, weapon: 'ice' })
    const target = makePlant({ id: 'pea-target', potId: 'pot-1', productionProgress: 5 })
    const initial = { ...createInitialState(), plants: [source, target] }
    const merged = reduceEconomy(initial, { type: 'move-or-merge', plantId: source.id, potId: 'pot-1' })

    expect(merged.plants).toHaveLength(1)
    expect(merged.plants[0]).toMatchObject({
      id: target.id,
      star: 2,
      potId: 'pot-1',
      productionProgress: 0,
      attackCooldown: 0,
    })
    expect(merged.inventory.weapons.ice).toBe(1)
    expect(merged.feedback.at(-1)?.text).toContain('武器已回收')

    const maxSource = makePlant({ id: 'max-source', star: 4, nurseryIndex: 0 })
    const maxTarget = makePlant({ id: 'max-target', star: 4, potId: 'pot-2' })
    const capped = { ...createInitialState(), plants: [maxSource, maxTarget] }
    expect(getPlantDropStatus(capped, maxSource.id, 'pot-2')).toMatchObject({ legal: false, action: 'invalid' })
    expect(reduceEconomy(capped, { type: 'move-or-merge', plantId: maxSource.id, potId: 'pot-2' }).plants).toHaveLength(2)
  })

  it('uses the doubled attacking ranges while sunflower remains non-attacking', () => {
    expect(getPlantStats(makePlant({ id: 'pea', kind: 'pea' })).range).toBe(44)
    expect(getPlantStats(makePlant({ id: 'watermelon', kind: 'watermelon' })).range).toBe(44)
    expect(getPlantStats(makePlant({ id: 'banana', kind: 'banana' })).range).toBe(38)
    expect(getPlantStats(makePlant({ id: 'durian', kind: 'durian' })).range).toBe(18)
    expect(getPlantStats(makePlant({ id: 'sunflower', kind: 'sunflower' })).range).toBe(0)
  })
})
