# 📱 Guía de Cambios - Frontend: Citas e Ingresos

## 🎯 Resumen de Cambios

Se han corregido **3 bugs críticos** en el backend relacionados con la gestión de citas e ingresos:

1. ✅ **Ingresos ahora se crean SOLO al completar** (no al confirmar)
2. ✅ **Ingresos se crean correctamente** al completar citas confirmadas
3. ✅ **Nuevo endpoint WhatsApp para rechazo** de citas

---

## 🔧 Cambios Técnicos

### **1. Lógica de Ingresos Corregida**

**ANTES (Incorrecto):**
- Los ingresos se creaban tanto al **confirmar** como al **completar** una cita
- Si una cita ya estaba en `Confirmed`, al cambiarla a `Completed` NO se creaban ingresos

**AHORA (Correcto):**
- Los ingresos se crean **SOLO cuando se completa** una cita (`Status = Completed`)
- Los ingresos se crean correctamente al completar desde cualquier estado anterior (Pending, Confirmed, etc.)
- Si no hay servicios, NO se crean ingresos

---

### **2. Nuevo Endpoint: WhatsApp para Rechazo**

**Endpoint agregado:**
```
GET /api/salon/appointments/{id}/whatsapp-url-reject
Authorization: Bearer {token}
```

**Respuesta:**
```json
{
  "url": "https://wa.me/50512345678?text=...",
  "phoneNumber": "50512345678",
  "message": "Hola {nombre}! 👋\n\nLamentamos informarte que no podemos atenderte el {fecha} a las {hora}. ¿Te gustaría reagendar para otro horario? Estaremos encantados de atenderte. 💅"
}
```

---

## 📋 Flujos Actualizados

### **Flujo 1: Cliente agenda CON servicio → Barbero confirma → Barbero completa**

```
1. Cliente agenda → Status: Pending, ServiceIds: [1, 2]
2. Barbero confirma → Status: Confirmed
   ❌ NO se crean ingresos (correcto, aún no se realizó)
3. Barbero completa → Status: Completed
   ✅ SÍ se crean ingresos por servicios 1 y 2
```

**Implementación Frontend:**
```javascript
// Al confirmar
await updateAppointment(appointmentId, { status: "Confirmed" });
// NO esperar ingresos aquí

// Al completar
await updateAppointment(appointmentId, { 
  status: "Completed",
  serviceIds: [1, 2] // Servicios realizados
});
// ✅ Los ingresos se crean automáticamente aquí
```

---

### **Flujo 2: Cliente agenda SIN servicio → Barbero confirma → Barbero completa CON servicios**

```
1. Cliente agenda → Status: Pending, ServiceIds: null
2. Barbero confirma → Status: Confirmed
   ❌ NO se crean ingresos (correcto, aún no se realizó)
3. Barbero completa con serviceIds: [1, 2] → Status: Completed
   ✅ SÍ se crean ingresos por servicios 1 y 2
```

**Implementación Frontend:**
```javascript
// Al confirmar
await updateAppointment(appointmentId, { status: "Confirmed" });

// Al completar (agregando servicios)
await updateAppointment(appointmentId, { 
  status: "Completed",
  serviceIds: [1, 2] // Servicios que se realizaron
});
// ✅ Los ingresos se crean automáticamente aquí
```

---

### **Flujo 3: Cliente agenda → Barbero rechaza**

```
1. Cliente agenda → Status: Pending
2. Barbero rechaza → Status: Cancelled
   ❌ NO se crean ingresos (correcto, no se realizó)
   ✅ Obtener URL WhatsApp de rechazo para notificar al cliente
```

**Implementación Frontend:**
```javascript
// Al rechazar
await updateAppointment(appointmentId, { status: "Cancelled" });

// Obtener URL WhatsApp de rechazo
const response = await fetch(
  `/api/salon/appointments/${appointmentId}/whatsapp-url-reject`,
  {
    headers: { Authorization: `Bearer ${token}` }
  }
);
const { url, message } = await response.json();

// Abrir WhatsApp con mensaje de disculpa
window.open(url, '_blank');
```

---

## 🔌 Endpoints Actualizados

### **1. Actualizar Cita (Sin cambios en la API, pero comportamiento mejorado)**

```
PUT /api/salon/appointments/{id}
Authorization: Bearer {token}
Content-Type: application/json

Body:
{
  "status": "Completed",  // o "Confirmed", "Cancelled"
  "serviceIds": [1, 2, 3]  // Opcional: servicios realizados
}
```

**Comportamiento:**
- Si `status = "Completed"` y hay `serviceIds`, se crean ingresos automáticamente
- Si `status = "Confirmed"`, NO se crean ingresos (corregido)
- Si `status = "Cancelled"`, NO se crean ingresos

---

### **2. Obtener URL WhatsApp Confirmación (Sin cambios)**

```
GET /api/salon/appointments/{id}/whatsapp-url
Authorization: Bearer {token}
```

**Respuesta:**
```json
{
  "url": "https://wa.me/50512345678?text=...",
  "phoneNumber": "50512345678",
  "message": "Hola {nombre}! 👋\n\nTu cita del {fecha} a las {hora} ha sido confirmada. ¡Te esperamos! ✂️"
}
```

---

### **3. Obtener URL WhatsApp Rechazo (NUEVO)**

```
GET /api/salon/appointments/{id}/whatsapp-url-reject
Authorization: Bearer {token}
```

**Respuesta:**
```json
{
  "url": "https://wa.me/50512345678?text=...",
  "phoneNumber": "50512345678",
  "message": "Hola {nombre}! 👋\n\nLamentamos informarte que no podemos atenderte el {fecha} a las {hora}. ¿Te gustaría reagendar para otro horario? Estaremos encantados de atenderte. 💅"
}
```

---

## 💻 Ejemplos de Implementación

### **Ejemplo 1: Completar Cita con Servicios**

```javascript
async function completeAppointment(appointmentId, serviceIds) {
  try {
    // Actualizar cita a completada con servicios
    const response = await fetch(`/api/salon/appointments/${appointmentId}`, {
      method: 'PUT',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        status: 'Completed',
        serviceIds: serviceIds // [1, 2, 3]
      })
    });

    if (!response.ok) {
      throw new Error('Error al completar cita');
    }

    const appointment = await response.json();
    
    // ✅ Los ingresos se crean automáticamente en el backend
    // No necesitas crear ingresos manualmente
    
    return appointment;
  } catch (error) {
    console.error('Error:', error);
    throw error;
  }
}
```

---

### **Ejemplo 2: Confirmar Cita (Sin crear ingresos)**

```javascript
async function confirmAppointment(appointmentId) {
  try {
    // Actualizar cita a confirmada
    const response = await fetch(`/api/salon/appointments/${appointmentId}`, {
      method: 'PUT',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        status: 'Confirmed'
      })
    });

    if (!response.ok) {
      throw new Error('Error al confirmar cita');
    }

    const appointment = await response.json();
    
    // Obtener URL WhatsApp para notificar
    const whatsappResponse = await fetch(
      `/api/salon/appointments/${appointmentId}/whatsapp-url`,
      {
        headers: { 'Authorization': `Bearer ${token}` }
      }
    );
    
    const { url } = await whatsappResponse.json();
    
    // Abrir WhatsApp
    window.open(url, '_blank');
    
    return appointment;
  } catch (error) {
    console.error('Error:', error);
    throw error;
  }
}
```

---

### **Ejemplo 3: Rechazar Cita con WhatsApp**

```javascript
async function rejectAppointment(appointmentId) {
  try {
    // Actualizar cita a cancelada
    const response = await fetch(`/api/salon/appointments/${appointmentId}`, {
      method: 'PUT',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        status: 'Cancelled'
      })
    });

    if (!response.ok) {
      throw new Error('Error al rechazar cita');
    }

    // Obtener URL WhatsApp de rechazo
    const whatsappResponse = await fetch(
      `/api/salon/appointments/${appointmentId}/whatsapp-url-reject`,
      {
        headers: { 'Authorization': `Bearer ${token}` }
      }
    );
    
    const { url, message } = await whatsappResponse.json();
    
    // Mostrar mensaje y abrir WhatsApp
    console.log('Mensaje de rechazo:', message);
    window.open(url, '_blank');
    
    return { success: true, whatsappUrl: url };
  } catch (error) {
    console.error('Error:', error);
    throw error;
  }
}
```

---

### **Ejemplo 4: React Native**

```javascript
import axios from 'axios';

const API_BASE = 'https://api.glownic.com/api/salon';

// Completar cita con servicios
const completeAppointment = async (appointmentId, serviceIds, token) => {
  const response = await axios.put(
    `${API_BASE}/appointments/${appointmentId}`,
    {
      status: 'Completed',
      serviceIds: serviceIds
    },
    {
      headers: { Authorization: `Bearer ${token}` }
    }
  );
  
  // ✅ Los ingresos se crean automáticamente
  return response.data;
};

// Rechazar cita
const rejectAppointment = async (appointmentId, token) => {
  // Cambiar estado a cancelado
  await axios.put(
    `${API_BASE}/appointments/${appointmentId}`,
    { status: 'Cancelled' },
    { headers: { Authorization: `Bearer ${token}` } }
  );
  
  // Obtener URL WhatsApp de rechazo
  const whatsappResponse = await axios.get(
    `${API_BASE}/appointments/${appointmentId}/whatsapp-url-reject`,
    { headers: { Authorization: `Bearer ${token}` } }
  );
  
  // Abrir WhatsApp
  const { url } = whatsappResponse.data;
  Linking.openURL(url);
  
  return whatsappResponse.data;
};
```

---

## ⚠️ Cambios Importantes para el Frontend

### **1. NO crear ingresos manualmente al completar citas**

**ANTES (Incorrecto):**
```javascript
// ❌ NO hacer esto
await updateAppointment(id, { status: 'Completed', serviceIds: [1, 2] });
await createIncome({ amount: 100, description: 'Cita completada' }); // ❌ Duplicado
```

**AHORA (Correcto):**
```javascript
// ✅ Solo actualizar la cita
await updateAppointment(id, { status: 'Completed', serviceIds: [1, 2] });
// Los ingresos se crean automáticamente en el backend
```

---

### **2. Usar nuevo endpoint WhatsApp para rechazo**

**ANTES:**
```javascript
// ❌ No existía endpoint para rechazo
// Tenías que construir el mensaje manualmente
```

**AHORA:**
```javascript
// ✅ Usar el nuevo endpoint
const response = await fetch(
  `/api/salon/appointments/${id}/whatsapp-url-reject`,
  { headers: { Authorization: `Bearer ${token}` } }
);
const { url } = await response.json();
window.open(url, '_blank');
```

---

### **3. Verificar ingresos después de completar**

Si necesitas verificar que los ingresos se crearon correctamente:

```javascript
// Completar cita
await updateAppointment(id, { 
  status: 'Completed', 
  serviceIds: [1, 2] 
});

// Esperar un momento para que se procesen los ingresos
await new Promise(resolve => setTimeout(resolve, 500));

// Verificar ingresos
const incomes = await getIncomes();
const appointmentIncomes = incomes.filter(i => i.appointmentId === id);
console.log('Ingresos creados:', appointmentIncomes);
```

---

## ✅ Checklist de Implementación

- [ ] **Eliminar creación manual de ingresos** al completar citas
- [ ] **Verificar que los ingresos se crean automáticamente** al completar con servicios
- [ ] **Implementar botón "Rechazar"** con llamada al nuevo endpoint WhatsApp
- [ ] **Actualizar flujo de confirmación** para NO esperar ingresos
- [ ] **Probar todos los escenarios:**
  - [ ] Cliente agenda CON servicio → Confirmar → Completar
  - [ ] Cliente agenda SIN servicio → Confirmar → Completar con servicios
  - [ ] Cliente agenda → Rechazar → WhatsApp de disculpa
  - [ ] Verificar que los ingresos aparecen en finanzas

---

## 🐛 Problemas Resueltos

### **Problema 1: Ingresos no se creaban al completar cita confirmada**
✅ **Resuelto:** Ahora se crean correctamente al cambiar a `Completed` desde cualquier estado

### **Problema 2: Ingresos se creaban al confirmar (incorrecto)**
✅ **Resuelto:** Ahora solo se crean al completar

### **Problema 3: No había forma de enviar WhatsApp de rechazo**
✅ **Resuelto:** Nuevo endpoint `/whatsapp-url-reject` disponible

---

## 📞 Soporte

Si encuentras algún problema o tienes dudas sobre la implementación, verifica:

1. ✅ Que estés usando el endpoint correcto (`PUT /appointments/{id}`)
2. ✅ Que envíes `status: "Completed"` (no "Confirmed") para crear ingresos
3. ✅ Que incluyas `serviceIds` si hay servicios realizados
4. ✅ Que uses el nuevo endpoint `/whatsapp-url-reject` para rechazos

---

**Última actualización:** Enero 2025
**Versión API:** 1.0
