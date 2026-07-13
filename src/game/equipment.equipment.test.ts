import { describe, expect, it } from 'vitest'
import {
  applyWeaponHit,
  canExpandPot,
  canInstallWeapon,
  expandPot,
  getAttackModifiers,
  getCoveragePreview,
  getCoverageRadiusPercent,
  getLegalExpansionCandidates,
  getSunflowerModifiers,
  grantMilestoneReward,
  installWeapon,
  isOrthogonallyAdjacent,
} from './equipment'
import { EXPANSION_CANDIDATES, INITIAL_POTS, PLANTING_CELLS, gridToPoint } from './config'
import { createInitialState } from './state'
import type { Plant, WeaponKind, Zombie } from './types'

const createPlant = (weapon: WeaponKind | null = null, kind: Plant['kind'] = 'pea'): Plant => ({
  id: 'plant-1',
  kind,
  star: 1,
  potId: 'pot-1',
  nurseryIndex: null,
  weapon,
  attackCooldown: 0,
  productionProgress: 0,
  moveCooldown: 0,
  facing: { x: 1, y: 0 },
})

const createZombie = (): Zombie => ({
  id: 'zombie-1',
  kind: 'normal',
  hp: 100,
  maxHp: 100,
  speed: 4,
  pathProgress: 0,
  reward: 2,
  threat: 1,
  spawnOrder: 1,
  status: { slowUntil: 0, freezeUntil: 0, iceHits: 0, burns: [] },
})

describe('equipment inventory and installation', () => {
  it('installs exactly one available weapon and rejects replacements', () => {
    const base = createInitialState()
    const state = {
      ...base,
      plants: [createPlant()],
      inventory: { ...base.inventory, weapons: { ...base.inventory.weapons, ice: 1 } },
    }

    expect(canInstallWeapon(state, 'ice', 'plant-1')).toBe(true)
    const installed = installWeapon(state, 'ice', 'plant-1')
    expect(installed.inventory.weapons.ice).toBe(0)
    expect(installed.plants[0].weapon).toBe('ice')

    const rejected = installWeapon(installed, 'gatling', 'plant-1')
    expect(rejected.plants[0].weapon).toBe('ice')
    expect(rejected.inventory.weapons.gatling).toBe(0)
    expect(rejected.feedback.at(-1)?.tone).toBe('danger')
  })

  it('grants repeatable inventory counts at explicit milestones only', () => {
    const base = createInitialState()
    expect(grantMilestoneReward(base, 2)).toBe(base)
    const waveThree = grantMilestoneReward(base, 3)
    expect(waveThree.inventory.weapons.gatling).toBe(1)
    expect(waveThree.inventory.pots).toBe(1)
    const waveTwelve = grantMilestoneReward(waveThree, 12)
    expect(waveTwelve.inventory.weapons).toEqual({ gatling: 2, ice: 1, chili: 1 })
    expect(waveTwelve.inventory.pots).toBe(2)
  })
})

describe('weapon modifiers', () => {
  it('modifies gatling attack damage and interval without affecting range data', () => {
    const modifiers = getAttackModifiers(createPlant('gatling'))
    expect(modifiers.damageMultiplier).toBe(0.75)
    expect(modifiers.intervalMultiplier).toBeCloseTo(1 / 1.8)
  })

  it('converts every sunflower weapon into an economic or support modifier', () => {
    expect(getSunflowerModifiers(createPlant('gatling', 'sunflower')).productionIntervalMultiplier).toBeCloseTo(1 / 1.8)
    expect(getSunflowerModifiers(createPlant('ice', 'sunflower'))).toMatchObject({
      waveStartSlowMultiplier: 0.7,
      waveStartSlowDuration: 2,
    })
    expect(getSunflowerModifiers(createPlant('chili', 'sunflower')).bonusSunPerProduction).toBe(1)
  })

  it('slows on every ice hit and freezes on the fifth before resetting the counter', () => {
    const plant = createPlant('ice')
    let zombie = createZombie()
    for (let hit = 0; hit < 5; hit += 1) zombie = applyWeaponHit(zombie, plant, 1, 10)
    expect(zombie.hp).toBe(95)
    expect(zombie.status.slowUntil).toBe(12)
    expect(zombie.status.freezeUntil).toBe(11)
    expect(zombie.status.iceHits).toBe(0)
  })

  it('caps independent chili burns at three layers', () => {
    const plant = createPlant('chili')
    let zombie = createZombie()
    for (let hit = 0; hit < 4; hit += 1) zombie = applyWeaponHit(zombie, plant, 10, hit)
    expect(zombie.hp).toBe(60)
    expect(zombie.status.burns).toHaveLength(3)
    expect(zombie.status.burns.every((burn) => burn.remaining === 3 && burn.damagePerSecond === 2)).toBe(true)
  })
})

describe('pot expansion and coverage preview', () => {
  it('keeps every planting cell unique, grid-aligned, and outside the central road corridor', () => {
    const keys = PLANTING_CELLS.map((cell) => `${cell.column}:${cell.row}`)
    expect(new Set(keys).size).toBe(keys.length)
    expect(INITIAL_POTS.every((pot) => keys.includes(`${pot.column}:${pot.row}`))).toBe(true)
    expect(PLANTING_CELLS.every((cell) => cell.x <= 33 || cell.x >= 65)).toBe(true)
  })

  it('accepts cardinal grid neighbors and rejects diagonal neighbors', () => {
    expect(isOrthogonallyAdjacent(gridToPoint({ column: 0, row: 0 }), gridToPoint({ column: 1, row: 0 }))).toBe(true)
    expect(isOrthogonallyAdjacent(gridToPoint({ column: 0, row: 0 }), gridToPoint({ column: 1, row: 1 }))).toBe(false)
  })

  it('spends one pot on a permanent legal expansion', () => {
    const base = createInitialState()
    const state = { ...base, inventory: { ...base.inventory, pots: 1 } }
    const point = EXPANSION_CANDIDATES[0]
    expect(canExpandPot(state, point)).toBe(true)
    expect(getLegalExpansionCandidates(state)).toContainEqual(point)
    const expanded = expandPot(state, point)
    expect(expanded.inventory.pots).toBe(0)
    expect(expanded.pots).toContainEqual(expect.objectContaining({ column: point.column, row: point.row, active: true }))
  })

  it('does not consume inventory for a diagonal or forbidden location', () => {
    const base = createInitialState()
    const state = { ...base, inventory: { ...base.inventory, pots: 1 } }
    const rejected = expandPot(state, { x: 37, y: 41 })
    expect(rejected.inventory.pots).toBe(1)
    expect(rejected.pots).toEqual(state.pots)
    expect(rejected.feedback.at(-1)?.tone).toBe('danger')
  })

  it('returns the designed empty-pot radii and a banana direction', () => {
    const origin = gridToPoint({ column: 2, row: 1 })
    expect(getCoveragePreview('pea', origin)?.radiusInGrid).toBe(4)
    expect(getCoveragePreview('watermelon', origin)?.radiusInGrid).toBe(4)
    expect(getCoveragePreview('durian', origin)?.radiusInGrid).toBe(1.3)
    expect(getCoveragePreview('banana', origin)?.direction).not.toBeNull()
    expect(getCoveragePreview('sunflower', origin)).toBeNull()
    expect(getCoverageRadiusPercent('pea')).toBe(44)
    expect(getCoverageRadiusPercent('watermelon')).toBe(44)
    expect(getCoverageRadiusPercent('banana')).toBe(38)
    expect(getCoverageRadiusPercent('durian')).toBe(18)
    expect(getCoverageRadiusPercent('sunflower')).toBe(0)
  })
})
