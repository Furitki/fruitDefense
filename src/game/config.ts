import type { GridCoordinate, GridPoint, PlantConfig, PlantKind, Point, Pot, Star, WeaponKind, ZombieKind } from './types'

export const MAX_WAVES = 15

export const PLANT_CONFIG: Record<PlantKind, PlantConfig> = {
  pea: { kind: 'pea', name: '豌豆', emoji: '🌱', damage: 10, interval: 1, range: 44, description: '稳定的单体远程输出' },
  watermelon: { kind: 'watermelon', name: '西瓜', emoji: '🍉', damage: 22, interval: 2.2, range: 44, description: '低频范围爆炸伤害' },
  banana: { kind: 'banana', name: '香蕉', emoji: '🍌', damage: 8, interval: 1.6, range: 38, description: '直线往返穿透攻击' },
  durian: { kind: 'durian', name: '榴莲', emoji: '🌵', damage: 18, interval: 1.8, range: 18, description: '近战范围砸击' },
  sunflower: { kind: 'sunflower', name: '向日葵', emoji: '🌻', damage: 0, interval: 10, range: 0, description: '周期生产阳光' },
}

export const STAR_DAMAGE: Record<Star, number> = { 1: 1, 2: 1.8, 3: 3.2, 4: 5.6 }
export const STAR_SPEED: Record<Star, number> = { 1: 1, 2: 1.05, 3: 1.1, 4: 1.2 }
export const STAR_RANGE: Record<Star, number> = { 1: 1, 2: 1.05, 3: 1.1, 4: 1.15 }
export const SUNFLOWER_INTERVAL: Record<Star, number> = { 1: 10, 2: 9.5, 3: 9, 4: 8 }
export const SUNFLOWER_YIELD: Record<Star, number> = { 1: 1, 2: 2, 3: 4, 4: 7 }

export const PLANT_KINDS: PlantKind[] = ['pea', 'watermelon', 'banana', 'durian', 'sunflower']
export const WEAPON_META: Record<WeaponKind, { name: string; emoji: string; description: string }> = {
  gatling: { name: '机枪', emoji: '🔫', description: '攻速 +80%，单次伤害 -25%' },
  ice: { name: '冰块', emoji: '🧊', description: '减速并累计冻结' },
  chili: { name: '辣椒', emoji: '🌶️', description: '附加三层燃烧' },
}

export const ZOMBIE_META: Record<ZombieKind, { name: string; emoji: string; hp: number; speed: number; reward: number; threat: number }> = {
  normal: { name: '普通僵尸', emoji: '🧟', hp: 34, speed: 4.4, reward: 2, threat: 1 },
  runner: { name: '路障快尸', emoji: '🧟‍♂️', hp: 25, speed: 6.4, reward: 2, threat: 1 },
  armored: { name: '铁桶僵尸', emoji: '🧟‍♀️', hp: 80, speed: 3.4, reward: 4, threat: 2 },
  boss: { name: '园丁尸王', emoji: '👹', hp: 430, speed: 2.7, reward: 20, threat: 3 },
}

export const PATH_POINTS: Point[] = [
  { x: 50, y: 2 }, { x: 50, y: 9 }, { x: 76, y: 9 }, { x: 91, y: 17 },
  { x: 94, y: 40 }, { x: 93, y: 74 }, { x: 83, y: 88 }, { x: 53, y: 92 },
  { x: 22, y: 90 }, { x: 8, y: 78 }, { x: 6, y: 51 }, { x: 8, y: 22 },
  { x: 18, y: 11 }, { x: 42, y: 9 }, { x: 50, y: 2 }, { x: 50, y: 92 },
]

export const ORCHARD_DESTINATION: Point = PATH_POINTS[PATH_POINTS.length - 1]

export const BATTLE_GRID = { left: 17, top: 25, columnStep: 8, rowStep: 10 } as const

export const gridToPoint = ({ column, row }: GridCoordinate): GridPoint => ({
  column,
  row,
  x: BATTLE_GRID.left + column * BATTLE_GRID.columnStep,
  y: BATTLE_GRID.top + row * BATTLE_GRID.rowStep,
})

const LEFT_REGION: GridCoordinate[] = Array.from({ length: 4 }, (_, row) => (
  Array.from({ length: 3 }, (_, column) => ({ column, row }))
)).flat()
const RIGHT_REGION: GridCoordinate[] = Array.from({ length: 4 }, (_, row) => (
  Array.from({ length: 3 }, (_, offset) => ({ column: offset + 6, row }))
)).flat()

export const PLANTING_CELLS: GridPoint[] = [...LEFT_REGION, ...RIGHT_REGION].map(gridToPoint)

const INITIAL_GRID_CELLS: GridCoordinate[] = [
  { column: 0, row: 0 }, { column: 1, row: 0 }, { column: 2, row: 0 },
  { column: 0, row: 1 }, { column: 1, row: 1 },
  { column: 0, row: 2 }, { column: 1, row: 2 },
  { column: 6, row: 0 }, { column: 7, row: 0 }, { column: 8, row: 0 },
  { column: 7, row: 1 }, { column: 8, row: 1 }, { column: 8, row: 2 },
]

const initialCellKeys = new Set(INITIAL_GRID_CELLS.map(({ column, row }) => `${column}:${row}`))

export const INITIAL_POTS: Pot[] = INITIAL_GRID_CELLS.map((cell, index) => ({
  id: `pot-${index + 1}`,
  ...gridToPoint(cell),
  active: true,
}))

export const EXPANSION_CANDIDATES: GridPoint[] = PLANTING_CELLS.filter(
  ({ column, row }) => !initialCellKeys.has(`${column}:${row}`),
)

export const refreshCost = (refreshCount: number) => 10 + refreshCount * 5
