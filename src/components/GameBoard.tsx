import { PATH_POINTS } from '../game/config'
import type { GameCommand, GameState } from '../game/types'

export function GameBoard({ state, dispatch }: { state: GameState; dispatch: (command: GameCommand) => void }) {
  const path = PATH_POINTS.map((point) => `${point.x},${point.y}`).join(' ')
  return (
    <section className="board" aria-label="果园战场">
      <svg className="road-map" viewBox="0 0 100 100" role="img" aria-label="僵尸行进道路">
        <polyline points={path} className="road-border" />
        <polyline points={path} className="road" />
      </svg>
      <div className="gate gate-in">入口<br /><span>↓</span></div>
      <div className="gate gate-out">出口<br /><span>↓</span></div>
      <div className="orchard-core"><span className="core-tree">🌳</span><strong>果园核心</strong></div>
      {state.pots.filter((pot) => pot.active).map((pot) => (
        <button
          key={pot.id}
          type="button"
          className="pot"
          style={{ left: `${pot.x}%`, top: `${pot.y}%` }}
          aria-label={`空花盆 ${pot.id}`}
          onClick={() => dispatch({ type: 'select', selection: { type: 'pot', id: pot.id } })}
        >🪴</button>
      ))}
      <div className="board-hint">先刷新植物，再拖入花盆</div>
    </section>
  )
}
