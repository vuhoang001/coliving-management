#!/usr/bin/env bash
# Xuất TOÀN BỘ dữ liệu hiện có (PostgreSQL + ảnh MinIO) ra thư mục ./backup
# để mang sang máy khác. Chạy khi stack đang chạy: ./scripts/export-data.sh
set -euo pipefail
cd "$(dirname "$0")/.."

DB=coliving-db
MINIO=coliving-minio
BUCKET=coliving
MC_USER=${MINIO_USER:-minioadmin}
MC_PASS=${MINIO_PASSWORD:-minioadmin}

mkdir -p backup

echo "→ [1/2] Dump PostgreSQL..."
docker exec "$DB" pg_dump -U postgres -d coliving --clean --if-exists --no-owner > backup/db.sql
echo "    ✓ backup/db.sql ($(du -h backup/db.sql | cut -f1))"

echo "→ [2/2] Export ảnh trên MinIO (bucket: $BUCKET)..."
docker exec "$MINIO" mc alias set local "http://localhost:9000" "$MC_USER" "$MC_PASS" >/dev/null 2>&1
docker exec "$MINIO" sh -c "rm -rf /tmp/mc-export && mkdir -p /tmp/mc-export && mc mirror --overwrite --quiet local/$BUCKET /tmp/mc-export/$BUCKET" 2>/dev/null || true
rm -rf "backup/minio"
mkdir -p "backup/minio"
if docker cp "$MINIO:/tmp/mc-export/$BUCKET" "backup/minio/" 2>/dev/null; then
  echo "    ✓ backup/minio/$BUCKET ($(du -sh backup/minio 2>/dev/null | cut -f1))"
else
  echo "    (bucket rỗng — chưa có ảnh upload nào)"
fi

echo ""
echo "✅ Xong. Gửi CẢ thư mục dự án (bao gồm ./backup) sang máy khác,"
echo "   rồi ở máy đó chạy:  ./scripts/import-data.sh"
