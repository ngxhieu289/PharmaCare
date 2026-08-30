#!/usr/bin/env bash
set -euo pipefail

api="${PHARMACARE_API_URL:-http://127.0.0.1:5080/api}"
: "${PHARMACARE_ADMIN_PASSWORD:?Set PHARMACARE_ADMIN_PASSWORD before running this script}"

tmp_dir="$(mktemp -d)"
trap 'rm -rf "$tmp_dir"' EXIT

request_status() {
  local output_file="$1"
  shift
  curl -sS -o "$output_file" -w '%{http_code}' "$@"
}

expect_status() {
  local expected="$1" actual="$2" label="$3" body_file="$4"
  if [[ "$actual" != "$expected" ]]; then
    printf 'FAIL %-48s expected=%s actual=%s body=%s\n' \
      "$label" "$expected" "$actual" "$(cat "$body_file")"
    exit 1
  fi
  printf 'PASS %-48s HTTP %s\n' "$label" "$actual"
}

auth_header() {
  printf 'Authorization: Bearer %s' "$1"
}

login_json="$(curl -fsS "$api/auth/login" \
  -H 'Content-Type: application/json' \
  -d "{\"email\":\"admin\",\"password\":\"$PHARMACARE_ADMIN_PASSWORD\"}")"
admin_access="$(jq -er '.accessToken' <<<"$login_json")"

suffix="$(date +%s)"
customer_email="week1-${suffix}@pharmacare.local"
customer_username="week1-${suffix}"
customer_password='Customer@123456'
register_body="$tmp_dir/register.json"
register_status="$(request_status "$register_body" "$api/auth/register" \
  -H 'Content-Type: application/json' \
  -d "{\"email\":\"$customer_email\",\"username\":\"$customer_username\",\"password\":\"$customer_password\",\"displayName\":\"Week 1 Security Test\"}")"
expect_status 200 "$register_status" 'Đăng ký Customer phục vụ kiểm thử' "$register_body"

customer_access="$(jq -er '.accessToken' "$register_body")"
customer_refresh="$(jq -er '.refreshToken' "$register_body")"

me_body="$tmp_dir/me.json"
me_status="$(request_status "$me_body" "$api/auth/me" -H "$(auth_header "$customer_access")")"
expect_status 200 "$me_status" 'Access token mới truy cập /auth/me' "$me_body"
customer_id="$(jq -er '.id' "$me_body")"

lock_body="$tmp_dir/lock.json"
lock_status="$(request_status "$lock_body" -X PATCH "$api/users/$customer_id/status" \
  -H "$(auth_header "$admin_access")" \
  -H 'Content-Type: application/json' \
  -d '{"isActive":false}')"
expect_status 204 "$lock_status" 'Admin khóa tài khoản thử nghiệm' "$lock_body"

old_access_body="$tmp_dir/old-access.json"
old_access_status="$(request_status "$old_access_body" "$api/auth/me" \
  -H "$(auth_header "$customer_access")")"
expect_status 401 "$old_access_status" 'Access token cũ mất hiệu lực ngay' "$old_access_body"

old_refresh_body="$tmp_dir/old-refresh.json"
old_refresh_status="$(request_status "$old_refresh_body" -X POST "$api/auth/refresh" \
  -H 'Content-Type: application/json' \
  -d "{\"refreshToken\":\"$customer_refresh\"}")"
expect_status 401 "$old_refresh_status" 'Refresh token bị thu hồi khi khóa user' "$old_refresh_body"

unlock_body="$tmp_dir/unlock.json"
unlock_status="$(request_status "$unlock_body" -X PATCH "$api/users/$customer_id/status" \
  -H "$(auth_header "$admin_access")" \
  -H 'Content-Type: application/json' \
  -d '{"isActive":true}')"
expect_status 204 "$unlock_status" 'Admin mở lại tài khoản thử nghiệm' "$unlock_body"

customer_login="$(curl -fsS "$api/auth/login" \
  -H 'Content-Type: application/json' \
  -d "{\"email\":\"$customer_username\",\"password\":\"$customer_password\"}")"
rotated_from="$(jq -er '.refreshToken' <<<"$customer_login")"

rotation_body="$tmp_dir/rotation.json"
rotation_status="$(request_status "$rotation_body" -X POST "$api/auth/refresh" \
  -H 'Content-Type: application/json' \
  -d "{\"refreshToken\":\"$rotated_from\"}")"
expect_status 200 "$rotation_status" 'Refresh token rotation lần đầu' "$rotation_body"
rotated_to="$(jq -er '.refreshToken' "$rotation_body")"

replay_body="$tmp_dir/replay.json"
replay_status="$(request_status "$replay_body" -X POST "$api/auth/refresh" \
  -H 'Content-Type: application/json' \
  -d "{\"refreshToken\":\"$rotated_from\"}")"
expect_status 401 "$replay_status" 'Phát lại refresh token cũ bị từ chối' "$replay_body"

descendant_body="$tmp_dir/descendant.json"
descendant_status="$(request_status "$descendant_body" -X POST "$api/auth/refresh" \
  -H 'Content-Type: application/json' \
  -d "{\"refreshToken\":\"$rotated_to\"}")"
expect_status 401 "$descendant_status" 'Token thay thế cũng bị thu hồi khi phát hiện replay' "$descendant_body"

revoke_login="$(curl -fsS "$api/auth/login" \
  -H 'Content-Type: application/json' \
  -d "{\"email\":\"$customer_username\",\"password\":\"$customer_password\"}")"
revoke_refresh="$(jq -er '.refreshToken' <<<"$revoke_login")"
revoke_body="$tmp_dir/revoke.json"
revoke_status="$(request_status "$revoke_body" -X POST "$api/auth/revoke" \
  -H 'Content-Type: application/json' \
  -d "{\"refreshToken\":\"$revoke_refresh\"}")"
expect_status 200 "$revoke_status" 'Chủ động revoke refresh token' "$revoke_body"

after_revoke_body="$tmp_dir/after-revoke.json"
after_revoke_status="$(request_status "$after_revoke_body" -X POST "$api/auth/refresh" \
  -H 'Content-Type: application/json' \
  -d "{\"refreshToken\":\"$revoke_refresh\"}")"
expect_status 401 "$after_revoke_status" 'Refresh sau revoke bị từ chối' "$after_revoke_body"

printf '\nAll Week 1 authentication security checks passed.\n'
