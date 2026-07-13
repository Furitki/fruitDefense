import type { CSSProperties, DragEvent } from 'react'
import { EXPANSION_CANDIDATES, PLANT_CONFIG, WEAPON_META } from '../game/config'
import {
  canExpandPot,
  canInstallWeapon,
  describeWeaponEffect,
  getCoveragePreview,
  getCoverageRadiusPercent,
  getLegalExpansionCandidates,
} from '../game/equipment'
import type { GameCommand, GameState, PlantKind, Point, WeaponKind } from '../game/types'
import { WEAPON_DRAG_MIME } from './ToolInventory'
import './equipment.css'

export interface EquipmentPanelProps {
  state: GameState
  dispatch: (command: GameCommand) => void
  plantId?: string | null
  onClose?: () => void
  className?: string
}

const isWeaponKind = (value: string): value is WeaponKind => (
  value === 'gatling' || value === 'ice' || value === 'chili'
)

const readDraggedWeapon = (event: DragEvent<HTMLElement>): WeaponKind | null => {
  const value = event.dataTransfer.getData(WEAPON_DRAG_MIME)
    || event.dataTransfer.getData('text/plain')
  return isWeaponKind(value) ? value : null
}

export function EquipmentPanel({
  state,
  dispatch,
  plantId,
  onClose,
  className = '',
}: EquipmentPanelProps) {
  const selectedPlantId = plantId ?? (state.selection?.type === 'plant' ? state.selection.id : null)
  const plant = state.plants.find((candidate) => candidate.id === selectedPlantId)
  const selectedWeapon = state.selection?.type === 'weapon' ? state.selection.weapon : null

  if (!plant) {
    return (
      <aside className={`equipment-panel equipment-panel--empty ${className}`.trim()} aria-label="植物装备状态">
        <span aria-hidden="true">🧰</span>
        <p>选择一株植物查看武器效果</p>
      </aside>
    )
  }

  const config = PLANT_CONFIG[plant.kind]
  const targetState = selectedWeapon
    ? canInstallWeapon(state, selectedWeapon, plant.id) ? 'valid' : 'invalid'
    : 'idle'

  const install = (weapon: WeaponKind) => dispatch({ type: 'install-weapon', weapon, plantId: plant.id })

  return (
    <aside
      className={`equipment-panel equipment-target equipment-target--${targetState} ${className}`.trim()}
      aria-label={`${config.name}装备状态`}
      data-install-target={targetState}
      onDragOver={(event) => {
        if (readDraggedWeapon(event)) {
          event.preventDefault()
          event.dataTransfer.dropEffect = 'copy'
        }
      }}
      onDrop={(event) => {
        event.preventDefault()
        const weapon = readDraggedWeapon(event)
        if (weapon) install(weapon)
      }}
    >
      <header className="equipment-panel__header">
        <span className="equipment-panel__plant" aria-hidden="true">{config.emoji}</span>
        <div>
          <strong>{config.name} · {plant.star} 星</strong>
          <small>{plant.potId ? '已种植' : '苗圃中'} · 每株限装一种武器</small>
        </div>
        {onClose && <button type="button" className="equipment-panel__close" onClick={onClose} aria-label="关闭装备面板">×</button>}
      </header>

      {plant.weapon ? (
        <div className="equipment-status" data-weapon={plant.weapon}>
          <span className="equipment-status__emoji" aria-hidden="true">{WEAPON_META[plant.weapon].emoji}</span>
          <div>
            <strong>已安装：{WEAPON_META[plant.weapon].name}</strong>
            <p>{describeWeaponEffect(plant.weapon, plant.kind)}</p>
          </div>
          <span className="equipment-status__lock" title="武器不能直接拆卸">🔒</span>
        </div>
      ) : (
        <div className={`equipment-status equipment-status--${targetState}`}>
          <span className="equipment-status__emoji" aria-hidden="true">➕</span>
          <div>
            <strong>{selectedWeapon ? `可安装${WEAPON_META[selectedWeapon].name}` : '未安装武器'}</strong>
            <p>{selectedWeapon ? describeWeaponEffect(selectedWeapon, plant.kind) : '从工具栏拖入一种武器'}</p>
          </div>
          {selectedWeapon && (
            <button
              type="button"
              className="equipment-panel__install"
              disabled={!canInstallWeapon(state, selectedWeapon, plant.id)}
              onClick={() => install(selectedWeapon)}
            >安装</button>
          )}
        </div>
      )}

      <footer className="equipment-panel__footer">
        <span>{plant.weapon ? '武器会随植物参与合成，来源武器自动返回库存' : '武器安装后不能直接拆卸'}</span>
      </footer>
    </aside>
  )
}

export interface CoveragePreviewLayerProps {
  origin: Point
  plantKind: PlantKind
  facing?: Point
  label?: string
}

export function CoveragePreviewLayer({ origin, plantKind, facing, label }: CoveragePreviewLayerProps) {
  const preview = getCoveragePreview(plantKind, origin, facing)
  if (!preview) return null
  const radius = getCoverageRadiusPercent(plantKind)
  const direction = preview.direction
  const angle = direction ? Math.atan2(direction.y, direction.x) * 180 / Math.PI : 0
  const circleStyle: CSSProperties = {
    left: `${origin.x}%`,
    top: `${origin.y}%`,
    width: `${radius * 2}%`,
    aspectRatio: '1',
  }

  return (
    <div
      className={`coverage-preview coverage-preview--${plantKind}`}
      style={circleStyle}
      role="img"
      aria-label={label ?? `${PLANT_CONFIG[plantKind].name}约 ${preview.radiusInGrid} 格覆盖预览`}
    >
      <span className="coverage-preview__label">{preview.radiusInGrid} 格</span>
      {direction && (
        <span
          className="coverage-preview__direction"
          style={{ transform: `translateY(-50%) rotate(${angle}deg)` }}
          aria-hidden="true"
        />
      )}
    </div>
  )
}

export interface ExpansionOverlayProps {
  state: GameState
  dispatch: (command: GameCommand) => void
  previewKind?: PlantKind
  previewPoint?: Point | null
  showAllCandidates?: boolean
}

export function ExpansionOverlay({
  state,
  dispatch,
  previewKind,
  previewPoint = null,
  showAllCandidates = false,
}: ExpansionOverlayProps) {
  const active = state.selection?.type === 'pot-tool'
  const legal = getLegalExpansionCandidates(state)
  if (!active && !showAllCandidates && !previewPoint) return null

  return (
    <div className="expansion-overlay" aria-label="花盆扩建候选位置">
      {(active || showAllCandidates) && EXPANSION_CANDIDATES.map((candidate) => {
        const allowed = canExpandPot(state, candidate)
        return (
          <button
            key={`${candidate.x}-${candidate.y}`}
            type="button"
            className={`expansion-candidate ${allowed ? 'is-legal' : 'is-illegal'}`}
            style={{ left: `${candidate.x}%`, top: `${candidate.y}%` }}
            aria-label={`${allowed ? '可' : '不可'}扩建花盆：${candidate.x},${candidate.y}`}
            aria-disabled={!allowed}
            onClick={() => dispatch({ type: 'expand-pot', x: candidate.x, y: candidate.y })}
          >{allowed ? '+' : '×'}</button>
        )
      })}
      {active && <span className="expansion-overlay__summary">可扩建 {legal.length} 格</span>}
      {previewKind && previewPoint && (
        <CoveragePreviewLayer origin={previewPoint} plantKind={previewKind} />
      )}
    </div>
  )
}
