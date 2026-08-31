#!/bin/bash
# Standalone-compose equivalent of the schema/role slice this service owns
# in orchestration's cluster init script — same shape, one service only.
set -euo pipefail

psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" <<-EOSQL
  CREATE SCHEMA IF NOT EXISTS notifications;
  CREATE ROLE notifications_role LOGIN PASSWORD '$NOTIFICATIONS_DB_PASSWORD';
  ALTER ROLE notifications_role SET search_path TO notifications;
  GRANT USAGE, CREATE ON SCHEMA notifications TO notifications_role;
  GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA notifications TO notifications_role;
  ALTER DEFAULT PRIVILEGES IN SCHEMA notifications GRANT ALL PRIVILEGES ON TABLES TO notifications_role;
  ALTER DEFAULT PRIVILEGES IN SCHEMA notifications GRANT ALL PRIVILEGES ON SEQUENCES TO notifications_role;
EOSQL
