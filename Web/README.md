# Realm2 Account Portal

A lightweight static site that lets players create new Realm2 accounts against the existing Express + SQLite backend. The page performs strong client-side validation and then calls the `/auth/register` endpoint exposed by the server.

## Getting started

1. **Copy the configuration template**
   ```bash
   cp config.example.js config.js
   ```
   Update `API_BASE_URL` to point at your locally running server (e.g. `http://localhost:3000`) or your deployed VPS address (e.g. `https://api.realm2.gg`).

2. **Serve the files locally (optional)**
   Any static file server works. For example, using `npx serve` from the `Web/` directory:
   ```bash
   npx serve .
   ```
   Then open the printed URL in your browser.

3. **Deploying**
   Upload the contents of the `Web/` directory to your preferred web host. Ensure `config.js` is included with the production API URL so that the registration form can reach the backend.

## Password requirements

The portal enforces the same rules as the backend:

- Minimum 8 characters
- At least one uppercase and one lowercase letter
- At least one number
- At least one special character

If the backend responds with an error (e.g. email already registered, username taken), the message is shown to the player.

## Integration notes

- Successful registrations return the user profile and tokens from the backend. The portal displays a welcome message so players know they can proceed to the game client.
- The registration form normalises email addresses to lowercase to avoid duplicates.
- The layout is responsive and uses no build tooling, making it easy to host from any static provider.
