import { PLANT_CONFIG, WEAPON_META } from '../game/config'
import { getPlantStats } from '../game/economy'
import type { Plant } from '../game/types'

export interface PlantDetailsProps {
  plant: Plant | null
  onClose?: () => void
}

const formatNumber = (value: number) => Number.isInteger(value) ? `${value}` : value.toFixed(1)

export function PlantDetails({ plant, onClose }: PlantDetailsProps) {
  if (!plant) {
    return (
      <aside className="economy-plant-details is-empty" aria-label="植物信息">
        <strong>植物信息</strong>
        <span>选择苗圃或场上的植物查看属性</span>
      </aside>
    )
  }

  const config = PLANT_CONFIG[plant.kind]
  const stats = getPlantStats(plant)
  const weapon = plant.weapon ? WEAPON_META[plant.weapon] : null
  return (
    <aside className="economy-plant-details" aria-label={`${config.name}信息`}>
      <header>
        <span className="economy-details-emoji" aria-hidden>{config.emoji}</span>
        <div><strong>{config.name}</strong><small>{'★'.repeat(plant.star)}</small></div>
        {onClose && <button type="button" className="economy-close" onClick={onClose} aria-label="关闭植物信息">×</button>}
      </header>
      <dl>
        {plant.kind === 'sunflower' ? (
          <>
            <div><dt>生产</dt><dd>{stats.production} 阳光</dd></div>
            <div><dt>间隔</dt><dd>{formatNumber(stats.interval)} 秒</dd></div>
          </>
        ) : (
          <>
            <div><dt>伤害</dt><dd>{formatNumber(stats.damage)}</dd></div>
            <div><dt>间隔</dt><dd>{formatNumber(stats.interval)} 秒</dd></div>
            <div><dt>范围</dt><dd>{formatNumber(stats.range)}</dd></div>
          </>
        )}
        <div><dt>武器</dt><dd>{weapon ? `${weapon.emoji} ${weapon.name}` : '无'}</dd></div>
      </dl>
    </aside>
  )
}
