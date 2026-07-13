import { describe, expect, it } from 'vitest'
import { render, screen } from '@testing-library/react'
import { createElement } from 'react'
import { Battlefield } from '../components/Battlefield'
import {
  PATH_LENGTH,
  WAVE_TABLE,
  getPlantAttackInterval,
  getPlantDamage,
  getPlantRange,
  reduceBattle,
  samplePath,
  selectTarget,
  stepBattle,
} from './battle'
import { PLANT_CONFIG, STAR_DAMAGE, STAR_RANGE, STAR_SPEED } from './config'
import { stepGame } from './engine'
import { createInitialState } from './state'
import type { GameState, Plant, PlantKind, Point, Star, Zombie } from './types'

const status = () => ({ slowUntil: 0, freezeUntil: 0, iceHits: 0, burns: [] })

const zombie = (overrides: Partial<Zombie> = {}): Zombie => ({
  id: 'zombie-test',
  kind: 'normal',
  hp: 100,
  maxHp: 100,
  speed: 0,
  pathProgress: 0,
  reward: 2,
  threat: 1,
  spawnOrder: 1,
  status: status(),
  ...overrides,
})

const plant = (kind: PlantKind, star: Star = 1, overrides: Partial<Plant> = {}): Plant => ({
  id: `plant-${kind}`,
  kind,
  star,
  potId: 'pot-1',
  nurseryIndex: null,
  weapon: null,
  attackCooldown: 0,
  productionProgress: 0,
  moveCooldown: 0,
  facing: { x: 1, y: 0 },
  ...overrides,
})

const nearestProgress = (point: Point) => {
  let result = 0
  let bestDistance = Number.POSITIVE_INFINITY
  for (let step = 0; step <= 4000; step += 1) {
    const progress = PATH_LENGTH * step / 4000
    const sampled = samplePath(progress)
    const gap = Math.hypot(sampled.x - point.x, sampled.y - point.y)
    if (gap < bestDistance) {
      bestDistance = gap
      result = progress
    }
  }
  return result
}

const battleState = (plants: Plant[], zombies: Zombie[]): GameState => {
  const initial = createInitialState(7)
  return {
    ...initial,
    phase: 'playing',
    plants,
    zombies,
    wave: { index: 1, spawned: 99, total: 99, spawnCooldown: 999, betweenTimer: 0, started: true },
  }
}

const advanceBattle = (state: GameState, seconds: number, step = 0.05) => {
  let next = state
  for (let elapsed = 0; elapsed < seconds; elapsed += step) {
    next = stepBattle({ ...next, elapsed: next.elapsed + step }, step)
  }
  return next
}

describe('zombie path and waves', () => {
  it('samples both path endpoints and clamps progress', () => {
    expect(samplePath(-1)).toEqual(samplePath(0))
    expect(samplePath(PATH_LENGTH + 10)).toEqual(samplePath(PATH_LENGTH))
    expect(samplePath(0)).not.toEqual(samplePath(PATH_LENGTH))
  })

  it('defines all 15 increasingly stronger waves and final bosses', () => {
    expect(WAVE_TABLE).toHaveLength(15)
    expect(WAVE_TABLE[14].hpMultiplier).toBeGreaterThan(WAVE_TABLE[0].hpMultiplier)
    expect(WAVE_TABLE[14].sequence.filter((kind) => kind === 'boss')).toHaveLength(2)
    expect(new Set(WAVE_TABLE.flatMap((wave) => wave.sequence))).toEqual(new Set(['normal', 'runner', 'armored', 'boss']))
  })

  it('moves a leaked zombie out and removes core life', () => {
    const state = battleState([], [zombie({ pathProgress: PATH_LENGTH - 0.1, speed: 10, threat: 2 })])
    const next = stepBattle({ ...state, lives: 2, elapsed: 1 }, 0.1)
    expect(next.lives).toBe(0)
    expect(next.phase).toBe('defeat')
    expect(next.zombies).toHaveLength(0)
  })

  it('pays a reward and automatically begins the next wave after preparation', () => {
    const state = battleState([], [])
    const cleared = stepBattle({ ...state, sun: 0, wave: { ...state.wave, index: 1 } }, 0.01)
    expect(cleared.phase).toBe('between-waves')
    expect(cleared.sun).toBeGreaterThan(0)
    const next = stepBattle(cleared, cleared.wave.betweenTimer)
    expect(next.phase).toBe('playing')
    expect(next.wave.index).toBe(2)
  })
})

describe('targeting and star growth', () => {
  it('targets shortest remaining path, then lower hp, then earlier spawn order', () => {
    const origin = samplePath(100)
    const targets = [
      zombie({ id: 'far', pathProgress: 96, hp: 1, spawnOrder: 0 }),
      zombie({ id: 'late', pathProgress: 101, hp: 20, spawnOrder: 9 }),
      zombie({ id: 'early', pathProgress: 101, hp: 20, spawnOrder: 2 }),
    ]
    expect(selectTarget(targets, origin, 20)?.id).toBe('early')
    expect(selectTarget([
      zombie({ id: 'healthy', pathProgress: 101, hp: 50 }),
      zombie({ id: 'hurt', pathProgress: 101, hp: 10 }),
    ], origin, 20)?.id).toBe('hurt')
  })

  it('applies four-star damage, speed and range multipliers without changing kind', () => {
    const upgraded = plant('pea', 4)
    expect(getPlantDamage(upgraded)).toBe(PLANT_CONFIG.pea.damage * STAR_DAMAGE[4])
    expect(getPlantAttackInterval(upgraded)).toBe(PLANT_CONFIG.pea.interval / STAR_SPEED[4])
    expect(getPlantRange(upgraded)).toBe(PLANT_CONFIG.pea.range * STAR_RANGE[4])
    expect(upgraded.kind).toBe('pea')
  })
})

describe('distinct plant attacks', () => {
  const potPoint = { x: 25, y: 28 }
  const targetProgress = nearestProgress(potPoint)

  it('creates a tracking pea projectile', () => {
    const state = battleState([plant('pea')], [zombie({ pathProgress: targetProgress })])
    const next = stepBattle(state, 0.01)
    expect(next.projectiles).toHaveLength(1)
    expect(next.projectiles[0]).toMatchObject({ kind: 'pea', targetId: 'zombie-test' })
  })

  it('explodes a watermelon at a saved point and damages nearby zombies', () => {
    const state = battleState([plant('watermelon')], [
      zombie({ id: 'center', pathProgress: targetProgress }),
      zombie({ id: 'nearby', pathProgress: targetProgress + 2, spawnOrder: 2 }),
    ])
    let next = stepBattle(state, 0.01)
    expect(next.projectiles[0]?.kind).toBe('watermelon')
    next = { ...next, plants: next.plants.map((item) => ({ ...item, attackCooldown: 999 })) }
    next = advanceBattle(next, 0.5)
    expect(next.zombies.find((item) => item.id === 'center')!.hp).toBeLessThan(100)
    expect(next.zombies.find((item) => item.id === 'nearby')!.hp).toBeLessThan(100)
  })

  it('hits each zombie at most once outbound and once returning with banana', () => {
    const state = battleState([plant('banana')], [zombie({ hp: 1000, maxHp: 1000, pathProgress: targetProgress })])
    let next = stepBattle(state, 0.01)
    next = { ...next, plants: next.plants.map((item) => ({ ...item, attackCooldown: 999 })) }
    next = advanceBattle(next, 2)
    expect(next.zombies[0].hp).toBe(1000 - getPlantDamage(plant('banana')) * 2)
    expect(next.projectiles).toHaveLength(0)
  })

  it('applies durian damage to every zombie in melee range', () => {
    const meleeProgress = nearestProgress({ x: 52, y: 24 })
    const state = battleState([plant('durian', 1, { potId: 'pot-3' })], [
      zombie({ id: 'first', pathProgress: meleeProgress }),
      zombie({ id: 'second', pathProgress: meleeProgress + 2, spawnOrder: 2 }),
    ])
    const next = stepBattle(state, 0.01)
    expect(next.zombies.map((item) => item.hp)).toEqual([82, 82])
  })

  it('lets the economy tick own sunflower production exactly once', () => {
    const state = battleState([plant('sunflower', 2, { productionProgress: 9.49 })], [zombie({ pathProgress: targetProgress })])
    const next = stepGame(state, 0.05)
    expect(next.sun).toBe(state.sun + 2)
    expect(next.plants[0].productionProgress).toBeCloseTo(0.04)
  })
})

describe('battle conclusion and rendering', () => {
  it('wins after the final zombie in wave 15 is destroyed', () => {
    const targetProgress = nearestProgress({ x: 52, y: 24 })
    const state = battleState([plant('durian', 1, { potId: 'pot-3' })], [zombie({ hp: 1, maxHp: 1, pathProgress: targetProgress })])
    const final = {
      ...state,
      wave: { index: 15, spawned: 1, total: 1, spawnCooldown: 1, betweenTimer: 0, started: true },
    } satisfies GameState
    expect(stepBattle(final, 0.01).phase).toBe('victory')
  })

  it('starts wave one from the ready phase', () => {
    const next = reduceBattle(createInitialState(), { type: 'start-wave' })
    expect(next.phase).toBe('playing')
    expect(next.wave).toMatchObject({ index: 1, started: true, spawned: 0 })
  })

  it('renders plants, zombies, projectiles and selected range from props', () => {
    const targetProgress = nearestProgress({ x: 25, y: 28 })
    const base = battleState([plant('pea')], [zombie({ pathProgress: targetProgress })])
    const state = stepBattle({ ...base, selection: { type: 'plant', id: 'plant-pea' } }, 0.01)
    render(createElement(Battlefield, { state, dispatch: () => undefined }))
    expect(screen.getByLabelText('果园战场')).toBeInTheDocument()
    expect(screen.getByLabelText('豌豆 1星')).toBeInTheDocument()
    expect(screen.getByLabelText(/普通僵尸 生命/)).toBeInTheDocument()
    expect(screen.getByLabelText('豌豆攻击范围')).toBeInTheDocument()
  })
})
