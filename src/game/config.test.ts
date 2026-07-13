import { describe, expect, it } from 'vitest'
import { BATTLE_GRID, ORCHARD_DESTINATION, PATH_POINTS, PLANTING_CELLS } from './config'

describe('battlefield geometry', () => {
  it('uses the final path point as the orchard destination', () => {
    expect(ORCHARD_DESTINATION).toBe(PATH_POINTS[PATH_POINTS.length - 1])
    expect(ORCHARD_DESTINATION).toEqual({ x: 50, y: 92 })
  })

  it('maps every planting cell to one canonical grid position', () => {
    const keys = PLANTING_CELLS.map((cell) => `${cell.column}:${cell.row}`)
    expect(new Set(keys).size).toBe(keys.length)
    for (const cell of PLANTING_CELLS) {
      expect(cell.x).toBe(BATTLE_GRID.left + cell.column * BATTLE_GRID.columnStep)
      expect(cell.y).toBe(BATTLE_GRID.top + cell.row * BATTLE_GRID.rowStep)
    }
  })
})
