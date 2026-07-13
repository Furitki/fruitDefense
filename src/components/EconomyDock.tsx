import { currentRefreshCost, getRefreshBlockReason } from '../game/economy'
import type { GameCommand, GameState, Plant } from '../game/types'
import { Nursery } from './Nursery'
import type { NurseryPointerHandlers, PlantDragPayload, PointerPosition } from './Nursery'
import { PlantDetails } from './PlantDetails'
import '../styles/economy.css'

export interface DropFeedbackValue {
  text: string
  tone: 'valid' | 'invalid' | 'info'
}

export interface EconomyDockProps extends Omit<NurseryPointerHandlers, 'onPointerDragEnd'> {
  state: GameState
  dispatch: (command: GameCommand) => void
  dropFeedback?: DropFeedbackValue | null
  onPointerPlantDrop?: (payload: PlantDragPayload, position: PointerPosition) => void
  draggingPlantId?: string | null
  returningPlantId?: string | null
  className?: string
}

export function DropFeedback({ value }: { value: DropFeedbackValue | null }) {
  return (
    <div className={`economy-drop-feedback${value ? ` is-${value.tone}` : ''}`} role="status" aria-live="polite">
      {value?.text ?? '拖动植物时，可用花盆会显示绿色提示'}
    </div>
  )
}

export function EconomyDock({
  state,
  dispatch,
  dropFeedback = null,
  onPointerPlantDrop,
  onPointerDragStart,
  onPointerDragMove,
  onPointerDragCancel,
  onDragVisualEnd,
  draggingPlantId,
  returningPlantId,
  className = '',
}: EconomyDockProps) {
  const selectedId = state.selection
    && (state.selection.type === 'plant' || state.selection.type === 'nursery')
    ? state.selection.id
    : null
  const selectedPlant = state.plants.find((plant) => plant.id === selectedId) ?? null
  const refreshReason = getRefreshBlockReason(state)
  const refreshPrice = currentRefreshCost(state)

  const selectPlant = (plant: Plant) => dispatch({
    type: 'select',
    selection: { type: plant.nurseryIndex === null ? 'plant' : 'nursery', id: plant.id },
  })
  const pointerDrop = (payload: PlantDragPayload, position: PointerPosition) => {
    onPointerPlantDrop?.(payload, position)
  }

  const combinedFeedback = dropFeedback ?? (refreshReason
    ? { text: refreshReason, tone: 'info' as const }
    : null)

  return (
    <section className={`economy-dock ${className}`.trim()} aria-label="植物经济操作区">
      <Nursery
        plants={state.plants}
        selectedPlantId={selectedId}
        draggingPlantId={draggingPlantId}
        returningPlantId={returningPlantId}
        onSelect={selectPlant}
        onCancelDrop={() => dispatch({ type: 'select', selection: null })}
        onPointerDragStart={onPointerDragStart}
        onPointerDragMove={onPointerDragMove}
        onPointerDragEnd={pointerDrop}
        onPointerDragCancel={onPointerDragCancel}
        onDragVisualEnd={onDragVisualEnd}
      />
      <DropFeedback value={combinedFeedback} />
      <button
        type="button"
        className={`economy-refresh${refreshReason ? ' is-blocked' : ''}`}
        aria-disabled={Boolean(refreshReason)}
        title={refreshReason ?? `消耗 ${refreshPrice} 阳光刷新五株植物`}
        onClick={() => dispatch({ type: 'refresh' })}
      >
        <strong>刷新植物</strong><span>☀️ {refreshPrice}</span>
      </button>
      <PlantDetails
        plant={selectedPlant}
        onClose={() => dispatch({ type: 'select', selection: null })}
      />
    </section>
  )
}
