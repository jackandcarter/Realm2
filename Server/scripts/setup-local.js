const fs = require('fs');
const path = require('path');
const dotenv = require('dotenv');

const rootDir = process.cwd();
const envPath = path.join(rootDir, '.env');
const envExamplePath = path.join(rootDir, '.env.example');

if (!fs.existsSync(envPath)) {
  if (!fs.existsSync(envExamplePath)) {
    console.error('Missing .env.example. Cannot generate local .env file.');
    process.exit(1);
  }

  fs.copyFileSync(envExamplePath, envPath);
  console.log('Created .env from .env.example.');
} else {
  console.log('.env already exists. Skipping copy.');
}

dotenv.config({ path: envPath });

const dataDir = process.env.DATA_DIR ?? path.join(rootDir, 'data');
if (!fs.existsSync(dataDir)) {
  fs.mkdirSync(dataDir, { recursive: true });
  console.log(`Created data directory at ${dataDir}`);
}

const backupDir = process.env.DB_BACKUP_DIR ?? path.join(dataDir, 'backups');
if (!fs.existsSync(backupDir)) {
  fs.mkdirSync(backupDir, { recursive: true });
  console.log(`Created backup directory at ${backupDir}`);
}

console.log('Local backend setup complete. Run "npm run dev" to start the server.');
