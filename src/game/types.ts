export type Point = { x: number; y: number }
export type GridCoordinate = { column: number; row: number }
export type GridPoint = Point & GridCoordinate
export type PlantKind = 'pea' | 'watermelon' | 'banana' | 'durian' | 'sunflower'
export type WeaponKind = 'gatling' | 'ice' | 'chili'
export type ZombieKind = 'normal' | 'runner' | 'armored' | 'boss'
export type GamePhase = 'ready' | 'playing' | 'between-waves' | 'victory' | 'defeat'
export type GameSpeed = 1 | 2
export type Star = 1 | 2 | 3 | 4

export interface PlantConfig {
  kind: PlantKind
  name: string
  emoji: string
  damage: number
  interval: number
  range: number
  description: string
}

export interface Plant {
  id: string
  kind: PlantKind
  star: Star
  potId: string | null
  nurseryIndex: number | null
  weapon: WeaponKind | null
  attackCooldown: number
  productionProgress: number
  moveCooldown: number
  facing: Point
}

export interface Pot extends GridPoint {
  id: string
  active: boolean
}

export interface ZombieStatus {
  slowUntil: number
  freezeUntil: number
  iceHits: number
  burns: Array<{ remaining: number; damagePerSecond: number }>
}

export interface Zombie {
  id: string
  kind: ZombieKind
  hp: number
  maxHp: number
  speed: number
  pathProgress: number
  reward: number
  threat: number
  spawnOrder: number
  status: ZombieStatus
}

export interface Projectile {
  id: string
  kind: 'pea' | 'watermelon' | 'banana'
  plantId: string
  targetId: string | null
  position: Point
  targetPoint: Point
  direction: Point
  progress: number
  returning: boolean
  damage: number
  hitIds: string[]
  ttl: number
}

export interface Feedback {
  id: string
  text: string
  tone: 'sun' | 'damage' | 'info' | 'danger'
  position?: Point
  ttl: number
}

export interface WaveRuntime {
  index: number
  spawned: number
  total: number
  spawnCooldown: number
  betweenTimer: number
  started: boolean
}

export interface Inventory {
  weapons: Record<WeaponKind, number>
  pots: number
}

export type Selection =
  | { type: 'plant'; id: string }
  | { type: 'nursery'; id: string }
  | { type: 'weapon'; weapon: WeaponKind }
  | { type: 'pot-tool' }
  | { type: 'pot'; id: string }
  | null

export interface GameState {
  phase: GamePhase
  paused: boolean
  speed: GameSpeed
  elapsed: number
  sun: number
  lives: number
  refreshCount: number
  wave: WaveRuntime
  plants: Plant[]
  pots: Pot[]
  zombies: Zombie[]
  projectiles: Projectile[]
  inventory: Inventory
  selection: Selection
  feedback: Feedback[]
  nextId: number
  randomSeed: number
}

export type GameCommand =
  | { type: 'toggle-pause' }
  | { type: 'set-speed'; speed: GameSpeed }
  | { type: 'start-wave' }
  | { type: 'restart' }
  | { type: 'select'; selection: Selection }
  | { type: 'refresh' }
  | { type: 'place-plant'; plantId: string; potId: string }
  | { type: 'move-or-merge'; plantId: string; potId: string }
  | { type: 'install-weapon'; weapon: WeaponKind; plantId: string }
  | { type: 'expand-pot'; x: number; y: number }

export interface GameModule {
  step(state: GameState, deltaSeconds: number): GameState
  reduce(state: GameState, command: GameCommand): GameState
}
