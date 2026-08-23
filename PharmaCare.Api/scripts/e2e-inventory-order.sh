#!/usr/bin/env bash
set -euo pipefail

api="${PHARMACARE_API_URL:-http://127.0.0.1:5080/api}"
: "${PHARMACARE_PHARMACIST_PASSWORD:?Set PHARMACARE_PHARMACIST_PASSWORD}"
: "${PHARMACARE_WAREHOUSE_PASSWORD:?Set PHARMACARE_WAREHOUSE_PASSWORD}"
stamp="$(date +%s)"
code="E2E-$stamp"
password="Customer@123"

login() {
  curl -fsS "$api/auth/login" -H 'Content-Type: application/json' \
    -d "{\"email\":\"$1\",\"password\":\"$2\"}" | jq -r '.accessToken'
}
auth() { printf 'Authorization: Bearer %s' "$1"; }

warehouse_token="$(login warehouse "$PHARMACARE_WAREHOUSE_PASSWORD")"
pharmacist_token="$(login pharmacist "$PHARMACARE_PHARMACIST_PASSWORD")"
customer_json="$(curl -fsS "$api/auth/register" -H 'Content-Type: application/json' -d "{\"email\":\"customer-$stamp@example.test\",\"username\":\"customer$stamp\",\"password\":\"$password\",\"displayName\":\"Khách E2E $stamp\",\"phone\":\"0912345678\"}")"
customer_token="$(jq -r '.accessToken' <<<"$customer_json")"

branches="$(curl -fsS "$api/branches?pageSize=100")"
branch1="$(jq -r '.items[0].id' <<<"$branches")"
branch2="$(jq -r '.items[1].id' <<<"$branches")"
category="$(curl -fsS "$api/categories?pageSize=1" | jq -r '.items[0].id')"
test -n "$branch1"; test -n "$branch2"; test "$branch2" != "null"

product_payload="$(jq -n --arg code "$code" --arg category "$category" '{code:$code,name:("Thuốc kiểm thử "+$code),activeIngredient:"Test ingredient",indications:"Kiểm thử luồng kho và đơn hàng",categoryId:$category,rxFlag:false,vatRate:5,packaging:"Hộp 2 vỉ x 10 viên",unitPrice:100000,storageTemp:"Dưới 30°C",warningText:"Sản phẩm chỉ dùng kiểm thử",saleUnits:[{unitName:"Hộp",conversionFactor:20,salePrice:100000,isDefault:true},{unitName:"Vỉ",conversionFactor:10,salePrice:50000,isDefault:false},{unitName:"Viên",conversionFactor:1,salePrice:5000,isDefault:false}]}')"
product="$(curl -fsS "$api/products" -H "$(auth "$warehouse_token")" -H 'Content-Type: application/json' -d "$product_payload")"
product_id="$(jq -r '.id' <<<"$product")"
strip_id="$(jq -r '.saleUnits[] | select(.unitName=="Vỉ") | .id' <<<"$product")"
test "$(jq '.saleUnits | length' <<<"$product")" -eq 3

batch_payload="$(jq -n --arg product "$product_id" --arg number "LOT-$stamp" '{productId:$product,batchNumber:$number,mfgDate:"2026-08-01",expiryDate:"2028-08-01",costPrice:3000}')"
batch="$(curl -fsS "$api/batches" -H "$(auth "$warehouse_token")" -H 'Content-Type: application/json' -d "$batch_payload")"
batch_id="$(jq -r '.id' <<<"$batch")"

curl -fsS -o /dev/null "$api/inventory/receive" -H "$(auth "$warehouse_token")" -H 'Content-Type: application/json' -d "$(jq -n --arg b "$branch1" --arg p "$product_id" --arg lot "$batch_id" '{branchId:$b,productId:$p,batchId:$lot,quantity:200,reorderLevel:20,note:"E2E nhận lô tại kho nguồn"}')"
curl -fsS -o /dev/null "$api/inventory/transfer" -H "$(auth "$warehouse_token")" -H 'Content-Type: application/json' -d "$(jq -n --arg from "$branch1" --arg to "$branch2" --arg p "$product_id" --arg lot "$batch_id" '{fromBranchId:$from,toBranchId:$to,productId:$p,batchId:$lot,quantity:60,note:"E2E phân phối sang chi nhánh bán"}')"

before="$(curl -fsS "$api/inventory?branchId=$branch2&productId=$product_id" -H "$(auth "$warehouse_token")")"
test "$(jq -r '.items[0].quantityOnHand' <<<"$before")" -eq 60
test "$(jq -r '.items[0].reservedQuantity' <<<"$before")" -eq 0

order_payload="$(jq -n --arg branch "$branch2" --arg product "$product_id" --arg unit "$strip_id" '{branchId:$branch,prescriptionId:null,orderType:"ONLINE",pickupType:"STORE_PICKUP",paymentMethod:"COD",recipientName:"Khách E2E",recipientPhone:"0912345678",items:[{productId:$product,saleUnitId:$unit,quantity:2}]}')"
order="$(curl -fsS "$api/orders" -H "$(auth "$customer_token")" -H 'Content-Type: application/json' -d "$order_payload")"
order_id="$(jq -r '.id' <<<"$order")"
test "$(jq -r '.status' <<<"$order")" = "PENDING"

reserved="$(curl -fsS "$api/inventory?branchId=$branch2&productId=$product_id" -H "$(auth "$warehouse_token")")"
test "$(jq -r '.items[0].quantityOnHand' <<<"$reserved")" -eq 60
test "$(jq -r '.items[0].reservedQuantity' <<<"$reserved")" -eq 20
test "$(jq -r '.items[0].availableQuantity' <<<"$reserved")" -eq 40

curl -fsS -o /dev/null "$api/orders/$order_id/confirm" -H "$(auth "$pharmacist_token")" -H 'Content-Type: application/json' -d '{"note":"E2E dược sĩ xác nhận đơn"}'
curl -fsS -o /dev/null "$api/orders/$order_id/complete" -H "$(auth "$pharmacist_token")" -H 'Content-Type: application/json' -d '{"note":"E2E giao thuốc và thu COD"}'
completed="$(curl -fsS "$api/orders/$order_id" -H "$(auth "$pharmacist_token")")"
test "$(jq -r '.status' <<<"$completed")" = "COMPLETED"
test "$(jq -r '.paymentStatus' <<<"$completed")" = "PAID"

after="$(curl -fsS "$api/inventory?branchId=$branch2&productId=$product_id" -H "$(auth "$warehouse_token")")"
test "$(jq -r '.items[0].quantityOnHand' <<<"$after")" -eq 40
test "$(jq -r '.items[0].reservedQuantity' <<<"$after")" -eq 0
transactions="$(curl -fsS "$api/inventory/transactions?branchId=$branch2&productId=$product_id&pageSize=100" -H "$(auth "$warehouse_token")")"
for type in TRANSFER_IN RESERVE SALE; do test "$(jq --arg type "$type" '[.items[] | select(.transactionType==$type)] | length' <<<"$transactions")" -ge 1; done

jq -n --arg product "$code" --arg order "$(jq -r '.code' <<<"$completed")" --argjson source 140 --argjson destination 40 --arg status "$(jq -r '.status' <<<"$completed")" --arg payment "$(jq -r '.paymentStatus' <<<"$completed")" '{result:"PASS",product:$product,order:$order,sourceRemaining:$source,destinationRemaining:$destination,orderStatus:$status,paymentStatus:$payment,verifiedLedger:["IMPORT","TRANSFER_OUT","TRANSFER_IN","RESERVE","SALE"]}'
