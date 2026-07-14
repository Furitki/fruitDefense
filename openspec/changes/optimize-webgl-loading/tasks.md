## 1. Build Output Optimization

- [x] 1.1 Configure WebGL production builds for Brotli compression with JavaScript decompression fallback for the current HTTP origin.
- [x] 1.2 Enable high managed-code stripping for the WebGL target.
- [x] 1.3 Derive a content version from generated Unity payload files and append it to every loader/data/framework/WASM URL.
- [x] 1.4 Log the version, compression mode, total output size, and individual payload sizes after a successful build.

## 2. Static Delivery Policy

- [x] 2.1 Teach the static server to identify Brotli files and emit the correct content encoding and underlying MIME type.
- [x] 2.2 Apply one-year public immutable caching only to versioned `/Build/` assets while keeping HTML and unversioned resources non-immutable.
- [x] 2.3 Handle HEAD requests without streaming response bodies and preserve malformed/path traversal request safety.

## 3. Acceptance and Deployment

- [x] 3.1 Extract generated Unity asset URLs and their shared version from `index.html` during acceptance.
- [x] 3.2 Validate Brotli fallback containers, MIME types, immutable cache policy, HTML cache policy, and content lengths locally and publicly.
- [x] 3.3 Record delivery metadata in `acceptance.json` while retaining Unity startup and four-state portrait canvas validation.
- [x] 3.4 Update deployment health checks to validate the generated Brotli fallback WASM URL and cache headers.

## 4. Verification

- [x] 4.1 Run PowerShell and Node syntax checks for the modified delivery tooling.
- [x] 4.2 Run Unity smoke validation and produce a clean WebGL build.
- [x] 4.3 Compare previous and optimized payload sizes and run local portrait delivery acceptance.
- [x] 4.4 Deploy the optimized build and pass public delivery plus portrait canvas acceptance.
- [x] 4.5 Run strict OpenSpec validation and confirm all implementation tasks are complete.
