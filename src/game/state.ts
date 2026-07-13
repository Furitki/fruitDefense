import { INITIAL_POTS } from './config'
import type { GameState } from './types'

export const createInitialState = (seed = 20260713): GameState => ({
  phase: 'ready',
  paused: false,
  speed: 1,
  elapsed: 0,
  sun: 80,
  lives: 10,
  refreshCount: 0,
  wave: { index: 0, spawned: 0, total: 0, spawnCooldown: 0, betweenTimer: 0, started: false },
  plants: [],
  pots: INITIAL_POTS.map((pot) => ({ ...pot })),
  zombies: [],
  projectiles: [],
  inventory: { weapons: { gatling: 0, ice: 0, chili: 0 }, pots: 0 },
  selection: null,
  feedback: [],
  nextId: 1,
  randomSeed: seed,
})

export const nextRandom = (seed: number): [number, number] => {
  const nextSeed = (seed * 1664525 + 1013904223) >>> 0
  return [nextSeed / 4294967296, nextSeed]
}
