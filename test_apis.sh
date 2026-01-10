#!/bin/bash

# Script para probar todas las APIs de GlowNic
# Usuario: luzmarobando@gmail.com

EMAIL="luzmarobando@gmail.com"
PASSWORD="wGuBpyQ&z0#f"
BASE_URL="http://localhost:5229"

echo "🔐 Obteniendo token JWT..."
TOKEN=$(curl -s "$BASE_URL/api/auth/login" -X POST \
  -H "Content-Type: application/json" \
  -d "{\"email\":\"$EMAIL\",\"password\":\"$PASSWORD\"}" | jq -r '.token')

if [ "$TOKEN" == "null" ] || [ -z "$TOKEN" ]; then
  echo "❌ Error: No se pudo obtener el token"
  exit 1
fi

echo "✅ Token obtenido: ${TOKEN:0:50}..."
echo ""
echo "🧪 PROBANDO TODAS LAS APIS DEL SALÓN..."
echo "=========================================="
echo ""

# Función para probar una API
test_api() {
  local method=$1
  local endpoint=$2
  local data=$3
  local description=$4
  
  echo "📌 $description"
  echo "   $method $endpoint"
  
  if [ -n "$data" ]; then
    response=$(curl -s -w "\nHTTP_STATUS:%{http_code}" "$BASE_URL$endpoint" \
      -X "$method" \
      -H "Authorization: Bearer $TOKEN" \
      -H "Content-Type: application/json" \
      -d "$data")
  else
    response=$(curl -s -w "\nHTTP_STATUS:%{http_code}" "$BASE_URL$endpoint" \
      -X "$method" \
      -H "Authorization: Bearer $TOKEN")
  fi
  
  http_code=$(echo "$response" | grep "HTTP_STATUS" | cut -d: -f2)
  body=$(echo "$response" | sed '/HTTP_STATUS/d')
  
  if [ "$http_code" -ge 200 ] && [ "$http_code" -lt 300 ]; then
    echo "   ✅ Status: $http_code"
    if echo "$body" | jq -e . >/dev/null 2>&1; then
      # Es JSON válido
      if echo "$body" | jq -e 'type == "array"' >/dev/null 2>&1; then
        count=$(echo "$body" | jq 'length')
        echo "   📊 Respuesta: Array con $count elementos"
      else
        echo "   📊 Respuesta: $(echo "$body" | jq -c . | head -c 100)..."
      fi
    else
      echo "   📊 Respuesta: $body" | head -c 100
    fi
  else
    echo "   ❌ Status: $http_code"
    echo "   📊 Error: $(echo "$body" | jq -r '.message // .' 2>/dev/null || echo "$body")"
  fi
  echo ""
}

# 1. AUTH APIs
echo "🔐 AUTENTICACIÓN"
echo "----------------"
test_api "POST" "/api/auth/login" "{\"email\":\"$EMAIL\",\"password\":\"$PASSWORD\"}" "Login"
echo ""

# 2. DASHBOARD Y PERFIL
echo "📊 DASHBOARD Y PERFIL"
echo "---------------------"
test_api "GET" "/api/salon/dashboard" "" "Dashboard del salón"
test_api "GET" "/api/salon/profile" "" "Perfil del salón"
test_api "GET" "/api/salon/qr-url" "" "URL del QR"
echo ""

# 3. CITAS (APPOINTMENTS)
echo "📅 CITAS"
echo "--------"
test_api "GET" "/api/salon/appointments" "" "Obtener todas las citas"
test_api "GET" "/api/salon/appointments?date=2024-12-20" "" "Obtener citas por fecha"
test_api "GET" "/api/salon/appointments/history" "" "Historial de citas"
echo ""

# 4. SERVICIOS
echo "💅 SERVICIOS"
echo "------------"
test_api "GET" "/api/salon/services" "" "Obtener todos los servicios"
echo ""

# 5. FINANZAS
echo "💰 FINANZAS"
echo "-----------"
test_api "GET" "/api/salon/finances/summary" "" "Resumen financiero"
test_api "GET" "/api/salon/finances/income" "" "Ingresos"
test_api "GET" "/api/salon/finances/expenses" "" "Egresos"
test_api "GET" "/api/salon/finances/categories" "" "Categorías de gastos"
echo ""

# 6. HORARIOS DE TRABAJO
echo "🕐 HORARIOS DE TRABAJO"
echo "----------------------"
test_api "GET" "/api/salon/working-hours" "" "Obtener horarios de trabajo"
echo ""

# 7. EMPLEADOS
echo "👥 EMPLEADOS"
echo "------------"
test_api "GET" "/api/salon/employees" "" "Obtener todos los empleados"
echo ""

# 8. REPORTES
echo "📈 REPORTES"
echo "-----------"
test_api "GET" "/api/salon/reports/employees/appointments" "" "Reporte de citas por empleado"
test_api "GET" "/api/salon/reports/employees/income" "" "Reporte de ingresos por empleado"
test_api "GET" "/api/salon/reports/employees/expenses" "" "Reporte de egresos por empleado"
test_api "GET" "/api/salon/reports/employees/activity" "" "Reporte de actividad por empleado"
echo ""

# 9. EXPORTACIÓN
echo "📤 EXPORTACIÓN"
echo "--------------"
test_api "GET" "/api/salon/export/appointments" "" "Exportar citas"
test_api "GET" "/api/salon/export/finances" "" "Exportar finanzas"
test_api "GET" "/api/salon/export/clients" "" "Exportar clientes"
echo ""

# 10. AYUDA Y SOPORTE
echo "❓ AYUDA Y SOPORTE"
echo "------------------"
test_api "GET" "/api/salon/help-support" "" "Ayuda y soporte"
echo ""

echo "=========================================="
echo "✅ PRUEBAS COMPLETADAS"
echo ""
