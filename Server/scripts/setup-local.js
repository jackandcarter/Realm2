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

console.log('Local backend setup complete. Ensure MariaDB is running before starting the server.');
