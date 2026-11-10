-- PostgreSQL initialization
-- Runs only on first initialization of the database volume.
-- Database is created automatically using POSTGRES_DB.

CREATE EXTENSION IF NOT EXISTS pgcrypto;

CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

CREATE EXTENSION IF NOT EXISTS pg_trgm;