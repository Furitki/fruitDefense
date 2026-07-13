import { createReadStream, existsSync, statSync } from 'node:fs'
import http from 'node:http'
import { extname, join, normalize, resolve } from 'node:path'

const rootDir = resolve(process.env.STATIC_ROOT ?? './dist')
const port = Number(process.env.PORT ?? 3000)

const mimeTypes = {
  '.css': 'text/css; charset=utf-8',
  '.html': 'text/html; charset=utf-8',
  '.ico': 'image/x-icon',
  '.js': 'text/javascript; charset=utf-8',
  '.json': 'application/json; charset=utf-8',
  '.map': 'application/json; charset=utf-8',
  '.png': 'image/png',
  '.svg': 'image/svg+xml',
  '.txt': 'text/plain; charset=utf-8',
  '.webp': 'image/webp',
  '.woff': 'font/woff',
  '.woff2': 'font/woff2',
}

const sendFile = (response, filePath) => {
  response.writeHead(200, {
    'Cache-Control': extname(filePath) === '.html' ? 'no-cache' : 'public, max-age=31536000, immutable',
    'Content-Type': mimeTypes[extname(filePath)] ?? 'application/octet-stream',
  })
  createReadStream(filePath).pipe(response)
}

const server = http.createServer((request, response) => {
  const url = new URL(request.url ?? '/', `http://${request.headers.host ?? 'localhost'}`)
  const requestPath = decodeURIComponent(url.pathname)
  const safePath = normalize(requestPath).replace(/^(\.\.[/\\])+/, '')
  const resolvedPath = resolve(join(rootDir, safePath))

  if (!resolvedPath.startsWith(rootDir)) {
    response.writeHead(403, { 'Content-Type': 'text/plain; charset=utf-8' })
    response.end('Forbidden')
    return
  }

  let filePath = resolvedPath
  if (existsSync(filePath) && statSync(filePath).isDirectory()) {
    filePath = join(filePath, 'index.html')
  }

  if (existsSync(filePath) && statSync(filePath).isFile()) {
    sendFile(response, filePath)
    return
  }

  sendFile(response, join(rootDir, 'index.html'))
})

server.listen(port, '0.0.0.0', () => {
  console.log(`fruitDefense listening on http://0.0.0.0:${port} serving ${rootDir}`)
})
