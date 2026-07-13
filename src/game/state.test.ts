import { describe, expect, it } from 'vitest'
import { reduceGame, stepGame } from './engine'
import { createInitialState, nextRandom } from './state'

describe('game foundation', () => {
  it('creates a complete initial state', () => {
    const state = createInitialState(7)
    expect(state.sun).toBe(80)
    expect(state.lives).toBe(10)
    expect(state.pots.length).toBeGreaterThan(0)
    expect(state.inventory.weapons).toEqual({ gatling: 0, ice: 0, chili: 0 })
  })

  it('does not advance while paused', () => {
    const state = { ...createInitialState(), paused: true }
    expect(stepGame(state, 1)).toBe(state)
  })

  it('resets runtime data', () => {
    const dirty = { ...createInitialState(99), sun: 1, lives: 2, elapsed: 30 }
    const reset = reduceGame(dirty, { type: 'restart' })
    expect(reset.sun).toBe(80)
    expect(reset.lives).toBe(10)
    expect(reset.elapsed).toBe(0)
  })

  it('uses a deterministic random sequence', () => {
    expect(nextRandom(42)).toEqual(nextRandom(42))
  })
})
