# 💅 Escenarios Completos: Flujos de Citas GlowNic

## 📋 Índice de Escenarios

1. [Cliente agenda CON servicios → Salón acepta → Salón completa](#escenario-1)
2. [Cliente agenda CON servicios → Salón acepta → Salón completa CON servicios diferentes](#escenario-2)
3. [Cliente agenda SIN servicios → Salón acepta → Salón completa CON servicios](#escenario-3)
4. [Cliente agenda SIN servicios → Salón acepta → Salón completa SIN servicios](#escenario-4)
5. [Cliente agenda CON servicios → Salón rechaza](#escenario-5)
6. [Cliente agenda SIN servicios → Salón rechaza](#escenario-6)
7. [Cliente agenda CON servicios → Salón completa directamente (sin aceptar)](#escenario-7)
8. [Cliente agenda SIN servicios → Salón completa directamente CON servicios](#escenario-8)

---

## 🎯 ESCENARIO 1: Cliente agenda CON servicios → Salón acepta → Salón completa

### **Paso 1: Cliente agenda cita**
```
👩 Cliente: María
📅 Fecha: 15/01/2025
⏰ Hora: 14:00
💅 Servicios seleccionados: 
   - Corte de pelo ($50)
   - Tinte ($80)
   - Manicure ($30)
```

**Request Frontend → Backend:**
```json
POST /api/public/salons/{slug}/appointment
{
  "barberSlug": "salon-bella",
  "serviceIds": [1, 2, 3],
  "clientName": "María González",
  "clientPhone": "50512345678",
  "date": "2025-01-15",
  "time": "14:00"
}
```

**Estado en Base de Datos:**
```sql
Appointment:
  - Id: 100
  - Status: Pending (1)
  - ServiceId: 1 (primer servicio, compatibilidad)
  - ClientName: "María González"
  - ClientPhone: "50512345678"
  - Date: 2025-01-15
  - Time: 14:00

AppointmentServices:
  - AppointmentId: 100, ServiceId: 1 (Corte de pelo)
  - AppointmentId: 100, ServiceId: 2 (Tinte)
  - AppointmentId: 100, ServiceId: 3 (Manicure)
```

**Respuesta Backend:**
```json
{
  "success": true,
  "message": "Tu cita ha sido agendada exitosamente"
}
```

---

### **Paso 2: Salón ve la cita en "Pendientes"**
```
📱 App del Salón muestra:
   - Cliente: María González
   - Fecha: 15/01/2025
   - Hora: 14:00
   - Servicios: Corte de pelo, Tinte, Manicure
   - Total: $160
   - Estado: ⏳ Pendiente
   
Botones disponibles:
   ✅ Aceptar
   ❌ Rechazar
```

---

### **Paso 3: Salón toca "Aceptar"**
```
💼 Salón: "Voy a aceptar esta cita"
```

**Request Frontend → Backend:**
```json
PUT /api/salon/appointments/100
Authorization: Bearer {token}
{
  "status": "Confirmed"
}
```

**Procesamiento Backend:**
```csharp
1. Cambia Status: Pending → Confirmed
2. NO crea ingresos (aún no se realizó el servicio)
3. Guarda cambios
```

**Estado en Base de Datos:**
```sql
Appointment:
  - Status: Confirmed (2) ✅
  - UpdatedAt: 2025-01-10 10:30:00

Transactions:
  - (Vacío - no se crean ingresos al confirmar)
```

**Respuesta Backend:**
```json
{
  "id": 100,
  "status": "Confirmed",
  "clientName": "María González",
  "services": [
    { "id": 1, "name": "Corte de pelo", "price": 50 },
    { "id": 2, "name": "Tinte", "price": 80 },
    { "id": 3, "name": "Manicure", "price": 30 }
  ]
}
```

**Frontend obtiene URL WhatsApp:**
```json
GET /api/salon/appointments/100/whatsapp-url
Authorization: Bearer {token}

Respuesta:
{
  "url": "https://wa.me/50512345678?text=Hola%20María%20González!%20👋%0A%0ATu%20cita%20del%2015/01/2025%20a%20las%2014:00%20ha%20sido%20confirmada.%20¡Te%20esperamos!%20✂️",
  "message": "Hola María González! 👋\n\nTu cita del 15/01/2025 a las 14:00 ha sido confirmada. ¡Te esperamos! ✂️"
}
```

**Frontend abre WhatsApp:**
```
📱 Se abre WhatsApp con mensaje prellenado
💬 Cliente recibe: "Hola María González! 👋 Tu cita del 15/01/2025 a las 14:00 ha sido confirmada. ¡Te esperamos! ✂️"
```

---

### **Paso 4: Día de la cita - Salón completa el servicio**
```
💼 Salón: "María vino, le hice todos los servicios, voy a completar la cita"
```

**Request Frontend → Backend:**
```json
PUT /api/salon/appointments/100
Authorization: Bearer {token}
{
  "status": "Completed",
  "serviceIds": [1, 2, 3]  // Servicios que se realizaron
}
```

**Procesamiento Backend:**
```csharp
1. Verifica que Status actual != Completed
2. Actualiza Status: Confirmed → Completed
3. Guarda servicios en AppointmentServices
4. Consulta servicios de la cita: [1, 2, 3]
5. Crea ingresos automáticamente:
   - Ingreso 1: $50 - "Cita - Corte de pelo - María González"
   - Ingreso 2: $80 - "Cita - Tinte - María González"
   - Ingreso 3: $30 - "Cita - Manicure - María González"
6. Total ingresos: $160
```

**Estado en Base de Datos:**
```sql
Appointment:
  - Status: Completed (3) ✅
  - UpdatedAt: 2025-01-15 14:45:00

Transactions:
  - Id: 500, Amount: 50, Description: "Cita - Corte de pelo - María González"
  - Id: 501, Amount: 80, Description: "Cita - Tinte - María González"
  - Id: 502, Amount: 30, Description: "Cita - Manicure - María González"
```

**Respuesta Backend:**
```json
{
  "id": 100,
  "status": "Completed",
  "clientName": "María González",
  "services": [
    { "id": 1, "name": "Corte de pelo", "price": 50 },
    { "id": 2, "name": "Tinte", "price": 80 },
    { "id": 3, "name": "Manicure", "price": 30 }
  ]
}
```

**Frontend muestra:**
```
✅ Cita completada
💰 Ingresos agregados: $160
📊 Aparece en sección "Finanzas" → "Ingresos"
```

---

## 🎯 ESCENARIO 2: Cliente agenda CON servicios → Salón acepta → Salón completa CON servicios diferentes

### **Paso 1-2: Igual que Escenario 1**
```
Cliente agenda con servicios [1, 2, 3]
Salón acepta → Status: Confirmed
```

---

### **Paso 3: Salón completa pero con servicios diferentes**
```
💼 Salón: "María vino, pero solo le hice Corte y Tinte, no el Manicure"
```

**Request Frontend → Backend:**
```json
PUT /api/salon/appointments/100
Authorization: Bearer {token}
{
  "status": "Completed",
  "serviceIds": [1, 2]  // Solo estos servicios se realizaron
}
```

**Procesamiento Backend:**
```csharp
1. Elimina servicios anteriores en AppointmentServices
2. Agrega nuevos servicios: [1, 2]
3. Cambia Status: Confirmed → Completed
4. Crea ingresos SOLO por servicios realizados:
   - Ingreso 1: $50 - "Cita - Corte de pelo - María González"
   - Ingreso 2: $80 - "Cita - Tinte - María González"
5. Total ingresos: $130 (NO $160)
```

**Resultado:**
```
✅ Cita completada
💰 Ingresos: $130 (solo servicios realizados)
❌ Manicure NO se factura (no se realizó)
```

---

## 🎯 ESCENARIO 3: Cliente agenda SIN servicios → Salón acepta → Salón completa CON servicios

### **Paso 1: Cliente agenda cita SIN servicios**
```
👩 Cliente: Ana
📅 Fecha: 20/01/2025
⏰ Hora: 10:00
💅 Servicios: (Ninguno seleccionado)
```

**Request Frontend → Backend:**
```json
POST /api/public/salons/{slug}/appointment
{
  "barberSlug": "salon-bella",
  "serviceIds": null,  // Sin servicios
  "clientName": "Ana Martínez",
  "clientPhone": "50587654321",
  "date": "2025-01-20",
  "time": "10:00"
}
```

**Estado en Base de Datos:**
```sql
Appointment:
  - Id: 101
  - Status: Pending (1)
  - ServiceId: null
  - ClientName: "Ana Martínez"
  - Date: 2025-01-20
  - Time: 10:00

AppointmentServices:
  - (Vacío - no hay servicios)
```

---

### **Paso 2: Salón acepta**
```
💼 Salón: "Voy a aceptar, luego veo qué servicios necesita"
```

**Request:**
```json
PUT /api/salon/appointments/101
{
  "status": "Confirmed"
}
```

**Resultado:**
```
✅ Status: Confirmed
❌ NO se crean ingresos (no hay servicios aún)
```

---

### **Paso 3: Día de la cita - Salón completa CON servicios**
```
💼 Salón: "Ana vino, le hice Corte y Peinado, voy a completar"
```

**Request Frontend → Backend:**
```json
PUT /api/salon/appointments/101
Authorization: Bearer {token}
{
  "status": "Completed",
  "serviceIds": [1, 4]  // Corte de pelo + Peinado (nuevos servicios)
}
```

**Procesamiento Backend:**
```csharp
1. Agrega servicios [1, 4] a AppointmentServices
2. Cambia Status: Confirmed → Completed
3. Crea ingresos automáticamente:
   - Ingreso 1: $50 - "Cita - Corte de pelo - Ana Martínez"
   - Ingreso 2: $40 - "Cita - Peinado - Ana Martínez"
4. Total ingresos: $90
```

**Resultado:**
```
✅ Cita completada
💰 Ingresos: $90 (servicios agregados al completar)
```

---

## 🎯 ESCENARIO 4: Cliente agenda SIN servicios → Salón acepta → Salón completa SIN servicios

### **Paso 1-2: Igual que Escenario 3**
```
Cliente agenda sin servicios
Salón acepta → Status: Confirmed
```

---

### **Paso 3: Salón completa SIN servicios**
```
💼 Salón: "Ana vino pero no le hice ningún servicio, solo consulta"
```

**Request Frontend → Backend:**
```json
PUT /api/salon/appointments/101
Authorization: Bearer {token}
{
  "status": "Completed"
  // Sin serviceIds
}
```

**Procesamiento Backend:**
```csharp
1. Cambia Status: Confirmed → Completed
2. NO hay servicios en AppointmentServices
3. NO hay ServiceId
4. NO se crean ingresos (no hay servicios que facturar)
```

**Resultado:**
```
✅ Cita completada
❌ NO se crean ingresos (no hay servicios)
💡 La cita queda como "completada sin facturación"
```

---

## 🎯 ESCENARIO 5: Cliente agenda CON servicios → Salón rechaza

### **Paso 1: Cliente agenda CON servicios**
```
👩 Cliente: Laura
📅 Fecha: 18/01/2025
⏰ Hora: 16:00
💅 Servicios: Corte de pelo ($50), Tinte ($80)
```

**Estado inicial:**
```
Appointment:
  - Status: Pending
  - ServiceIds: [1, 2]
```

---

### **Paso 2: Salón rechaza**
```
💼 Salón: "No puedo atender a esa hora, voy a rechazar"
```

**Request Frontend → Backend:**
```json
PUT /api/salon/appointments/102
Authorization: Bearer {token}
{
  "status": "Cancelled"
}
```

**Procesamiento Backend:**
```csharp
1. Cambia Status: Pending → Cancelled
2. NO se crean ingresos (cita cancelada)
3. NO se eliminan los servicios (quedan registrados)
```

**Frontend obtiene URL WhatsApp de rechazo:**
```json
GET /api/salon/appointments/102/whatsapp-url-reject
Authorization: Bearer {token}

Respuesta:
{
  "url": "https://wa.me/50512345678?text=...",
  "message": "Hola Laura! 👋\n\nLamentamos informarte que no podemos atenderte el 18/01/2025 a las 16:00. ¿Te gustaría reagendar para otro horario? Estaremos encantados de atenderte. 💅"
}
```

**Frontend abre WhatsApp:**
```
📱 Se abre WhatsApp con mensaje de disculpa
💬 Cliente recibe: "Hola Laura! 👋 Lamentamos informarte que no podemos atenderte el 18/01/2025 a las 16:00. ¿Te gustaría reagendar para otro horario? Estaremos encantados de atenderte. 💅"
```

**Estado Final:**
```sql
Appointment:
  - Status: Cancelled (4) ❌
  - ServiceIds: [1, 2] (quedan registrados)

Transactions:
  - (Vacío - no se crean ingresos)
```

---

## 🎯 ESCENARIO 6: Cliente agenda SIN servicios → Salón rechaza

### **Paso 1: Cliente agenda SIN servicios**
```
👩 Cliente: Carmen
📅 Fecha: 22/01/2025
⏰ Hora: 11:00
💅 Servicios: (Ninguno)
```

---

### **Paso 2: Salón rechaza**
```
💼 Salón: "No puedo ese día, voy a rechazar"
```

**Request:**
```json
PUT /api/salon/appointments/103
{
  "status": "Cancelled"
}
```

**Resultado:**
```
✅ Status: Cancelled
❌ NO se crean ingresos
📱 WhatsApp de disculpa enviado
```

---

## 🎯 ESCENARIO 7: Cliente agenda CON servicios → Salón completa directamente (sin aceptar)

### **Paso 1: Cliente agenda CON servicios**
```
👩 Cliente: Sofía
📅 Fecha: 25/01/2025
⏰ Hora: 15:00
💅 Servicios: Manicure ($30), Pedicure ($35)
```

**Estado inicial:**
```
Appointment:
  - Status: Pending
  - ServiceIds: [3, 5]
```

---

### **Paso 2: Salón completa directamente**
```
💼 Salón: "Sofía ya vino y le hice los servicios, voy a completar directamente"
```

**Request Frontend → Backend:**
```json
PUT /api/salon/appointments/104
Authorization: Bearer {token}
{
  "status": "Completed",
  "serviceIds": [3, 5]  // Servicios que se realizaron
}
```

**Procesamiento Backend:**
```csharp
1. Cambia Status: Pending → Completed (salta Confirmed)
2. Guarda servicios en AppointmentServices
3. Crea ingresos automáticamente:
   - Ingreso 1: $30 - "Cita - Manicure - Sofía"
   - Ingreso 2: $35 - "Cita - Pedicure - Sofía"
4. Total ingresos: $65
```

**Resultado:**
```
✅ Cita completada directamente
💰 Ingresos: $65
⏭️ Se saltó el paso de "Confirmar"
```

---

## 🎯 ESCENARIO 8: Cliente agenda SIN servicios → Salón completa directamente CON servicios

### **Paso 1: Cliente agenda SIN servicios**
```
👩 Cliente: Patricia
📅 Fecha: 28/01/2025
⏰ Hora: 13:00
💅 Servicios: (Ninguno)
```

---

### **Paso 2: Salón completa directamente CON servicios**
```
💼 Salón: "Patricia vino, le hice Corte, voy a completar"
```

**Request Frontend → Backend:**
```json
PUT /api/salon/appointments/105
Authorization: Bearer {token}
{
  "status": "Completed",
  "serviceIds": [1]  // Corte de pelo
}
```

**Procesamiento Backend:**
```csharp
1. Cambia Status: Pending → Completed
2. Agrega servicio [1] a AppointmentServices
3. Crea ingresos automáticamente:
   - Ingreso 1: $50 - "Cita - Corte de pelo - Patricia"
4. Total ingresos: $50
```

**Resultado:**
```
✅ Cita completada directamente
💰 Ingresos: $50
⏭️ Se agregaron servicios al completar
```

---

## 📊 Tabla Resumen de Todos los Escenarios

| Escenario | Cliente Agenda | Salón Acepta | Salón Completa | Servicios al Completar | Ingresos Creados |
|-----------|----------------|--------------|-----------------|------------------------|------------------|
| 1 | ✅ CON servicios | ✅ Sí | ✅ Sí | Mismos servicios | ✅ $160 (3 servicios) |
| 2 | ✅ CON servicios | ✅ Sí | ✅ Sí | Servicios diferentes | ✅ $130 (2 servicios) |
| 3 | ❌ SIN servicios | ✅ Sí | ✅ Sí | Agrega servicios | ✅ $90 (2 servicios) |
| 4 | ❌ SIN servicios | ✅ Sí | ✅ Sí | Sin servicios | ❌ $0 (sin servicios) |
| 5 | ✅ CON servicios | ❌ Rechaza | ❌ No | - | ❌ $0 (cancelada) |
| 6 | ❌ SIN servicios | ❌ Rechaza | ❌ No | - | ❌ $0 (cancelada) |
| 7 | ✅ CON servicios | ⏭️ Salta | ✅ Sí | Mismos servicios | ✅ $65 (2 servicios) |
| 8 | ❌ SIN servicios | ⏭️ Salta | ✅ Sí | Agrega servicios | ✅ $50 (1 servicio) |

---

## 🔄 Flujos Visuales

### **Flujo Normal (Con Aceptar)**
```
Cliente Agenda
    ↓
[Pendiente] ← Estado inicial
    ↓
Salón Acepta
    ↓
[Confirmada] ← NO se crean ingresos
    ↓
Salón Completa
    ↓
[Completada] ← ✅ SÍ se crean ingresos
```

### **Flujo Directo (Sin Aceptar)**
```
Cliente Agenda
    ↓
[Pendiente] ← Estado inicial
    ↓
Salón Completa directamente
    ↓
[Completada] ← ✅ SÍ se crean ingresos
```

### **Flujo Rechazo**
```
Cliente Agenda
    ↓
[Pendiente] ← Estado inicial
    ↓
Salón Rechaza
    ↓
[Cancelada] ← ❌ NO se crean ingresos
    ↓
WhatsApp de disculpa
```

---

## 💡 Reglas Importantes

### **✅ Cuándo SÍ se crean ingresos:**
1. Cuando `Status` cambia a `Completed`
2. Y hay servicios asociados (`serviceIds` o `ServiceId`)
3. Y NO existen ingresos previos para esa cita

### **❌ Cuándo NO se crean ingresos:**
1. Cuando `Status` cambia a `Confirmed` (solo confirmación)
2. Cuando `Status` cambia a `Cancelled` (rechazo)
3. Cuando se completa pero NO hay servicios
4. Cuando ya existen ingresos previos (evita duplicados)

### **📱 WhatsApp:**
- **Confirmación:** Usar `/whatsapp-url` → Mensaje de confirmación
- **Rechazo:** Usar `/whatsapp-url-reject` → Mensaje de disculpa

---

## 🎯 Casos Especiales

### **Caso A: Cliente agenda con servicios, salón completa con MENOS servicios**
```
Cliente agenda: [1, 2, 3] ($160)
Salón completa: [1, 2] ($130)
Resultado: ✅ Ingresos por $130 (solo servicios realizados)
```

### **Caso B: Cliente agenda con servicios, salón completa con MÁS servicios**
```
Cliente agenda: [1, 2] ($130)
Salón completa: [1, 2, 3, 4] ($200)
Resultado: ✅ Ingresos por $200 (todos los servicios realizados)
```

### **Caso C: Cliente agenda con servicios, salón completa con servicios DIFERENTES**
```
Cliente agenda: [1, 2] (Corte, Tinte)
Salón completa: [3, 4] (Manicure, Pedicure)
Resultado: ✅ Ingresos por servicios [3, 4] (solo los realizados)
```

---

## 📝 Notas para el Frontend

1. **NO crear ingresos manualmente** - El backend lo hace automáticamente
2. **Siempre enviar `serviceIds`** al completar si hay servicios realizados
3. **Usar endpoint correcto de WhatsApp** según la acción (confirmar/rechazar)
4. **Verificar ingresos** después de completar para mostrar confirmación al usuario
5. **Manejar casos sin servicios** - No es error, simplemente no se crean ingresos

---

**Última actualización:** Enero 2025
**Versión:** 1.0
