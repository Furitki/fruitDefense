import type { GameCommand, GameState } from '../game/types'

export function ControlDock({ state, dispatch }: { state: GameState; dispatch: (command: GameCommand) => void }) {
  return (
    <section className="control-dock" aria-label="植物和道具操作区">
      <div className="nursery-placeholder">
        {Array.from({ length: 5 }, (_, index) => <button type="button" className="nursery-slot" key={index} aria-label={`苗圃空位 ${index + 1}`}><span>+</span><small>空位</small></button>)}
      </div>
      <div className="dock-actions">
        <button className="primary-action" type="button" onClick={() => dispatch({ type: 'refresh' })}>刷新植物 <span>☀️ 10</span></button>
        <button className="start-action" type="button" onClick={() => dispatch({ type: 'start-wave' })}>{state.phase === 'ready' ? '开始第 1 波' : '战斗进行中'}</button>
      </div>
    </section>
  )
}
