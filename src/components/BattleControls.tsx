import { MAX_WAVES } from '../game/config'
import type { GameCommand, GameState } from '../game/types'

export interface BattleControlsProps {
  state: GameState
  dispatch: (command: GameCommand) => void
  compact?: boolean
}

export function BattleControls({ state, dispatch, compact = false }: BattleControlsProps) {
  const ended = state.phase === 'victory' || state.phase === 'defeat'
  const canStart = state.phase === 'ready' || state.phase === 'between-waves'
  const status = state.phase === 'ready'
    ? '布置植物后开始防守'
    : state.phase === 'between-waves'
      ? `${Math.ceil(state.wave.betweenTimer)} 秒后进入第 ${state.wave.index + 1} 波`
      : state.phase === 'playing'
        ? `第 ${state.wave.index}/${MAX_WAVES} 波 · 场上 ${state.zombies.length} 只僵尸`
        : state.phase === 'victory' ? '果园守卫成功' : '果园核心失守'

  return (
    <section className={`battle-controls${compact ? ' is-compact' : ''}`} aria-label="战斗控制">
      <strong className="battle-status-text" aria-live="polite">{status}</strong>
      <div className="battle-control-actions">
        {canStart && (
          <button type="button" className="battle-start-button" onClick={() => dispatch({ type: 'start-wave' })}>
            {state.phase === 'ready' ? '开始第 1 波' : '立即开始下一波'}
          </button>
        )}
        {!ended && state.phase !== 'ready' && (
          <button type="button" onClick={() => dispatch({ type: 'toggle-pause' })}>
            {state.paused ? '继续' : '暂停'}
          </button>
        )}
        {!ended && (
          <button type="button" onClick={() => dispatch({ type: 'set-speed', speed: state.speed === 1 ? 2 : 1 })}>
            速度 ×{state.speed}
          </button>
        )}
        {ended && <button type="button" onClick={() => dispatch({ type: 'restart' })}>重新开始</button>}
      </div>
    </section>
  )
}
