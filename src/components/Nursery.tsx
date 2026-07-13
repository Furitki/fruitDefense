import { PLANT_CONFIG } from '../game/config'
import { NURSERY_SIZE } from '../game/economy'
import type { Plant } from '../game/types'
import { useRef } from 'react'
import type { DragEvent, PointerEvent } from 'react'

export const PLANT_DRAG_MIME = 'application/x-fruit-defense-plant'

export interface PlantDragPayload {
  plantId: string
  source: 'nursery' | 'field'
}

export interface PointerPosition {
  clientX: number
  clientY: number
  pointerId: number
}

export interface PlantDragSession {
  payload: PlantDragPayload
  position: PointerPosition
  hoveredPotId: string | null
}

export interface NurseryPointerHandlers {
  onPointerDragStart?: (payload: PlantDragPayload, position: PointerPosition) => void
  onPointerDragMove?: (payload: PlantDragPayload, position: PointerPosition) => void
  onPointerDragEnd?: (payload: PlantDragPayload, position: PointerPosition) => void
  onPointerDragCancel?: (payload: PlantDragPayload) => void
  onDragVisualEnd?: () => void
}

export interface NurseryProps extends NurseryPointerHandlers {
  plants: Plant[]
  selectedPlantId?: string | null
  onSelect: (plant: Plant) => void
  onCancelDrop?: (payload: PlantDragPayload) => void
  draggingPlantId?: string | null
  returningPlantId?: string | null
}

export const writePlantDragPayload = (dataTransfer: DataTransfer, payload: PlantDragPayload) => {
  dataTransfer.effectAllowed = 'move'
  dataTransfer.setData(PLANT_DRAG_MIME, JSON.stringify(payload))
  dataTransfer.setData('text/plain', payload.plantId)
}

export const readPlantDragPayload = (dataTransfer: DataTransfer): PlantDragPayload | null => {
  const encoded = dataTransfer.getData(PLANT_DRAG_MIME)
  if (!encoded) return null
  try {
    const parsed = JSON.parse(encoded) as Partial<PlantDragPayload>
    if (typeof parsed.plantId !== 'string') return null
    if (parsed.source !== 'nursery' && parsed.source !== 'field') return null
    return { plantId: parsed.plantId, source: parsed.source }
  } catch {
    return null
  }
}

const pointerPosition = (event: PointerEvent<HTMLElement>): PointerPosition => ({
  clientX: event.clientX,
  clientY: event.clientY,
  pointerId: event.pointerId,
})

export function Nursery({
  plants,
  selectedPlantId,
  onSelect,
  onCancelDrop,
  onPointerDragStart,
  onPointerDragMove,
  onPointerDragEnd,
  onPointerDragCancel,
  onDragVisualEnd,
  draggingPlantId,
  returningPlantId,
}: NurseryProps) {
  const pointerDrag = useRef<{
    payload: PlantDragPayload
    startX: number
    startY: number
    started: boolean
  } | null>(null)
  const slots = new Map(plants
    .filter((plant) => plant.nurseryIndex !== null)
    .map((plant) => [plant.nurseryIndex as number, plant]))

  const cancelNativeDrop = (event: DragEvent<HTMLElement>) => {
    event.preventDefault()
    const payload = readPlantDragPayload(event.dataTransfer)
    if (payload) {
      onCancelDrop?.(payload)
      onPointerDragCancel?.(payload)
    }
  }

  return (
    <section
      className="economy-nursery"
      aria-label="植物苗圃"
      onDragOver={(event) => event.preventDefault()}
      onDrop={cancelNativeDrop}
    >
      {Array.from({ length: NURSERY_SIZE }, (_, index) => {
        const plant = slots.get(index)
        if (!plant) {
          return (
            <div className="economy-nursery-slot is-empty" key={index} aria-label={`苗圃空位 ${index + 1}`}>
              <span aria-hidden>＋</span>
              <small>空位</small>
            </div>
          )
        }

        const payload: PlantDragPayload = { plantId: plant.id, source: 'nursery' }
        const config = PLANT_CONFIG[plant.kind]
        return (
          <button
            type="button"
            className={`economy-nursery-slot is-filled${selectedPlantId === plant.id ? ' is-selected' : ''}${draggingPlantId === plant.id ? ' is-dragging' : ''}${returningPlantId === plant.id ? ' is-returning' : ''}`}
            key={plant.id}
            aria-label={`${plant.star} 星${config.name}，选择后点击花盆种植`}
            aria-pressed={selectedPlantId === plant.id}
            draggable
            onClick={() => onSelect(plant)}
            onDragStart={(event) => {
              onSelect(plant)
              writePlantDragPayload(event.dataTransfer, payload)
              onPointerDragStart?.(payload, { clientX: event.clientX, clientY: event.clientY, pointerId: -1 })
            }}
            onDrag={(event) => {
              if (event.clientX || event.clientY) {
                onPointerDragMove?.(payload, { clientX: event.clientX, clientY: event.clientY, pointerId: -1 })
              }
            }}
            onDragEnd={(event) => {
              if (event.dataTransfer.dropEffect === 'none') onPointerDragCancel?.(payload)
              else onDragVisualEnd?.()
            }}
            onPointerDown={(event) => {
              if (event.button !== 0) return
              event.currentTarget.setPointerCapture(event.pointerId)
              pointerDrag.current = { payload, startX: event.clientX, startY: event.clientY, started: false }
            }}
            onPointerMove={(event) => {
              if (!event.currentTarget.hasPointerCapture(event.pointerId)) return
              const active = pointerDrag.current
              if (!active) return
              if (!active.started && Math.hypot(event.clientX - active.startX, event.clientY - active.startY) > 8) {
                active.started = true
                onPointerDragStart?.(payload, pointerPosition(event))
              }
              if (active.started) onPointerDragMove?.(payload, pointerPosition(event))
            }}
            onPointerUp={(event) => {
              if (!event.currentTarget.hasPointerCapture(event.pointerId)) return
              if (pointerDrag.current?.started) onPointerDragEnd?.(payload, pointerPosition(event))
              pointerDrag.current = null
              event.currentTarget.releasePointerCapture(event.pointerId)
            }}
            onPointerCancel={(event) => {
              if (event.currentTarget.hasPointerCapture(event.pointerId)) {
                event.currentTarget.releasePointerCapture(event.pointerId)
              }
              if (pointerDrag.current?.started) onPointerDragCancel?.(payload)
              pointerDrag.current = null
            }}
          >
            <span className="economy-plant-emoji" aria-hidden>{config.emoji}</span>
            <strong>{config.name}</strong>
            <small>{'★'.repeat(plant.star)}</small>
          </button>
        )
      })}
    </section>
  )
}
