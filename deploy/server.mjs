import { createHash } from 'node:crypto'
import { createReadStream, existsSync, readFileSync, statSync } from 'node:fs'
import http from 'node:http'
import { basename, extname, join, normalize, resolve, sep } from 'node:path'

const rootDir = resolve(process.env.STATIC_ROOT ?? './dist')
const buildRoot = resolve(join(rootDir, 'Build'))
const port = Number(process.env.PORT ?? 3000)
const contentHashes = new Map()

const mimeTypes = {
  '.css': 'text/css; charset=utf-8',
  '.data': 'application/octet-stream',
  '.html': 'text/html; charset=utf-8',
  '.ico': 'image/x-icon',
  '.js': 'text/javascript; charset=utf-8',
  '.json': 'application/json; charset=utf-8',
  '.png': 'image/png',
  '.wasm': 'application/wasm',
}

const getContentHash = (filePath, stats) => {
  const fingerprint = `${stats.size}:${stats.mtimeMs}`
  const cached = contentHashes.get(filePath)
  if (cached?.fingerprint === fingerprint) {
    return cached.hash
  }

  const hash = createHash('sha256').update(readFileSync(filePath)).digest('hex')
  contentHashes.set(filePath, { fingerprint, hash })
  return hash
}

const sendFile = (request, response, filePath, url) => {
  const compressionExtension = extname(filePath)
  const compressed = compressionExtension === '.gz' || compressionExtension === '.br'
  const sourceExtension = compressed
    ? extname(basename(filePath, compressionExtension))
    : extname(filePath)
  const stats = statSync(filePath)
  const contentHash = getContentHash(filePath, stats)
  const etag = `"${contentHash}"`
  const isBuildFile = filePath === buildRoot || filePath.startsWith(`${buildRoot}${sep}`)
  const isVersionedBuildAsset = isBuildFile
    && url.pathname.startsWith('/Build/')
    && url.searchParams.get('v') === contentHash.slice(0, 12)
  const headers = {
    'Cache-Control': isVersionedBuildAsset
      ? 'public, max-age=31536000, immutable'
      : 'no-cache',
    'Content-Length': stats.size,
    'Content-Type': mimeTypes[sourceExtension] ?? 'application/octet-stream',
    ETag: etag,
  }
  if (compressed) {
    headers['Content-Encoding'] = compressionExtension === '.br' ? 'br' : 'gzip'
    headers.Vary = 'Accept-Encoding'
  }

  if (request.headers['if-none-match'] === etag) {
    response.writeHead(304, {
      'Cache-Control': headers['Cache-Control'],
      ETag: etag,
    })
    response.end()
    return
  }

  response.writeHead(200, headers)
  if (request.method === 'HEAD') {
    response.end()
    return
  }
  createReadStream(filePath).pipe(response)
}

const server = http.createServer((request, response) => {
  if (request.method !== 'GET' && request.method !== 'HEAD') {
    response.writeHead(405, {
      Allow: 'GET, HEAD',
      'Content-Type': 'text/plain; charset=utf-8',
    })
    response.end('Method Not Allowed')
    return
  }

  let url
  let requestPath
  try {
    url = new URL(request.url ?? '/', `http://${request.headers.host ?? 'localhost'}`)
    requestPath = decodeURIComponent(url.pathname)
  } catch {
    response.writeHead(400, { 'Content-Type': 'text/plain; charset=utf-8' })
    response.end('Bad Request')
    return
  }
  const safePath = normalize(requestPath).replace(/^(\.\.[/\\])+/, '')
  const resolvedPath = resolve(join(rootDir, safePath))

  if (resolvedPath !== rootDir && !resolvedPath.startsWith(`${rootDir}${sep}`)) {
    response.writeHead(403, { 'Content-Type': 'text/plain; charset=utf-8' })
    response.end('Forbidden')
    return
  }

  let filePath = resolvedPath
  if (existsSync(filePath) && statSync(filePath).isDirectory()) {
    filePath = join(filePath, 'index.html')
  }

  if (existsSync(filePath) && statSync(filePath).isFile()) {
    sendFile(request, response, filePath, url)
    return
  }

  sendFile(request, response, join(rootDir, 'index.html'), url)
})

server.listen(port, '0.0.0.0', () => {
  console.log(`fruitDefense WebGL listening on http://0.0.0.0:${port} serving ${rootDir}`)
})
