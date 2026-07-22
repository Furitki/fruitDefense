import path from "node:path";
import { fileURLToPath } from "node:url";
import { buildKnowledgeBase } from "./build.mjs";

function parseCli(argv) {
  const options = { check: true };
  for (let index = 0; index < argv.length; index += 1) {
    const argument = argv[index];
    if (argument === "--root" || argument === "--config") {
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
    const result = buildKnowledgeBase(parseCli(process.argv.slice(2)));
    console.log("DESIGN_KB_VALIDATE_OK: " + result.documents.length + " documents, digest " + result.contentDigest.slice(0, 12));
  } catch (error) {
    console.error(error && error.message ? error.message : String(error));
    process.exitCode = 1;
  }
}
