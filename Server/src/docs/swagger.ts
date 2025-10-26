import swaggerJSDoc from 'swagger-jsdoc';
import { getCanonicalRaceIds } from '../config/races';

const options: swaggerJSDoc.Options = {
  definition: {
    openapi: '3.0.3',
    info: {
      title: 'Realm2 Auth API',
      version: '1.0.0',
      description: 'Authentication service for Realm2 project',
    },
    components: {
      securitySchemes: {
        bearerAuth: {
          type: 'http',
          scheme: 'bearer',
          bearerFormat: 'JWT',
        },
      },
      schemas: {
        AuthTokens: {
          type: 'object',
          properties: {
            accessToken: { type: 'string' },
            refreshToken: { type: 'string' },
          },
          required: ['accessToken', 'refreshToken'],
        },
        AuthUser: {
          type: 'object',
          properties: {
            id: { type: 'string', format: 'uuid' },
            email: { type: 'string', format: 'email' },
            username: { type: 'string' },
            createdAt: { type: 'string', format: 'date-time' },
          },
          required: ['id', 'email', 'username', 'createdAt'],
        },
        AuthResponse: {
          type: 'object',
          properties: {
            user: { $ref: '#/components/schemas/AuthUser' },
            tokens: { $ref: '#/components/schemas/AuthTokens' },
          },
          required: ['user', 'tokens'],
        },
        CharacterAppearance: {
          type: 'object',
          description: 'Stored customization values for a character',
          properties: {
            height: {
              type: 'number',
              format: 'float',
              description: 'Height slider value (meters)',
            },
            build: {
              type: 'number',
              format: 'float',
              description: 'Body build slider value',
            },
          },
          additionalProperties: true,
        },
        Character: {
          type: 'object',
          properties: {
            id: { type: 'string', format: 'uuid' },
            userId: { type: 'string', format: 'uuid' },
            realmId: { type: 'string' },
            name: { type: 'string' },
            bio: { type: 'string', nullable: true },
            raceId: {
              type: 'string',
              enum: getCanonicalRaceIds(),
              description: 'Canonical race identifier',
            },
            appearance: {
              $ref: '#/components/schemas/CharacterAppearance',
            },
            createdAt: { type: 'string', format: 'date-time' },
          },
          required: ['id', 'userId', 'realmId', 'name', 'raceId', 'appearance', 'createdAt'],
        },
        CreateCharacterRequest: {
          type: 'object',
          required: ['realmId', 'name'],
          properties: {
            realmId: { type: 'string' },
            name: { type: 'string' },
            bio: {
              type: 'string',
              description: 'Optional character biography',
            },
            raceId: {
              type: 'string',
              enum: getCanonicalRaceIds(),
              description: 'Canonical race identifier to associate with the character',
            },
            appearance: {
              $ref: '#/components/schemas/CharacterAppearance',
            },
          },
        },
        CreateCharacterResponse: {
          type: 'object',
          properties: {
            character: { $ref: '#/components/schemas/Character' },
          },
          required: ['character'],
        },
      },
    },
  },
  apis: ['src/routes/*.ts'],
};

export const swaggerSpec = swaggerJSDoc(options);
