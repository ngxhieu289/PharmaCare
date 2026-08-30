#!/usr/bin/env bash
set -euo pipefail
api="${PHARMACARE_API_URL:-http://127.0.0.1:5080/api}"
: "${PHARMACARE_ADMIN_PASSWORD:?Set PHARMACARE_ADMIN_PASSWORD}"
stamp="$(date +%s)"
token="$(curl -fsS "$api/auth/login" -H 'Content-Type: application/json' -d "$(jq -n --arg password "$PHARMACARE_ADMIN_PASSWORD" '{email:"admin",password:$password}')" | jq -r '.accessToken')"
auth="Authorization: Bearer $token"
audit_before="$(curl -fsS "$api/audit-logs?action=HTTP_POST&pageSize=1" -H "$auth" | jq -r '.totalItems')"

permission_id="$(curl -fsS "$api/permissions" -H "$auth" | jq -r '.[]|select(.code=="products.read")|.id')"
role="$(curl -fsS "$api/roles" -H "$auth" -H 'Content-Type: application/json' -d "$(jq -n --arg n "E2ERole$stamp" --arg p "$permission_id" '{name:$n,description:"Vai trò kiểm thử Admin",permissionIds:[$p]}')")"
role_id="$(jq -r '.id' <<<"$role")"

branch="$(curl -fsS "$api/branches" -H "$auth" -H 'Content-Type: application/json' -d "$(jq -n --arg c "E2E-$stamp" '{code:$c,name:("Chi nhánh kiểm thử "+$c),address:"123 Đường kiểm thử",phone:"0900000000",province:"Hà Nội",district:"Cầu Giấy",ward:"Dịch Vọng"}')")"
branch_id="$(jq -r '.id' <<<"$branch")"

category="$(curl -fsS "$api/categories" -H "$auth" -H 'Content-Type: application/json' -d "$(jq -n --arg n "Danh mục E2E $stamp" --arg s "danh-muc-e2e-$stamp" '{name:$n,slug:$s,parentId:null}')")"
category_id="$(jq -r '.id' <<<"$category")"

user="$(curl -fsS "$api/users" -H "$auth" -H 'Content-Type: application/json' -d "$(jq -n --arg e "admin-e2e-$stamp@example.test" '{email:$e,displayName:"Nhân viên Admin E2E",password:"AdminE2E@123",phone:"0911111111"}')")"
user_id="$(jq -r '.id' <<<"$user")"
curl -fsS -o /dev/null -X PUT "$api/users/$user_id/roles/$role_id" -H "$auth"
curl -fsS -o /dev/null -X PUT "$api/users/$user_id/branches/$branch_id?isPrimary=true" -H "$auth"
assigned="$(curl -fsS "$api/users/$user_id" -H "$auth")"
test "$(jq -r '.roles[0]' <<<"$assigned")" = "E2ERole$stamp"
test "$(jq -r '.branches[0].id' <<<"$assigned")" = "$branch_id"

curl -fsS -o /dev/null -X PATCH "$api/users/$user_id/status" -H "$auth" -H 'Content-Type: application/json' -d '{"isActive":false}'
curl -fsS -o /dev/null -X PATCH "$api/categories/$category_id/status" -H "$auth" -H 'Content-Type: application/json' -d '{"isActive":false}'
curl -fsS -o /dev/null -X PATCH "$api/branches/$branch_id/status" -H "$auth" -H 'Content-Type: application/json' -d '{"isActive":false}'
curl -fsS -o /dev/null -X DELETE "$api/users/$user_id/roles/$role_id" -H "$auth"
curl -fsS -o /dev/null -X DELETE "$api/roles/$role_id" -H "$auth"

audit_after="$(curl -fsS "$api/audit-logs?action=HTTP_POST&pageSize=1" -H "$auth" | jq -r '.totalItems')"
audit_count="$((audit_after-audit_before))"
test "$audit_count" -ge 4
jq -n --arg result PASS --arg user "$user_id" --arg branch "$branch_id" --arg category "$category_id" --argjson auditedCreates "$audit_count" '{result:$result,userCreatedAssignedAndLocked:$user,branchCreatedAndDisabled:$branch,categoryCreatedAndDisabled:$category,customRoleCreatedAssignedAndDeleted:true,auditedCreates:$auditedCreates}'
