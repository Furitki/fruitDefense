import { fireEvent, render, screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { createInitialState } from '../game/state'
import type { GameState } from '../game/types'
import { ToolInventory } from './ToolInventory'

const stateWithWeapon = (): GameState => ({
  ...createInitialState(),
  inventory: { weapons: { gatling: 1, ice: 0, chili: 0 }, pots: 0 },
})

const mockPointerCapture = (element: HTMLElement) => {
  let captured: number | null = null
  Object.defineProperties(element, {
    setPointerCapture: { value: (pointerId: number) => { captured = pointerId } },
    hasPointerCapture: { value: (pointerId: number) => captured === pointerId },
    releasePointerCapture: { value: () => { captured = null } },
  })
}

describe('ToolInventory weapon pointer dragging', () => {
  it.each(['mouse', 'touch'] as const)('reports a complete %s pointer drag after the movement threshold', (pointerType) => {
    const dispatch = vi.fn()
    const onWeaponDragStart = vi.fn()
    const onWeaponDragMove = vi.fn()
    const onWeaponDragEnd = vi.fn()
    render(
      <ToolInventory
        state={stateWithWeapon()}
        dispatch={dispatch}
        onWeaponDragStart={onWeaponDragStart}
        onWeaponDragMove={onWeaponDragMove}
        onWeaponDragEnd={onWeaponDragEnd}
      />,
    )

    const weapon = screen.getByRole('button', { name: /机枪，库存 1/ })
    mockPointerCapture(weapon)
    fireEvent.pointerDown(weapon, { button: 0, clientX: 10, clientY: 20, pointerId: 7, pointerType })
    fireEvent.pointerMove(weapon, { clientX: 24, clientY: 24, pointerId: 7, pointerType })
    fireEvent.pointerUp(weapon, { clientX: 80, clientY: 90, pointerId: 7, pointerType })

    expect(onWeaponDragStart).toHaveBeenCalledWith('gatling', expect.objectContaining({ pointerId: 7 }))
    expect(onWeaponDragMove).toHaveBeenCalledWith('gatling', expect.objectContaining({ clientX: 24 }))
    expect(onWeaponDragEnd).toHaveBeenCalledWith('gatling', expect.objectContaining({ clientX: 80, clientY: 90 }))
    expect(dispatch).toHaveBeenCalledWith({ type: 'select', selection: { type: 'weapon', weapon: 'gatling' } })
  })

  it('keeps click-to-select as a fallback', () => {
    const dispatch = vi.fn()
    render(<ToolInventory state={stateWithWeapon()} dispatch={dispatch} />)
    fireEvent.click(screen.getByRole('button', { name: /机枪，库存 1/ }))
    expect(dispatch).toHaveBeenCalledWith({ type: 'select', selection: { type: 'weapon', weapon: 'gatling' } })
  })
})
