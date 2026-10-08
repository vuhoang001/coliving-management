#!/usr/bin/env bash
# Nạp dữ liệu từ ./backup vào máy mới (PostgreSQL + ảnh MinIO).
# Chạy SAU khi đã copy source + ./backup sang: ./scripts/import-data.sh
set -euo pipefail
cd "$(dirname "$0")/.."

DB=coliving-db
MINIO=coliving-minio
BUCKET=coliving
MC_USER=${MINIO_USER:-minioadmin}
MC_PASS=${MINIO_PASSWORD:-minioadmin}

[ -f backup/db.sql ] || { echo "❌ Không thấy backup/db.sql — bạn đã copy thư mục ./backup sang chưa?"; exit 1; }

echo "→ Khởi động DB + MinIO (chưa chạy API để không seed đè)..."
docker compose up -d db minio

echo "→ Chờ PostgreSQL sẵn sàng..."
until docker exec "$DB" pg_isready -U postgres -d coliving >/dev/null 2>&1; do sleep 1; done
sleep 3  # chờ MinIO nhận request

echo "→ [1/2] Restore PostgreSQL..."
docker exec -i "$DB" psql -U postgres -d coliving < backup/db.sql >/dev/null
echo "    ✓ đã nạp database"

echo "→ [2/2] Restore ảnh lên MinIO (bucket: $BUCKET)..."
docker exec "$MINIO" mc alias set local "http://localhost:9000" "$MC_USER" "$MC_PASS" >/dev/null 2>&1
docker exec "$MINIO" sh -c "mc mb -p local/$BUCKET; mc anonymous set download local/$BUCKET" >/dev/null 2>&1 || true
if [ -d "backup/minio/$BUCKET" ]; then
  docker exec "$MINIO" rm -rf /tmp/mc-import 2>/dev/null || true
  docker cp "backup/minio/$BUCKET" "$MINIO:/tmp/mc-import"
  docker exec "$MINIO" mc mirror --overwrite --quiet /tmp/mc-import "local/$BUCKET" >/dev/null 2>&1 || true
  echo "    ✓ đã nạp ảnh"
else
  echo "    (không có ảnh để nạp)"
fi

echo ""
echo "✅ Dữ liệu đã đồng bộ. Giờ chạy nốt để bật API + web:"
echo "     docker compose up -d --build"
