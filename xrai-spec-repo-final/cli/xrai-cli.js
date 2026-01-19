#!/usr/bin/env node
const fs = require('fs');

const validate = (filePath) => {
  try {
    const data = JSON.parse(fs.readFileSync(filePath));
    if (data.format !== 'XRAI') throw new Error('Invalid format type');
    console.log('✔ Valid XRAI file.');
  } catch (e) {
    console.error('✖ Validation failed:', e.message);
  }
};

const [, , cmd, file] = process.argv;
if (cmd === 'validate' && file) validate(file);
else console.log('Usage: xrai-cli.js validate <file.xrai.json>');
