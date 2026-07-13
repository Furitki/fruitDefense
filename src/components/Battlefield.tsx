import { useRef } from 'react'
import { ORCHARD_DESTINATION, PATH_POINTS, PLANT_CONFIG, PLANTING_CELLS, ZOMBIE_META } from '../game/config'
import { getPlantPosition, getPlantRange, samplePath, selectTarget } from '../game/battle'
import { createPlantDropCommand, getPlantDropStatus } from '../game/economy'
import { canInstallWeapon } from '../game/equipment'
import type { GameCommand, GameState, Plant, Point, WeaponKind } from '../game/types'
import type { PlantDragPayload, PlantDragSession, PointerPosition } from './Nursery'
import { readPlantDragPayload, writePlantDragPayload } from './Nursery'
import { WEAPON_DRAG_MIME } from './ToolInventory'
import '../styles/battle.css'

export interface BattlefieldProps {
  state: GameState
  dispatch: (command: GameCommand) => void
  onPointerPlantDrop?: (payload: PlantDragPayload, position: PointerPosition) => void
  onPlantDrop?: (payload: PlantDragPayload, potId: string) => void
  onPlantDragStart?: (payload: PlantDragPayload, position: PointerPosition) => void
  onPlantDragMove?: (payload: PlantDragPayload, position: PointerPosition) => void
  onPlantDragVisualEnd?: () => void
  onPlantDragCancel?: (payload: PlantDragPayload) => void
  onWeaponDrop?: (weapon: WeaponKind, plantId: string) => void
  dragSession?: PlantDragSession | null
  weaponDragSession?: { weapon: WeaponKind; hoveredPlantId: string | null } | null
  dropPulse?: { potId: string; action: 'plant' | 'move' | 'merge'; key: number } | null
  returningPlantId?: string | null
  className?: string
}

const plantDirection = (plant: Plant, origin: Point, state: GameState): Point => {
  const target = selectTarget(state.zombies, origin, getPlantRange(plant))
  if (target) {
    const position = samplePath(target.pathProgress)
    const length = Math.max(0.001, Math.hypot(position.x - origin.x, position.y - origin.y))
    return { x: (position.x - origin.x) / length, y: (position.y - origin.y) / length }
  }
  const length = Math.hypot(plant.facing.x, plant.facing.y)
  return length > 0 ? { x: plant.facing.x / length, y: plant.facing.y / length } : { x: 1, y: 0 }
}

const isWeapon = (value: string): value is WeaponKind => value === 'gatling' || value === 'ice' || value === 'chili'

export function Battlefield({
  state,
  dispatch,
  onPointerPlantDrop,
  onPlantDrop,
  onPlantDragStart,
  onPlantDragMove,
  onPlantDragVisualEnd,
  onPlantDragCancel,
  onWeaponDrop,
  dragSession = null,
  weaponDragSession = null,
  dropPulse = null,
  returningPlantId = null,
  className = '',
}: BattlefieldProps) {
  const pointerDrag = useRef<{ payload: PlantDragPayload; startX: number; startY: number; moved: boolean; started: boolean } | null>(null)
  const suppressClick = useRef(false)
  const roadPoints = PATH_POINTS.map((point) => `${point.x},${point.y}`).join(' ')
  const plantedByPot = new Map(state.plants.filter((plant) => plant.potId).map((plant) => [plant.potId, plant]))
  const selection = state.selection
  const selectedPlant = selection?.type === 'plant'
    ? state.plants.find((plant) => plant.id === selection.id) ?? null
    : null
  const selectedMoveId = selection && (selection.type === 'plant' || selection.type === 'nursery') ? selection.id : null
  const draggedPlantId = dragSession?.payload.plantId ?? null
  const draggedPlant = draggedPlantId ? state.plants.find((plant) => plant.id === draggedPlantId) ?? null : null
  const hoveredPot = dragSession?.hoveredPotId
    ? state.pots.find((pot) => pot.id === dragSession.hoveredPotId) ?? null
    : null
  const hoveredDrop = draggedPlant && hoveredPot ? getPlantDropStatus(state, draggedPlant.id, hoveredPot.id) : null
  const rangePlant = hoveredDrop?.legal ? draggedPlant : selectedPlant
  const selectedOrigin = hoveredDrop?.legal && hoveredPot
    ? { x: hoveredPot.x, y: hoveredPot.y }
    : selectedPlant ? getPlantPosition(selectedPlant, state.pots) : null
  const selectedRange = rangePlant ? getPlantRange(rangePlant) : 0
  const bananaDirection = rangePlant?.kind === 'banana' && selectedOrigin
    ? plantDirection(rangePlant, selectedOrigin, state)
    : null

  return (
    <section className={`battle-field ${className}`.trim()} aria-label="果园战场">
      <svg className="battle-road-map" viewBox="0 0 100 100" role="img" aria-label="僵尸从入口前往我方果园的道路">
        <polyline points={roadPoints} className="battle-road-border" />
        <polyline points={roadPoints} className="battle-road" />
        {bananaDirection && selectedOrigin && (
          <line
            className="battle-banana-guide"
            x1={selectedOrigin.x}
            y1={selectedOrigin.y}
            x2={selectedOrigin.x + bananaDirection.x * selectedRange}
            y2={selectedOrigin.y + bananaDirection.y * selectedRange}
          />
        )}
      </svg>

      <div className="battle-gate battle-gate-in">入口 <span>↓</span></div>
      <div
        className="battle-core"
        aria-label="果园终点"
        data-path-end={`${ORCHARD_DESTINATION.x}:${ORCHARD_DESTINATION.y}`}
        style={{ left: `${ORCHARD_DESTINATION.x}%`, top: `${ORCHARD_DESTINATION.y}%` }}
      ><span>🌳</span><strong>我方果园</strong></div>

      <div className="battle-soil-grid" aria-label="规则种植区域">
        {PLANTING_CELLS.map((cell) => {
          const active = state.pots.some((pot) => pot.active && pot.column === cell.column && pot.row === cell.row)
          return (
            <span
              key={`${cell.column}-${cell.row}`}
              className={`battle-soil-cell${active ? ' is-active' : ' is-expandable'}`}
              style={{ left: `${cell.x}%`, top: `${cell.y}%` }}
              data-grid-cell={`${cell.column}:${cell.row}`}
              data-cell-shape="square"
            />
          )
        })}
      </div>

      {selectedOrigin && selectedRange > 0 && (
        <div
          className="battle-attack-range"
          aria-label={`${PLANT_CONFIG[rangePlant!.kind].name}攻击范围`}
          style={{ left: `${selectedOrigin.x}%`, top: `${selectedOrigin.y}%`, width: `${selectedRange * 2}%` }}
        />
      )}

      {state.pots.filter((pot) => pot.active).map((pot) => {
        const plant = plantedByPot.get(pot.id)
        const dropStatus = draggedPlantId ? getPlantDropStatus(state, draggedPlantId, pot.id) : null
        const activeWeapon = weaponDragSession?.weapon ?? (selection?.type === 'weapon' ? selection.weapon : null)
        const weaponTarget = activeWeapon && plant
          ? canInstallWeapon(state, activeWeapon, plant.id)
          : null
        const targetClass = dropStatus
          ? dropStatus.legal ? ` is-drop-valid is-drop-${dropStatus.action}` : dropStatus.action === 'cancel' ? '' : ' is-drop-invalid'
          : weaponTarget === true ? ' is-drop-valid' : weaponTarget === false ? ' is-drop-invalid' : ''
        const hoveredClass = dragSession?.hoveredPotId === pot.id ? ' is-drop-hovered' : ''
        const weaponHoveredClass = weaponDragSession?.hoveredPlantId === plant?.id ? ' is-weapon-drop-hovered' : ''
        const sourceClass = plant && dragSession && plant.id === dragSession.payload.plantId ? ' is-drag-source' : ''
        const pulseClass = dropPulse?.potId === pot.id
          ? ` is-drop-success is-${dropPulse.action}-success`
          : ''
        const returnClass = plant?.id === returningPlantId ? ' is-drag-returning' : ''

        const activatePot = () => {
          if (suppressClick.current) {
            suppressClick.current = false
            return
          }
          if (selection?.type === 'weapon' && plant) {
            dispatch({ type: 'install-weapon', weapon: selection.weapon, plantId: plant.id })
            return
          }
          if (selectedMoveId) {
            const source = state.plants.find((candidate) => candidate.id === selectedMoveId)
            if (source && source.id !== plant?.id) {
              if (onPlantDrop) {
                onPlantDrop(
                  { plantId: source.id, source: source.nurseryIndex !== null ? 'nursery' : 'field' },
                  pot.id,
                )
              } else {
                dispatch(source.nurseryIndex !== null
                  ? { type: 'place-plant', plantId: source.id, potId: pot.id }
                  : { type: 'move-or-merge', plantId: source.id, potId: pot.id })
              }
              return
            }
          }
          dispatch({ type: 'select', selection: plant ? { type: 'plant', id: plant.id } : { type: 'pot', id: pot.id } })
        }

        return (
          <button
            key={pot.id}
            type="button"
            className={`battle-pot${plant ? ' is-planted' : ''}${plant && selectedPlant?.id === plant.id ? ' is-selected' : ''}${targetClass}${hoveredClass}${weaponHoveredClass}${sourceClass}${pulseClass}${returnClass}`}
            style={{ left: `${pot.x}%`, top: `${pot.y}%` }}
            aria-label={plant ? `${PLANT_CONFIG[plant.kind].name} ${plant.star}星` : `空花盆 ${pot.id}`}
            data-pot-id={pot.id}
            data-plant-id={plant?.id}
            draggable={Boolean(plant)}
            onClick={activatePot}
            onDragStart={(event) => {
              if (!plant) return
              dispatch({ type: 'select', selection: { type: 'plant', id: plant.id } })
              const payload: PlantDragPayload = { plantId: plant.id, source: 'field' }
              writePlantDragPayload(event.dataTransfer, payload)
              onPlantDragStart?.(payload, { clientX: event.clientX, clientY: event.clientY, pointerId: -1 })
            }}
            onDrag={(event) => {
              if (!plant || (!event.clientX && !event.clientY)) return
              onPlantDragMove?.(
                { plantId: plant.id, source: 'field' },
                { clientX: event.clientX, clientY: event.clientY, pointerId: -1 },
              )
            }}
            onDragEnd={(event) => {
              if (event.dataTransfer.dropEffect === 'none' && plant) {
                onPlantDragCancel?.({ plantId: plant.id, source: 'field' })
              } else onPlantDragVisualEnd?.()
            }}
            onDragOver={(event) => {
              event.preventDefault()
              event.dataTransfer.dropEffect = 'move'
            }}
            onDrop={(event) => {
              event.preventDefault()
              const payload = readPlantDragPayload(event.dataTransfer)
              if (payload) {
                if (onPlantDrop) onPlantDrop(payload, pot.id)
                else {
                  const command = createPlantDropCommand(state, payload.plantId, pot.id)
                  if (command) dispatch(command)
                }
                return
              }
              const weapon = event.dataTransfer.getData(WEAPON_DRAG_MIME) || event.dataTransfer.getData('text/plain')
              if (plant && isWeapon(weapon)) {
                if (onWeaponDrop) onWeaponDrop(weapon, plant.id)
                else dispatch({ type: 'install-weapon', weapon, plantId: plant.id })
              }
            }}
            onPointerDown={(event) => {
              if (!plant || event.button !== 0) return
              pointerDrag.current = {
                payload: { plantId: plant.id, source: 'field' },
                startX: event.clientX,
                startY: event.clientY,
                moved: false,
                started: false,
              }
              event.currentTarget.setPointerCapture(event.pointerId)
              if (selection?.type !== 'weapon') {
                dispatch({ type: 'select', selection: { type: 'plant', id: plant.id } })
              }
            }}
            onPointerMove={(event) => {
              const active = pointerDrag.current
              if (!active || !event.currentTarget.hasPointerCapture(event.pointerId)) return
              active.moved ||= Math.hypot(event.clientX - active.startX, event.clientY - active.startY) > 8
              if (active.moved) {
                const position = { clientX: event.clientX, clientY: event.clientY, pointerId: event.pointerId }
                if (!active.started) {
                  active.started = true
                  onPlantDragStart?.(active.payload, position)
                }
                onPlantDragMove?.(active.payload, position)
              }
            }}
            onPointerUp={(event) => {
              const active = pointerDrag.current
              if (!active || !event.currentTarget.hasPointerCapture(event.pointerId)) return
              if (active.moved) {
                suppressClick.current = true
                onPointerPlantDrop?.(active.payload, { clientX: event.clientX, clientY: event.clientY, pointerId: event.pointerId })
              }
              pointerDrag.current = null
              onPlantDragVisualEnd?.()
              event.currentTarget.releasePointerCapture(event.pointerId)
            }}
            onPointerCancel={(event) => {
              const active = pointerDrag.current
              pointerDrag.current = null
              if (active?.started) onPlantDragCancel?.(active.payload)
              if (event.currentTarget.hasPointerCapture(event.pointerId)) event.currentTarget.releasePointerCapture(event.pointerId)
            }}
          >
            {plant ? (
              <>
                <span className="battle-plant-emoji" aria-hidden>{PLANT_CONFIG[plant.kind].emoji}</span>
                <span className="battle-stars" aria-hidden>{'★'.repeat(plant.star)}</span>
                {plant.weapon && <span className={`battle-weapon battle-weapon-${plant.weapon}`} aria-label={`${plant.weapon}武器`} />}
              </>
            ) : <span className="battle-empty-pot" aria-hidden />}
            {dragSession?.hoveredPotId === pot.id && dropStatus && (
              <span className={`battle-drop-label is-${dropStatus.legal ? dropStatus.action : 'invalid'}`}>
                {dropStatus.action === 'merge' && plant
                  ? `合成 ★${plant.star + 1}`
                  : dropStatus.reason}
              </span>
            )}
            {weaponDragSession?.hoveredPlantId === plant?.id && activeWeapon && (
              <span className={`battle-drop-label is-${weaponTarget ? 'weapon' : 'invalid'}`}>
                {weaponTarget ? '松开安装武器' : '这株植物不能安装'}
              </span>
            )}
          </button>
        )
      })}

      {state.zombies.map((zombie) => {
        const position = samplePath(zombie.pathProgress)
        const health = Math.max(0, zombie.hp / zombie.maxHp) * 100
        const frozen = zombie.status.freezeUntil > state.elapsed
        const slowed = zombie.status.slowUntil > state.elapsed
        return (
          <div
            key={zombie.id}
            className={`battle-zombie battle-zombie-${zombie.kind}${frozen ? ' is-frozen' : slowed ? ' is-slowed' : ''}`}
            style={{ left: `${position.x}%`, top: `${position.y}%` }}
            aria-label={`${ZOMBIE_META[zombie.kind].name} 生命 ${Math.ceil(zombie.hp)}/${zombie.maxHp}`}
          >
            <span className="battle-zombie-emoji" aria-hidden>{ZOMBIE_META[zombie.kind].emoji}</span>
            <span className="battle-health"><span style={{ width: `${health}%` }} /></span>
            {zombie.status.burns.length > 0 && <span className="battle-status" aria-label="燃烧">🔥</span>}
          </div>
        )
      })}

      {state.projectiles.map((projectile) => (
        <span
          key={projectile.id}
          className={`battle-projectile battle-projectile-${projectile.kind}${projectile.returning ? ' is-returning' : ''}`}
          style={{ left: `${projectile.position.x}%`, top: `${projectile.position.y}%` }}
          aria-hidden
        >{projectile.kind === 'pea' ? '●' : projectile.kind === 'watermelon' ? '🍉' : '🍌'}</span>
      ))}

      <div className="battle-feedback-layer" aria-live="polite">
        {state.feedback.filter((item) => item.position).map((item) => (
          <span
            key={item.id}
            className={`battle-feedback battle-feedback-${item.tone}`}
            style={{ left: `${item.position!.x}%`, top: `${item.position!.y}%` }}
          >{item.text}</span>
        ))}
      </div>

      {state.phase === 'between-waves' && (
        <div className="battle-banner">下一波倒计时 {Math.ceil(state.wave.betweenTimer)}</div>
      )}
    </section>
  )
}
