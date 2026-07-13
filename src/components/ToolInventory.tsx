import { useRef } from 'react'
import { WEAPON_META } from '../game/config'
import type { GameCommand, GameState, WeaponKind } from '../game/types'
import type { PointerPosition } from './Nursery'
import './equipment.css'

export const WEAPON_DRAG_MIME = 'application/x-fruit-defense-weapon'

export interface ToolInventoryProps {
  state: GameState
  dispatch: (command: GameCommand) => void
  onWeaponDragStart?: (weapon: WeaponKind, position: PointerPosition) => void
  onWeaponDragMove?: (weapon: WeaponKind, position: PointerPosition) => void
  onWeaponDragEnd?: (weapon: WeaponKind, position: PointerPosition) => void
  onWeaponDragCancel?: (weapon: WeaponKind) => void
  onDragVisualEnd?: () => void
  draggingWeapon?: WeaponKind | null
  className?: string
}

const WEAPONS: WeaponKind[] = ['gatling', 'ice', 'chili']

export function ToolInventory({
  state,
  dispatch,
  onWeaponDragStart,
  onWeaponDragMove,
  onWeaponDragEnd,
  onWeaponDragCancel,
  onDragVisualEnd,
  draggingWeapon = null,
  className = '',
}: ToolInventoryProps) {
  const pointerDrag = useRef<{
    weapon: WeaponKind
    startX: number
    startY: number
    started: boolean
  } | null>(null)
  const suppressClick = useRef(false)
  const selectedWeapon = state.selection?.type === 'weapon' ? state.selection.weapon : null
  const potToolSelected = state.selection?.type === 'pot-tool'

  const positionOf = (event: React.PointerEvent<HTMLElement>): PointerPosition => ({
    clientX: event.clientX,
    clientY: event.clientY,
    pointerId: event.pointerId,
  })

  return (
    <section className={`tool-inventory ${className}`.trim()} aria-label="武器和花盆库存">
      <div className="tool-inventory__group" role="group" aria-label="武器栏">
        {WEAPONS.map((weapon) => {
          const meta = WEAPON_META[weapon]
          const count = state.inventory.weapons[weapon]
          const selected = selectedWeapon === weapon
          return (
            <button
              key={weapon}
              type="button"
              className={`tool-card tool-card--${weapon}${selected ? ' is-selected' : ''}${draggingWeapon === weapon ? ' is-dragging' : ''}`}
              disabled={count <= 0}
              draggable={count > 0}
              aria-pressed={selected}
              aria-label={`${meta.name}，库存 ${count}，${meta.description}`}
              title={`${meta.name}：${meta.description}`}
              onClick={() => {
                if (suppressClick.current) {
                  suppressClick.current = false
                  return
                }
                dispatch({ type: 'select', selection: { type: 'weapon', weapon } })
              }}
              onDragStart={(event) => {
                event.dataTransfer.effectAllowed = 'copy'
                event.dataTransfer.setData(WEAPON_DRAG_MIME, weapon)
                event.dataTransfer.setData('text/plain', weapon)
                dispatch({ type: 'select', selection: { type: 'weapon', weapon } })
                onWeaponDragStart?.(weapon, { clientX: event.clientX, clientY: event.clientY, pointerId: -1 })
              }}
              onDrag={(event) => {
                if (event.clientX || event.clientY) {
                  onWeaponDragMove?.(weapon, { clientX: event.clientX, clientY: event.clientY, pointerId: -1 })
                }
              }}
              onDragEnd={(event) => {
                if (event.dataTransfer.dropEffect === 'none') onWeaponDragCancel?.(weapon)
                else onDragVisualEnd?.()
              }}
              onPointerDown={(event) => {
                if (event.button !== 0 || count <= 0) return
                pointerDrag.current = { weapon, startX: event.clientX, startY: event.clientY, started: false }
                event.currentTarget.setPointerCapture(event.pointerId)
              }}
              onPointerMove={(event) => {
                const active = pointerDrag.current
                if (!active || active.weapon !== weapon || !event.currentTarget.hasPointerCapture(event.pointerId)) return
                if (!active.started && Math.hypot(event.clientX - active.startX, event.clientY - active.startY) > 8) {
                  active.started = true
                  dispatch({ type: 'select', selection: { type: 'weapon', weapon } })
                  onWeaponDragStart?.(weapon, positionOf(event))
                }
                if (active.started) onWeaponDragMove?.(weapon, positionOf(event))
              }}
              onPointerUp={(event) => {
                const active = pointerDrag.current
                if (!active || active.weapon !== weapon || !event.currentTarget.hasPointerCapture(event.pointerId)) return
                if (active.started) {
                  suppressClick.current = true
                  onWeaponDragEnd?.(weapon, positionOf(event))
                }
                pointerDrag.current = null
                event.currentTarget.releasePointerCapture(event.pointerId)
              }}
              onPointerCancel={(event) => {
                const active = pointerDrag.current
                pointerDrag.current = null
                if (active?.started) onWeaponDragCancel?.(weapon)
                if (event.currentTarget.hasPointerCapture(event.pointerId)) {
                  event.currentTarget.releasePointerCapture(event.pointerId)
                }
              }}
            >
              <span className="tool-card__emoji" aria-hidden="true">{meta.emoji}</span>
              <span className="tool-card__name">{meta.name}</span>
              <strong className="tool-card__count" aria-hidden="true">×{count}</strong>
            </button>
          )
        })}
      </div>
      <button
        type="button"
        className={`tool-card tool-card--pot${potToolSelected ? ' is-selected' : ''}`}
        disabled={state.inventory.pots <= 0}
        aria-pressed={potToolSelected}
        aria-label={`花盆扩建道具，库存 ${state.inventory.pots}`}
        title="选择后点击绿色候选地块进行永久扩建"
        onClick={() => dispatch({ type: 'select', selection: { type: 'pot-tool' } })}
      >
        <span className="tool-card__emoji" aria-hidden="true">🌱</span>
        <span className="tool-card__name">花盆</span>
        <strong className="tool-card__count" aria-hidden="true">×{state.inventory.pots}</strong>
      </button>
      <p className="tool-inventory__hint">
        {potToolSelected
          ? '点击战场上的绿色候选格扩建'
          : selectedWeapon
            ? '将武器拖到绿色植物上安装'
            : '选择或拖动道具来使用'}
      </p>
    </section>
  )
}
