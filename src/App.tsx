import { useMemo, useState } from 'react'
import { BattleControls } from './components/BattleControls'
import { Battlefield } from './components/Battlefield'
import { EconomyDock } from './components/EconomyDock'
import type { DropFeedbackValue } from './components/EconomyDock'
import { CoveragePreviewLayer, EquipmentPanel, ExpansionOverlay } from './components/EquipmentPanel'
import { Hud } from './components/Hud'
import type { PlantDragPayload, PlantDragSession, PointerPosition } from './components/Nursery'
import { ToolInventory } from './components/ToolInventory'
import { PLANT_CONFIG, WEAPON_META } from './game/config'
import { getPlantDropStatus } from './game/economy'
import { canInstallWeapon } from './game/equipment'
import { useGameEngine } from './game/engine'
import type { GameState, PlantKind, WeaponKind } from './game/types'

const PREVIEW_KINDS: PlantKind[] = ['pea', 'watermelon', 'banana', 'durian', 'sunflower']

const dragPreviewPosition = (position: PointerPosition) => {
  const width = window.innerWidth
  const height = window.innerHeight
  const offsetX = position.clientX > width - 110 ? -54 : 54
  const offsetY = position.clientY < 90 ? 54 : -54
  return {
    left: Math.max(44, Math.min(width - 44, position.clientX + offsetX)),
    top: Math.max(44, Math.min(height - 44, position.clientY + offsetY)),
  }
}

export function App({ initialState }: { initialState?: GameState } = {}) {
  const { state, dispatch } = useGameEngine(initialState)
  const [dropFeedback, setDropFeedback] = useState<DropFeedbackValue | null>(null)
  const [previewKind, setPreviewKind] = useState<PlantKind>('pea')
  const [dragSession, setDragSession] = useState<PlantDragSession | null>(null)
  const [weaponDragSession, setWeaponDragSession] = useState<{
    weapon: WeaponKind
    position: PointerPosition
    hoveredPlantId: string | null
  } | null>(null)
  const [dropPulse, setDropPulse] = useState<{ potId: string; action: 'plant' | 'move' | 'merge'; key: number } | null>(null)
  const [returningPlantId, setReturningPlantId] = useState<string | null>(null)
  const ended = state.phase === 'victory' || state.phase === 'defeat'
  const selectedPotId = state.selection?.type === 'pot' ? state.selection.id : null
  const selectedPlantId = state.selection?.type === 'plant' || state.selection?.type === 'nursery'
    ? state.selection.id
    : null
  const selectedPot = selectedPotId ? state.pots.find((pot) => pot.id === selectedPotId) ?? null : null
  const previewPoint = useMemo(() => selectedPot ? { x: selectedPot.x, y: selectedPot.y } : null, [selectedPot])
  const dragPlant = dragSession ? state.plants.find((plant) => plant.id === dragSession.payload.plantId) ?? null : null
  const dragPot = dragSession?.hoveredPotId ? state.pots.find((pot) => pot.id === dragSession.hoveredPotId) ?? null : null
  const dragStatus = dragPlant && dragPot ? getPlantDropStatus(state, dragPlant.id, dragPot.id) : null
  const weaponDragPlant = weaponDragSession?.hoveredPlantId
    ? state.plants.find((plant) => plant.id === weaponDragSession.hoveredPlantId) ?? null
    : null
  const weaponDragLegal = weaponDragSession && weaponDragPlant
    ? canInstallWeapon(state, weaponDragSession.weapon, weaponDragPlant.id)
    : false

  const reportDrop = (value: DropFeedbackValue) => {
    setDropFeedback(value)
    window.setTimeout(() => setDropFeedback(null), 1500)
  }

  const potIdAt = (position: PointerPosition) => {
    const element = document.elementFromPoint(position.clientX, position.clientY)
    const potElement = element?.closest('[data-pot-id]') as HTMLElement | null
    return potElement?.dataset.potId ?? null
  }

  const plantIdAt = (position: PointerPosition) => {
    const element = document.elementFromPoint(position.clientX, position.clientY)
    const plantElement = element?.closest('[data-plant-id]') as HTMLElement | null
    return plantElement?.dataset.plantId ?? null
  }

  const beginPlantDrag = (payload: PlantDragPayload, position: PointerPosition) => {
    setDragSession({ payload, position, hoveredPotId: potIdAt(position) })
    setDropFeedback({ text: '拖动中：绿色可放置，金色可合成', tone: 'info' })
  }

  const movePlantDrag = (payload: PlantDragPayload, position: PointerPosition) => {
    const hoveredPotId = potIdAt(position)
    setDragSession({ payload, position, hoveredPotId })
    if (!hoveredPotId) {
      setDropFeedback({ text: '松开将取消移动', tone: 'info' })
      return
    }
    const status = getPlantDropStatus(state, payload.plantId, hoveredPotId)
    setDropFeedback({ text: status.reason, tone: status.legal ? 'valid' : status.action === 'cancel' ? 'info' : 'invalid' })
  }

  const pulseReturn = (plantId: string) => {
    setReturningPlantId(plantId)
    window.setTimeout(() => setReturningPlantId(null), 500)
  }

  const cancelPlantDrag = (payload: PlantDragPayload) => {
    setDragSession(null)
    pulseReturn(payload.plantId)
    reportDrop({ text: '已取消，水果返回原位', tone: 'info' })
  }

  const completePlantDrop = (payload: PlantDragPayload, potId: string | null) => {
    if (!potId) {
      setDragSession(null)
      pulseReturn(payload.plantId)
      reportDrop({ text: '已取消，水果返回原位', tone: 'info' })
      return
    }
    const plant = state.plants.find((candidate) => candidate.id === payload.plantId)
    if (!plant) return
    const status = getPlantDropStatus(state, plant.id, potId)
    setDragSession(null)
    if (!status.legal || (status.action !== 'plant' && status.action !== 'move' && status.action !== 'merge')) {
      pulseReturn(payload.plantId)
      reportDrop({ text: status.reason, tone: status.action === 'cancel' ? 'info' : 'invalid' })
      return
    }
    dispatch(status.action === 'plant'
      ? { type: 'place-plant', plantId: plant.id, potId }
      : { type: 'move-or-merge', plantId: plant.id, potId })
    setDropPulse({ potId, action: status.action, key: Date.now() })
    window.setTimeout(() => setDropPulse(null), 650)
    const text = status.action === 'merge'
      ? `${PLANT_CONFIG[plant.kind].name}合成成功，升为 ${plant.star + 1} 星！`
      : status.action === 'plant' ? '水果已稳稳种下' : '水果已移动到新花盆'
    reportDrop({ text, tone: 'valid' })
  }

  const onPointerPlantDrop = (payload: PlantDragPayload, position: PointerPosition) => {
    completePlantDrop(payload, potIdAt(position))
  }

  const beginWeaponDrag = (weapon: WeaponKind, position: PointerPosition) => {
    const hoveredPlantId = plantIdAt(position)
    setWeaponDragSession({ weapon, position, hoveredPlantId })
    setDropFeedback({ text: '拖动武器：绿色植物可以安装', tone: 'info' })
  }

  const moveWeaponDrag = (weapon: WeaponKind, position: PointerPosition) => {
    const hoveredPlantId = plantIdAt(position)
    setWeaponDragSession({ weapon, position, hoveredPlantId })
    const plant = hoveredPlantId ? state.plants.find((candidate) => candidate.id === hoveredPlantId) : null
    if (!plant) {
      setDropFeedback({ text: '请拖到一株植物上', tone: 'info' })
      return
    }
    const legal = canInstallWeapon(state, weapon, plant.id)
    setDropFeedback({
      text: legal ? `松开为${PLANT_CONFIG[plant.kind].name}安装${WEAPON_META[weapon].name}` : '这株植物已经装备武器',
      tone: legal ? 'valid' : 'invalid',
    })
  }

  const cancelWeaponDrag = () => {
    setWeaponDragSession(null)
    reportDrop({ text: '已取消安装，武器返回库存', tone: 'info' })
  }

  const completeWeaponDrop = (weapon: WeaponKind, plantId: string | null) => {
    setWeaponDragSession(null)
    const plant = plantId ? state.plants.find((candidate) => candidate.id === plantId) : null
    if (!plant) {
      reportDrop({ text: '已取消安装，武器返回库存', tone: 'info' })
      return
    }
    if (!canInstallWeapon(state, weapon, plant.id)) {
      reportDrop({ text: '这株植物不能安装该武器', tone: 'invalid' })
      return
    }
    dispatch({ type: 'install-weapon', weapon, plantId: plant.id })
    reportDrop({ text: `${WEAPON_META[weapon].name}安装成功`, tone: 'valid' })
  }

  return (
    <main className="app-shell">
      <div className="game-frame">
        <Hud state={state} dispatch={dispatch} />
        <div className="board-stack">
          <Battlefield
            state={state}
            dispatch={dispatch}
            onPointerPlantDrop={onPointerPlantDrop}
            onPlantDrop={(payload, potId) => completePlantDrop(payload, potId)}
            onPlantDragStart={beginPlantDrag}
            onPlantDragMove={movePlantDrag}
            onPlantDragVisualEnd={() => setDragSession(null)}
            onPlantDragCancel={cancelPlantDrag}
            onWeaponDrop={(weapon, plantId) => completeWeaponDrop(weapon, plantId)}
            dragSession={dragSession}
            weaponDragSession={weaponDragSession}
            dropPulse={dropPulse}
            returningPlantId={returningPlantId}
          />
          <ExpansionOverlay state={state} dispatch={dispatch} />
          {previewPoint && <CoveragePreviewLayer origin={previewPoint} plantKind={previewKind} />}
        </div>
        <BattleControls state={state} dispatch={dispatch} compact />

        <section className="build-panel" aria-label="构筑操作区">
          <ToolInventory
            state={state}
            dispatch={dispatch}
            onWeaponDragStart={beginWeaponDrag}
            onWeaponDragMove={moveWeaponDrag}
            onWeaponDragEnd={(weapon, position) => completeWeaponDrop(weapon, plantIdAt(position))}
            onWeaponDragCancel={cancelWeaponDrag}
            onDragVisualEnd={() => setWeaponDragSession(null)}
            draggingWeapon={weaponDragSession?.weapon}
          />
          {selectedPot && (
            <div className="placement-preview" aria-label="空花盆攻击覆盖预览">
              <strong>预览这个花盆：</strong>
              {PREVIEW_KINDS.map((kind) => (
                <button
                  key={kind}
                  type="button"
                  className={previewKind === kind ? 'is-selected' : ''}
                  aria-pressed={previewKind === kind}
                  onClick={() => setPreviewKind(kind)}
                >{PLANT_CONFIG[kind].emoji} {PLANT_CONFIG[kind].name}</button>
              ))}
            </div>
          )}
          <EconomyDock
            state={state}
            dispatch={dispatch}
            dropFeedback={dropFeedback}
            onPointerPlantDrop={onPointerPlantDrop}
            onPointerDragStart={beginPlantDrag}
            onPointerDragMove={movePlantDrag}
            onPointerDragCancel={cancelPlantDrag}
            onDragVisualEnd={() => setDragSession(null)}
            draggingPlantId={dragSession?.payload.plantId}
            returningPlantId={returningPlantId}
          />
          {(selectedPlantId || state.selection?.type === 'weapon') && (
            <EquipmentPanel
              state={state}
              dispatch={dispatch}
              plantId={selectedPlantId}
              onClose={() => dispatch({ type: 'select', selection: null })}
            />
          )}
        </section>

        {dragSession && dragPlant && (
          <div
            className={`plant-drag-ghost${dragStatus?.legal ? ` is-${dragStatus.action}` : dragStatus ? ' is-invalid' : ''}`}
            style={dragPreviewPosition(dragSession.position)}
            aria-hidden="true"
          >
            <span>{PLANT_CONFIG[dragPlant.kind].emoji}</span>
            <strong>{'★'.repeat(dragPlant.star)}</strong>
          </div>
        )}

        {weaponDragSession && (
          <div
            className={`weapon-drag-ghost${weaponDragPlant ? weaponDragLegal ? ' is-valid' : ' is-invalid' : ''}`}
            style={dragPreviewPosition(weaponDragSession.position)}
            aria-hidden="true"
          >
            <span>{WEAPON_META[weaponDragSession.weapon].emoji}</span>
          </div>
        )}

        {state.paused && !ended && (
          <div className="modal-backdrop">
            <section className="modal-card">
              <span className="modal-emoji">⏸️</span>
              <h1>游戏暂停</h1>
              <button type="button" onClick={() => dispatch({ type: 'toggle-pause' })}>继续守护果园</button>
            </section>
          </div>
        )}
        {ended && (
          <div className="modal-backdrop">
            <section className="modal-card">
              <span className="modal-emoji">{state.phase === 'victory' ? '🏆' : '🧟'}</span>
              <h1>{state.phase === 'victory' ? '果园守住了！' : '僵尸闯进果园了'}</h1>
              <p>坚持到第 {state.wave.index} 波</p>
              <button type="button" onClick={() => dispatch({ type: 'restart' })}>重新开始</button>
            </section>
          </div>
        )}
      </div>
    </main>
  )
}
