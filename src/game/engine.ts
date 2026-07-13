import { useCallback, useEffect, useRef, useState } from 'react'
import { reduceBattle, stepBattle } from './battle'
import { reduceEconomy, stepEconomy } from './economy'
import { reduceEquipment, stepEquipment } from './equipment'
import { createInitialState } from './state'
import type { GameCommand, GameState } from './types'

export const stepGame = (state: GameState, deltaSeconds: number): GameState => {
  if (state.paused || state.phase === 'victory' || state.phase === 'defeat') return state
  const delta = Math.min(deltaSeconds, 0.05) * state.speed
  let next = { ...state, elapsed: state.elapsed + delta }
  next = stepBattle(next, delta)
  next = stepEconomy(next, delta)
  next = stepEquipment(next, delta)
  next = {
    ...next,
    feedback: next.feedback.map((item) => ({ ...item, ttl: item.ttl - delta })).filter((item) => item.ttl > 0),
    plants: next.plants.map((plant) => ({ ...plant, moveCooldown: Math.max(0, plant.moveCooldown - delta) })),
  }
  return next
}

export const reduceGame = (state: GameState, command: GameCommand): GameState => {
  if (command.type === 'restart') return createInitialState(state.randomSeed)
  if (command.type === 'toggle-pause') return { ...state, paused: !state.paused }
  if (command.type === 'set-speed') return { ...state, speed: command.speed }
  if (command.type === 'select') return { ...state, selection: command.selection }

  let next = reduceBattle(state, command)
  next = reduceEconomy(next, command)
  next = reduceEquipment(next, command)
  return next
}

export const useGameEngine = (initialState?: GameState) => {
  const [state, setState] = useState<GameState>(() => initialState ?? createInitialState())
  const frameRef = useRef<number | null>(null)
  const previousRef = useRef<number | null>(null)

  useEffect(() => {
    const frame = (time: number) => {
      const previous = previousRef.current ?? time
      previousRef.current = time
      setState((current) => stepGame(current, (time - previous) / 1000))
      frameRef.current = requestAnimationFrame(frame)
    }
    frameRef.current = requestAnimationFrame(frame)
    const resetClock = () => { previousRef.current = null }
    window.addEventListener('blur', resetClock)
    document.addEventListener('visibilitychange', resetClock)
    return () => {
      if (frameRef.current !== null) cancelAnimationFrame(frameRef.current)
      window.removeEventListener('blur', resetClock)
      document.removeEventListener('visibilitychange', resetClock)
    }
  }, [])

  const dispatch = useCallback((command: GameCommand) => setState((current) => reduceGame(current, command)), [])
  return { state, dispatch }
}
