import { fireEvent, render, screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { App } from './App'
import { createInitialState } from './game/state'
import type { Plant } from './game/types'

describe('App', () => {
  it('renders the playable shell', () => {
    render(<App />)
    expect(screen.getByLabelText('游戏状态')).toBeInTheDocument()
    expect(screen.getByLabelText('果园战场')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /刷新植物/ })).toBeInTheDocument()
    expect(screen.queryByText(/出售/)).not.toBeInTheDocument()
    expect(screen.queryByText('出口')).not.toBeInTheDocument()
    expect(screen.getByLabelText('果园终点')).toHaveAttribute('data-path-end', '50:92')
    const soilCells = document.querySelectorAll('[data-cell-shape="square"]')
    expect(soilCells).toHaveLength(24)
  })

  it('completes refresh, placement, and wave start through the UI', () => {
    render(<App />)
    fireEvent.click(screen.getByRole('button', { name: '刷新植物☀️ 10' }))
    expect(screen.getByTestId('sun-count')).toHaveTextContent('70')
    expect(screen.getByRole('button', { name: '刷新植物☀️ 15' })).toHaveAttribute('aria-disabled', 'true')

    fireEvent.click(screen.getByRole('button', { name: '1 星西瓜，选择后点击花盆种植' }))
    fireEvent.click(screen.getByRole('button', { name: '空花盆 pot-1' }))
    expect(screen.getByRole('button', { name: '西瓜 1星' })).toBeInTheDocument()

    fireEvent.click(screen.getByRole('button', { name: '开始第 1 波' }))
    expect(screen.getByText(/第 1\/15 波/)).toBeInTheDocument()
  })

  it('merges two matching field fruits and reports the upgraded star', () => {
    render(<App />)
    fireEvent.click(screen.getByRole('button', { name: '刷新植物☀️ 10' }))

    const nurseryDurians = screen.getAllByRole('button', { name: '1 星榴莲，选择后点击花盆种植' })
    expect(nurseryDurians).toHaveLength(2)
    fireEvent.click(nurseryDurians[0])
    fireEvent.click(screen.getByRole('button', { name: '空花盆 pot-1' }))
    fireEvent.click(screen.getByRole('button', { name: '1 星榴莲，选择后点击花盆种植' }))
    fireEvent.click(screen.getByRole('button', { name: '空花盆 pot-2' }))

    const fieldDurians = screen.getAllByRole('button', { name: '榴莲 1星' })
    expect(fieldDurians).toHaveLength(2)
    fireEvent.click(fieldDurians[0])
    fireEvent.click(fieldDurians[1])

    expect(screen.getByRole('button', { name: '榴莲 2星' })).toBeInTheDocument()
    expect(screen.getByRole('status')).toHaveTextContent('榴莲合成成功，升为 2 星')
  })

  it('installs a weapon through the complete pointer drag path', () => {
    const plant: Plant = {
      id: 'pointer-plant',
      kind: 'pea',
      star: 1,
      potId: 'pot-1',
      nurseryIndex: null,
      weapon: null,
      attackCooldown: 0,
      productionProgress: 0,
      moveCooldown: 0,
      facing: { x: 1, y: 0 },
    }
    const initialState = {
      ...createInitialState(),
      plants: [plant],
      inventory: { weapons: { gatling: 1, ice: 0, chili: 0 }, pots: 0 },
    }
    render(<App initialState={initialState} />)

    const weapon = screen.getByRole('button', { name: /机枪，库存 1/ })
    const target = screen.getByRole('button', { name: '豌豆 1星' })
    let captured: number | null = null
    Object.defineProperties(weapon, {
      setPointerCapture: { value: (pointerId: number) => { captured = pointerId } },
      hasPointerCapture: { value: (pointerId: number) => captured === pointerId },
      releasePointerCapture: { value: () => { captured = null } },
    })
    const originalElementFromPoint = document.elementFromPoint
    Object.defineProperty(document, 'elementFromPoint', {
      configurable: true,
      value: vi.fn(() => target),
    })

    fireEvent.pointerDown(weapon, { button: 0, clientX: 10, clientY: 10, pointerId: 9, pointerType: 'touch' })
    fireEvent.pointerMove(weapon, { clientX: 40, clientY: 40, pointerId: 9, pointerType: 'touch' })
    fireEvent.pointerUp(weapon, { clientX: 80, clientY: 80, pointerId: 9, pointerType: 'touch' })

    expect(screen.getByRole('button', { name: /机枪，库存 0/ })).toBeDisabled()
    expect(target.querySelector('.battle-weapon-gatling')).toBeInTheDocument()
    Object.defineProperty(document, 'elementFromPoint', {
      configurable: true,
      value: originalElementFromPoint,
    })
  })
})
