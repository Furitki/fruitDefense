import { MAX_WAVES } from '../game/config'
import type { GameCommand, GameState } from '../game/types'

export function Hud({ state, dispatch }: { state: GameState; dispatch: (command: GameCommand) => void }) {
  return (
    <header className="hud" aria-label="游戏状态">
      <div className="stat"><span aria-hidden>☀️</span><span>阳光</span><strong data-testid="sun-count">{state.sun}</strong></div>
      <div className="stat"><span aria-hidden>❤️</span><span>生命</span><strong>{state.lives}</strong></div>
      <div className="stat"><span aria-hidden>🚩</span><span>波次</span><strong>{state.wave.index}/{MAX_WAVES}</strong></div>
      <div className="hud-controls">
        <button className="icon-button" type="button" onClick={() => dispatch({ type: 'toggle-pause' })} aria-label={state.paused ? '继续游戏' : '暂停游戏'}>{state.paused ? '▶' : 'Ⅱ'}</button>
        <button className="speed-button" type="button" onClick={() => dispatch({ type: 'set-speed', speed: state.speed === 1 ? 2 : 1 })} aria-label="切换游戏速度">×{state.speed}</button>
      </div>
    </header>
  )
}
