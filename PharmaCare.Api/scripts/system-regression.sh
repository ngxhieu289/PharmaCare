#!/usr/bin/env bash
set -euo pipefail

api="${PHARMACARE_API_URL:-http://127.0.0.1:5080/api}"
: "${PHARMACARE_ADMIN_PASSWORD:?Set PHARMACARE_ADMIN_PASSWORD}"
: "${PHARMACARE_MANAGER_PASSWORD:?Set PHARMACARE_MANAGER_PASSWORD}"
: "${PHARMACARE_PHARMACIST_PASSWORD:?Set PHARMACARE_PHARMACIST_PASSWORD}"
: "${PHARMACARE_WAREHOUSE_PASSWORD:?Set PHARMACARE_WAREHOUSE_PASSWORD}"
tmp_dir="$(mktemp -d)"
trap 'rm -rf "$tmp_dir"' EXIT

login() {
  curl -fsS "$api/auth/login" -H 'Content-Type: application/json' \
    -d "{\"email\":\"$1\",\"password\":\"$2\"}" | jq -r '.accessToken'
}
status() {
  curl -sS -o "$tmp_dir/body" -w '%{http_code}' "$@"
}
expect() {
  local expected="$1" actual="$2" label="$3"
  if [[ "$actual" != "$expected" ]]; then
    printf 'FAIL %-38s expected=%s actual=%s body=%s\n' "$label" "$expected" "$actual" "$(cat "$tmp_dir/body")"
    exit 1
  fi
  printf 'PASS %-38s HTTP %s\n' "$label" "$actual"
}
auth() { printf 'Authorization: Bearer %s' "$1"; }

admin="$(login admin "$PHARMACARE_ADMIN_PASSWORD")"
manager="$(login manager "$PHARMACARE_MANAGER_PASSWORD")"
pharmacist="$(login pharmacist "$PHARMACARE_PHARMACIST_PASSWORD")"
warehouse="$(login warehouse "$PHARMACARE_WAREHOUSE_PASSWORD")"

expect 200 "$(status "$api/auth/me" -H "$(auth "$admin")")" 'Admin /me'
expect 200 "$(status "$api/users?pageSize=5" -H "$(auth "$admin")")" 'Admin đọc tài khoản'
expect 200 "$(status "$api/roles" -H "$(auth "$admin")")" 'Admin đọc vai trò'
expect 200 "$(status "$api/permissions" -H "$(auth "$admin")")" 'Admin đọc ma trận quyền'
expect 200 "$(status "$api/audit-logs?pageSize=5" -H "$(auth "$admin")")" 'Admin đọc audit'
expect 403 "$(status "$api/users?pageSize=5" -H "$(auth "$manager")")" 'Manager không quản trị user'
expect 200 "$(status "$api/reports/dashboard" -H "$(auth "$manager")")" 'Manager đọc báo cáo'
expect 403 "$(status "$api/users?pageSize=5" -H "$(auth "$pharmacist")")" 'Dược sĩ không quản trị user'
expect 200 "$(status "$api/orders?pageSize=5" -H "$(auth "$pharmacist")")" 'Dược sĩ đọc đơn hàng'
expect 403 "$(status "$api/orders?pageSize=5" -H "$(auth "$warehouse")")" 'Nhân viên kho không đọc đơn'
expect 200 "$(status "$api/inventory?pageSize=5" -H "$(auth "$warehouse")")" 'Nhân viên kho đọc tồn'

latency="$(curl -sS -o /dev/null -w '%{time_total}' "$api/products?pageSize=20")"
jq -n --arg result PASS --arg latencySeconds "$latency" '{result:$result,roleBoundaryChecks:10,publicCatalogLatencySeconds:($latencySeconds|tonumber)}'
