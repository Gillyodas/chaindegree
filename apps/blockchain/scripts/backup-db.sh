#!/usr/bin/env bash
# ==============================================================================
# ChainDegree Database Backup Script
# Automatically creates SQL Server DB backup file (.bak)
# ==============================================================================

set -euo pipefail

BACKUP_DIR="${BACKUP_DIR:-/var/opt/mssql/backup}"
TIMESTAMP=$(date +"%Y%m%d_%H%M%S")
BACKUP_FILE="${BACKUP_DIR}/ChainDegree_Backup_${TIMESTAMP}.bak"

echo "[INFO] Starting ChainDegree Database Backup at $(date)..."

# Ensure backup directory exists inside container
docker exec chaindegree-sqlserver mkdir -p "${BACKUP_DIR}"

# Execute SQL Server backup command
docker exec chaindegree-sqlserver /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "${SA_PASSWORD:-YourStrongPass123!}" -C \
  -Q "BACKUP DATABASE [ChainDegree] TO DISK = N'${BACKUP_FILE}' WITH NOFORMAT, NOINIT, NAME = N'ChainDegree-Full', SKIP, NOREWIND, NOUNLOAD, STATS = 10"

echo "[SUCCESS] Database backup created at container path: ${BACKUP_FILE}"

# Retention Policy: Delete backup files older than 30 days
echo "[INFO] Cleaning up backups older than 30 days..."
docker exec chaindegree-sqlserver find "${BACKUP_DIR}" -type f -name "ChainDegree_Backup_*.bak" -mtime +30 -delete || true

echo "[SUCCESS] Backup workflow completed at $(date)."
