import { fireEvent, render, screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { createInitialState } from '../game/state'
import type { GameState, Plant } from '../game/types'
import { Battlefield } from './Battlefield'

const plant: Plant = {
  id: 'test-plant',
  kind: 'watermelon',
  star: 1,
  potId: 'pot-1',
  nurseryIndex: null,
  weapon: null,
  attackCooldown: 0,
  productionProgress: 0,
  moveCooldown: 0,
  facing: { x: 1, y: 0 },
}

const weaponSelectedState = (): GameState => ({
  ...createInitialState(),
  plants: [plant],
  inventory: { weapons: { gatling: 1, ice: 0, chili: 0 }, pots: 0 },
  selection: { type: 'weapon', weapon: 'gatling' },
})

describe('Battlefield weapon targeting', () => {
  it('preserves the weapon selection through pointer down and installs on click', () => {
    const dispatch = vi.fn()
    render(<Battlefield state={weaponSelectedState()} dispatch={dispatch} />)
    const target = screen.getByRole('button', { name: '西瓜 1星' })
    Object.defineProperty(target, 'setPointerCapture', { value: vi.fn() })

    fireEvent.pointerDown(target, { button: 0, pointerId: 4, clientX: 20, clientY: 20 })
    fireEvent.click(target)

    expect(dispatch).not.toHaveBeenCalledWith({ type: 'select', selection: { type: 'plant', id: plant.id } })
    expect(dispatch).toHaveBeenCalledWith({ type: 'install-weapon', weapon: 'gatling', plantId: plant.id })
  })
})
