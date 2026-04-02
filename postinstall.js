// postinstall.js
// Creates required folders under ../Assets if they do not exist,
// and optionally copies example/template files.

const fs = require('fs');
const path = require('path');
const readline = require('readline');

const assetsDir = path.resolve(__dirname, '../');
const examplesDir = path.resolve(__dirname, 'Examples');

const folders = [
  'Cutscenes',         // external/mod cutscene JSON files (runtime)
  'Resources/Cutscenes', // bundled cutscene JSON files (loaded via Resources.Load)
  'Scripts'            // Lua scripts (used by MapLoaderFramework integration)
];

// Create folders if they do not exist
folders.forEach(folder => {
  const fullPath = path.join(assetsDir, folder);
  if (!fs.existsSync(fullPath)) {
    fs.mkdirSync(fullPath, { recursive: true });
    console.log(`Created folder: ${fullPath}`);
  } else {
    console.log(`Folder already exists: ${fullPath}`);
  }
});

// Helper to copy a file with overwrite prompt
function copyFileWithPrompt(src, dest, rl, cb) {
  if (fs.existsSync(dest)) {
    rl.question(`File ${dest} exists. Overwrite? (y/N): `, answer => {
      if (answer.trim().toLowerCase() === 'y') {
        fs.copyFileSync(src, dest);
        console.log(`Overwritten: ${dest}`);
      } else {
        console.log(`Skipped: ${dest}`);
      }
      cb();
    });
  } else {
    fs.copyFileSync(src, dest);
    console.log(`Copied: ${dest}`);
    cb();
  }
}

// Recursively copy template files from Examples to Assets with user confirmation
function copyTemplates() {
  if (!fs.existsSync(examplesDir)) {
    console.log('No Examples directory found. Skipping template copy.');
    return;
  }

  const rl = readline.createInterface({ input: process.stdin, output: process.stdout });
  rl.question('Copy example files from CutsceneManager/Examples to your Assets folders? (y/N): ', answer => {
    if (answer.trim().toLowerCase() !== 'y') {
      console.log('Template copy skipped.');
      rl.close();
      return;
    }

    function walkDir(dir, relBase = '') {
      let results = [];
      fs.readdirSync(dir, { withFileTypes: true }).forEach(entry => {
        const relPath = path.join(relBase, entry.name);
        const absPath = path.join(dir, entry.name);
        if (entry.isDirectory()) {
          results = results.concat(walkDir(absPath, relPath));
        } else {
          results.push({ relPath, absPath });
        }
      });
      return results;
    }

    const files = walkDir(examplesDir);
    let idx = 0;

    function next() {
      if (idx >= files.length) {
        console.log('Template copy complete.');
        rl.close();
        return;
      }
      const { relPath, absPath } = files[idx++];
      const dest = path.join(assetsDir, relPath);
      const destDir = path.dirname(dest);
      if (!fs.existsSync(destDir)) fs.mkdirSync(destDir, { recursive: true });
      copyFileWithPrompt(absPath, dest, rl, next);
    }

    next();
  });
}

copyTemplates();
