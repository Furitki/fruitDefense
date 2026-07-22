import crypto from "node:crypto";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const TOOL_VERSION = "0.2.1";
const ALLOWED_STATUSES = new Set(["draft", "active", "deprecated", "archived"]);
const IGNORED_DIRECTORIES = new Set(["node_modules", "logs"]);

export function buildKnowledgeBase(options = {}) {
  const root = path.resolve(options.root || process.cwd());
  const configRelative = normalizeRepoPath(options.config || "docs/design-kb.config.json", "config path");
  const configPath = resolveInsideRoot(root, configRelative);
  if (!fs.existsSync(configPath)) throw new Error("Missing Design KB config: " + configRelative);

  const config = normalizeConfig(JSON.parse(fs.readFileSync(configPath, "utf8")), root);
  const failures = [];
  const documents = readDocuments(root, config, failures);
  validateDocuments(documents, failures);
  if (failures.length) throw new Error("Design KB validation failed:\n- " + failures.join("\n- "));

  const digestInput = {
    schemaVersion: config.schemaVersion,
    kitVersion: config.kitVersion,
    projectId: config.projectId,
    title: config.title,
    contentRoots: config.contentRoots,
    exclude: config.exclude,
    documents: documents.map((document) => ({ path: document.path, source: document.source })),
  };
  const contentDigest = crypto.createHash("sha256").update(JSON.stringify(digestInput)).digest("hex");
  const dataset = {
    schemaVersion: 1,
    kitVersion: config.kitVersion,
    projectId: config.projectId,
    title: config.title,
    contentDigest,
    documents: documents.map((document) => ({
      path: document.path,
      title: document.title,
      id: document.metadata.id,
      parent: document.metadata.parent,
      order: Number(document.metadata.order),
      status: document.metadata.status,
      source: document.source,
    })),
  };

  const templatePath = path.join(path.dirname(fileURLToPath(import.meta.url)), "viewer.template.html");
  const template = fs.readFileSync(templatePath, "utf8").replace(/\r\n/g, "\n");
  const placeholder = "<!-- DESIGN_KB_DATA -->";
  if (template.split(placeholder).length !== 2) throw new Error("Viewer template must contain exactly one data placeholder");
  const serialized = JSON.stringify(dataset).replace(/</g, "\\u003c");
  const html = template.replace(placeholder, serialized);
  const outputPath = resolveInsideRoot(root, config.output);
  let written = false;

  if (options.check) {
    if (!fs.existsSync(outputPath)) throw new Error("Generated knowledge base is missing: " + config.output);
    const existing = fs.readFileSync(outputPath, "utf8").replace(/\r\n/g, "\n");
    if (existing !== html) {
      throw new Error("Generated knowledge base is stale: " + config.output + ". Run node tools/design-kb/build.mjs --root <repo-root>.");
    }
  } else if (options.write !== false) {
    const existing = fs.existsSync(outputPath) ? fs.readFileSync(outputPath, "utf8").replace(/\r\n/g, "\n") : null;
    if (existing !== html) {
      fs.mkdirSync(path.dirname(outputPath), { recursive: true });
      fs.writeFileSync(outputPath, html, "utf8");
      written = true;
    }
  }

  return { root, config, documents, contentDigest, html, outputPath, written };
}

function normalizeConfig(raw, root) {
  if (!raw || typeof raw !== "object" || Array.isArray(raw)) throw new Error("Design KB config must be a JSON object");
  if (raw.schemaVersion !== 1) throw new Error("Unsupported Design KB schemaVersion: " + String(raw.schemaVersion));
  const contentRoots = Array.isArray(raw.contentRoots) ? raw.contentRoots.map((value) => normalizeRepoPath(value, "content root")) : [];
  if (!contentRoots.length) throw new Error("Design KB config requires at least one content root");
  for (const contentRoot of contentRoots) resolveInsideRoot(root, contentRoot);
  const exclude = Array.isArray(raw.exclude) ? raw.exclude.map((value) => normalizeRepoPath(value, "exclude path")) : [];
  const output = normalizeRepoPath(raw.output || "docs/design-kb.html", "output path");
  if (!output.toLowerCase().endsWith(".html")) throw new Error("Design KB output must be an HTML file");
  resolveInsideRoot(root, output);
  const title = String(raw.title || "").trim();
  if (!title) throw new Error("Design KB config requires a title");
  const projectId = String(raw.projectId || "").trim();
  if (!/^[a-z0-9][a-z0-9-]{7,63}$/i.test(projectId)) throw new Error("Design KB config requires a valid projectId");
  return {
    schemaVersion: 1,
    kitVersion: String(raw.kitVersion || TOOL_VERSION),
    projectId,
    title,
    contentRoots,
    exclude: [...new Set(exclude)].sort(),
    output,
  };
}

function readDocuments(root, config, failures) {
  const byPath = new Map();
  for (const contentRoot of config.contentRoots) {
    const absoluteRoot = resolveInsideRoot(root, contentRoot);
    if (!fs.existsSync(absoluteRoot) || !fs.statSync(absoluteRoot).isDirectory()) {
      failures.push("Missing content root: " + contentRoot);
      continue;
    }
    walkMarkdown(root, absoluteRoot, config.exclude, byPath, failures);
  }
  const documents = [...byPath.values()].sort((left, right) => left.path.localeCompare(right.path, "en"));
  if (!documents.length) failures.push("No Markdown documents found in configured content roots");
  return documents;
}

function walkMarkdown(root, directory, exclude, output, failures) {
  const entries = fs.readdirSync(directory, { withFileTypes: true }).sort((left, right) => left.name.localeCompare(right.name, "en"));
  for (const entry of entries) {
    if (entry.name.startsWith(".") || IGNORED_DIRECTORIES.has(entry.name.toLowerCase())) continue;
    const absolute = path.join(directory, entry.name);
    const relative = toPosix(path.relative(root, absolute));
    if (isExcluded(relative, exclude)) continue;
    if (entry.isSymbolicLink()) {
      failures.push("Symlink is not allowed in Design KB content: " + relative);
      continue;
    }
    if (entry.isDirectory()) {
      walkMarkdown(root, absolute, exclude, output, failures);
      continue;
    }
    if (!entry.isFile() || !entry.name.toLowerCase().endsWith(".md")) continue;
    if (output.has(relative)) continue;
    const source = fs.readFileSync(absolute, "utf8").replace(/\r\n/g, "\n");
    output.set(relative, parseDocument(relative, source, failures));
  }
}

function parseDocument(relative, source, failures) {
  const metadata = {};
  let body = source;
  if (!source.startsWith("---\n")) {
    failures.push(relative + ": missing front matter");
  } else {
    const closing = source.indexOf("\n---\n", 4);
    if (closing < 0) {
      failures.push(relative + ": front matter has no closing delimiter");
    } else {
      const block = source.slice(4, closing);
      body = source.slice(closing + 5).replace(/^\n/, "");
      for (const rawLine of block.split("\n")) {
        if (!rawLine.trim() || rawLine.trimStart().startsWith("#")) continue;
        const match = rawLine.match(/^([A-Za-z0-9_-]+):(?:\s*(.*))?$/);
        if (!match) {
          failures.push(relative + ": invalid front matter line: " + rawLine);
          continue;
        }
        metadata[match[1]] = decodeScalar(match[2] || "");
      }
    }
  }
  for (const field of ["id", "parent", "order", "status"]) {
    if (!Object.prototype.hasOwnProperty.call(metadata, field)) failures.push(relative + ": missing front matter field " + field);
  }
  if (metadata.id && !/^[a-z0-9][a-z0-9-]*$/.test(metadata.id)) failures.push(relative + ": invalid id " + metadata.id);
  if (metadata.order !== undefined && !/^-?\d+$/.test(metadata.order)) failures.push(relative + ": order must be an integer");
  if (metadata.status && !ALLOWED_STATUSES.has(metadata.status)) failures.push(relative + ": unsupported status " + metadata.status);
  const titleMatch = body.match(/^#\s+(.+?)\s*$/m);
  if (!titleMatch) failures.push(relative + ": missing level-one heading");
  return { path: relative, source, body, metadata, title: titleMatch ? titleMatch[1].trim() : path.basename(relative, ".md") };
}

function validateDocuments(documents, failures) {
  const byId = new Map();
  const byPath = new Map(documents.map((document) => [document.path, document]));
  for (const document of documents) {
    if (!document.metadata.id) continue;
    const entries = byId.get(document.metadata.id) || [];
    entries.push(document);
    byId.set(document.metadata.id, entries);
  }
  for (const [id, entries] of byId) {
    if (entries.length > 1) failures.push("Duplicate id " + id + ": " + entries.map((entry) => entry.path).join(", "));
  }
  let rootCount = 0;
  for (const document of documents) {
    const parent = document.metadata.parent || "";
    if (!parent) rootCount += 1;
    if (parent === document.metadata.id) failures.push(document.path + ": parent cannot reference itself");
    if (parent && (byId.get(parent) || []).length !== 1) failures.push(document.path + ": parent must reference one unique id (" + parent + ")");
    validateParentCycle(document, byId, failures);
    validateRelativeLinks(document, byPath, failures);
  }
  if (!rootCount) failures.push("Knowledge base requires at least one root document");
}

function validateParentCycle(document, byId, failures) {
  const seen = new Set();
  let current = document;
  while (current && current.metadata.id) {
    if (seen.has(current.metadata.id)) {
      failures.push(document.path + ": parent cycle detected at " + current.metadata.id);
      return;
    }
    seen.add(current.metadata.id);
    const parent = current.metadata.parent || "";
    if (!parent) return;
    const matches = byId.get(parent) || [];
    if (matches.length !== 1) return;
    current = matches[0];
  }
}

function validateRelativeLinks(document, byPath, failures) {
  const searchable = document.body.replace(/```[\s\S]*?```/g, "");
  const pattern = /\[[^\]]+\]\(([^)\s]+)(?:\s+"[^"]*")?\)/g;
  for (const match of searchable.matchAll(pattern)) {
    const href = match[1];
    if (!href || href.startsWith("#") || /^[a-z][a-z0-9+.-]*:/i.test(href)) continue;
    let target;
    try { target = decodeURIComponent(href.split("#", 1)[0]); }
    catch { failures.push(document.path + ": invalid encoded link " + href); continue; }
    if (!target.toLowerCase().endsWith(".md")) continue;
    const resolved = path.posix.normalize(path.posix.join(path.posix.dirname(document.path), target.replace(/\\/g, "/")));
    if (path.posix.isAbsolute(resolved) || resolved === ".." || resolved.startsWith("../")) {
      failures.push(document.path + ": link escapes configured project paths: " + href);
    } else if (!byPath.has(resolved)) {
      failures.push(document.path + ": missing Markdown link target " + href);
    }
  }
}

function normalizeRepoPath(value, label) {
  const normalized = String(value || "").trim().replace(/\\/g, "/").replace(/^\.\//, "");
  if (!normalized || normalized.startsWith("/") || /^[A-Za-z]:/.test(normalized)) throw new Error("Invalid " + label + ": " + String(value));
  const parts = normalized.split("/");
  if (parts.some((part) => !part || part === "." || part === "..")) throw new Error("Invalid " + label + ": " + String(value));
  return parts.join("/");
}

function resolveInsideRoot(root, relative) {
  const absolute = path.resolve(root, ...relative.split("/"));
  const check = path.relative(root, absolute);
  if (check === ".." || check.startsWith(".." + path.sep) || path.isAbsolute(check)) throw new Error("Path escapes repository root: " + relative);
  return absolute;
}

function isExcluded(relative, exclude) {
  return exclude.some((entry) => relative === entry || relative.startsWith(entry + "/"));
}

function decodeScalar(value) {
  const trimmed = String(value).trim();
  if (trimmed.length >= 2 && trimmed[0] === trimmed.at(-1) && (trimmed[0] === "\"" || trimmed[0] === "'")) {
    return trimmed.slice(1, -1).replace(/\\\"/g, "\"").replace(/\\\\/g, "\\");
  }
  return trimmed;
}

function toPosix(value) {
  return value.replace(/\\/g, "/");
}

function parseCli(argv) {
  const options = {};
  for (let index = 0; index < argv.length; index += 1) {
    const argument = argv[index];
    if (argument === "--check") options.check = true;
    else if (argument === "--root" || argument === "--config") {
      const value = argv[index + 1];
      if (!value) throw new Error("Missing value for " + argument);
      options[argument.slice(2)] = value;
      index += 1;
    } else throw new Error("Unknown argument: " + argument);
  }
  return options;
}

const isMain = process.argv[1] && path.resolve(process.argv[1]) === path.resolve(fileURLToPath(import.meta.url));
if (isMain) {
  try {
    const options = parseCli(process.argv.slice(2));
    const result = buildKnowledgeBase(options);
    const label = options.check ? "DESIGN_KB_CHECK_OK" : "DESIGN_KB_BUILD_OK";
    const writeState = options.check ? "checked" : (result.written ? "written" : "unchanged");
    console.log(label + ": " + result.documents.length + " documents, digest " + result.contentDigest.slice(0, 12) + ", output " + result.config.output + " (" + writeState + ")");
  } catch (error) {
    console.error(error && error.message ? error.message : String(error));
    process.exitCode = 1;
  }
}
