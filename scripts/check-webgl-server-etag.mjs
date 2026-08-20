import { spawn } from 'node:child_process'
import { mkdtemp, rm, writeFile } from 'node:fs/promises'
import { tmpdir } from 'node:os'
import { dirname, join, resolve } from 'node:path'
import { fileURLToPath } from 'node:url'

const scriptDir = dirname(fileURLToPath(import.meta.url))
const projectRoot = resolve(scriptDir, '..')
const serverPath = join(projectRoot, 'deploy', 'server.mjs')
const fixtureRoot = await mkdtemp(join(tmpdir(), 'fruit-defense-etag-'))
const fixturePath = join(fixtureRoot, 'index.html')
const port = 24000 + Math.floor(Math.random() * 10000)
const origin = `http://127.0.0.1:${port}/`
let server

const waitForServer = async () => {
  for (let attempt = 0; attempt < 50; attempt += 1) {
    try {
      return await fetch(origin)
    } catch {
      await new Promise((resolveDelay) => setTimeout(resolveDelay, 50))
    }
  }
  throw new Error(`Static server did not start on ${origin}`)
}

try {
  await writeFile(fixturePath, '<!doctype html><title>before</title>')
  server = spawn(process.execPath, [serverPath], {
    cwd: projectRoot,
    env: {
      ...process.env,
      PORT: String(port),
      STATIC_ROOT: fixtureRoot,
    },
    stdio: ['ignore', 'pipe', 'pipe'],
    windowsHide: true,
  })

  const first = await waitForServer()
  const firstBody = await first.text()
  const firstEtag = first.headers.get('etag')
  if (!first.ok || !firstEtag || !firstBody.includes('before')) {
    throw new Error('Initial static response is incomplete')
  }

  await writeFile(fixturePath, '<!doctype html><title>after replacement</title>')
  const second = await fetch(origin, {
    headers: { 'If-None-Match': firstEtag },
  })
  const secondBody = await second.text()
  const secondEtag = second.headers.get('etag')
  if (second.status !== 200) {
    throw new Error(`Expected 200 after in-place replacement, got ${second.status}`)
  }
  if (!secondEtag || secondEtag === firstEtag) {
    throw new Error('ETag did not change after in-place replacement')
  }
  if (!secondBody.includes('after replacement')) {
    throw new Error('Replacement body was not served')
  }

  console.log(`WEBGL_SERVER_ETAG_OK old=${firstEtag} new=${secondEtag}`)
} finally {
  if (server && server.exitCode === null) {
    server.kill()
    await new Promise((resolveExit) => server.once('exit', resolveExit))
  }
  await rm(fixtureRoot, { recursive: true, force: true })
}
