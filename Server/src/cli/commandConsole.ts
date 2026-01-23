import readline from 'readline';
import { register } from '../services/authService';
import { logger } from '../observability/logger';
import { usernameRegex, validatePassword } from '../utils/authValidation';

type CommandHandler = (args: string[]) => Promise<void>;

const handlers: Record<string, CommandHandler> = {
  help: async () => {
    logger.info(
      [
        'Available commands:',
        '  help',
        '  create-user <email> <username> <password>',
        '  exit',
      ].join('\n')
    );
  },
  'create-user': async (args) => {
    const [email, username, password] = await resolveCreateUserArgs(args);
    if (!usernameRegex.test(username)) {
      logger.warn('Username must be 3-20 characters using letters, numbers, hyphens, or underscores.');
      return;
    }
    const passwordError = validatePassword(password);
    if (passwordError) {
      logger.warn(passwordError);
      return;
    }

    try {
      const result = await register(email, username, password);
      logger.info(
        {
          userId: result.user.id,
          email: result.user.email,
          username: result.user.username,
          accessToken: result.tokens.accessToken,
          refreshToken: result.tokens.refreshToken,
        },
        'User created via CLI.'
      );
    } catch (error) {
      logger.error({ err: error }, 'Failed to create user.');
    }
  },
  exit: async () => {
    logger.info('Shutting down command console.');
    process.exit(0);
  },
};

export function startCommandConsole(): void {
  if (!process.stdin.isTTY) {
    logger.info('Command console disabled (no TTY).');
    return;
  }

  const rl = readline.createInterface({
    input: process.stdin,
    output: process.stdout,
    prompt: 'realm2> ',
  });

  logger.info('Command console ready. Type "help" for commands.');
  rl.prompt();

  let busy = false;
  rl.on('line', async (line) => {
    if (busy) {
      logger.warn('Command still running. Please wait.');
      rl.prompt();
      return;
    }
    const trimmed = line.trim();
    if (!trimmed) {
      rl.prompt();
      return;
    }
    const [command, ...args] = trimmed.split(/\s+/);
    const handler = handlers[command];
    if (!handler) {
      logger.warn(`Unknown command: ${command}`);
      rl.prompt();
      return;
    }

    busy = true;
    rl.pause();
    try {
      await handler(args);
    } finally {
      busy = false;
      rl.resume();
      rl.prompt();
    }
  });

  rl.on('close', () => {
    logger.info('Command console closed.');
  });
}

async function resolveCreateUserArgs(args: string[]): Promise<[string, string, string]> {
  const [emailArg, usernameArg, passwordArg] = args;
  const email = await promptIfMissing('Email: ', emailArg);
  const username = await promptIfMissing('Username: ', usernameArg);
  const password = await promptIfMissing('Password: ', passwordArg);
  return [email.trim(), username.trim(), password];
}

function promptIfMissing(prompt: string, current?: string): Promise<string> {
  if (current && current.trim().length > 0) {
    return Promise.resolve(current);
  }
  return new Promise((resolve) => {
    process.stdout.write(prompt);
    process.stdin.once('data', (data) => {
      resolve(String(data).trim());
    });
  });
}
