/**
 * Layout Keep-Alive Session
 * Renovación automática de sesión para evitar cierres inesperados
 * 
 * ESTRATEGIA PARA TALLER/PUNTO DE VENTA:
 * - Renueva SIEMPRE cada 10 minutos (ignora inactividad)
 * - Permite sesiones de 12 horas sin interrupciones
 * - Usuario solo hace login una vez al día
 * - Funciona incluso si no hay clientes por horas
 */

(function() {
    'use strict';
    
    let keepAliveInterval;
    let keepAliveFailCount = 0;
    let lastActivity = Date.now();
    const KEEP_ALIVE_INTERVAL = 10 * 60 * 1000; // 10 minutos
    const MAX_FAIL_COUNT = 3; // Máximo de fallos consecutivos antes de alertar
    
    // Función para renovar la sesión
    function renewSession() {
        fetch('/auth/keep-alive', {
            method: 'GET',
            credentials: 'include',
            headers: {
                'X-Requested-With': 'XMLHttpRequest'
            }
        })
        .then(response => {
            if (response.ok) {
                keepAliveFailCount = 0; // Reset contador de fallos
                return response.json();
            }
            if (response.status === 401 || response.status === 403) {
                console.warn('Sesión expirada, redirigiendo al login...');
                // Mostrar notificación antes de redirigir
                if (typeof Notify !== 'undefined') {
                    Notify.warning('Tu sesión ha expirado. Redirigiendo al login...');
                }
                setTimeout(function() {
                    window.location.href = '/login';
                }, 2000);
            }
            return null;
        })
        .then(data => {
            if (data && data.success) {
                const timeSinceLastActivity = Date.now() - lastActivity;
                const minutesInactive = Math.floor(timeSinceLastActivity / 60000);
                
                if (minutesInactive > 30) {
                } else {
                }
            }
        })
        .catch(error => {
            keepAliveFailCount++;
            console.error('Error al renovar sesión (intento ' + keepAliveFailCount + '/' + MAX_FAIL_COUNT + '):', error);
            
            // Si fallan 3 intentos consecutivos, alertar al usuario
            if (keepAliveFailCount >= MAX_FAIL_COUNT) {
                console.error('Múltiples fallos al renovar sesión. Puede haber problemas de conexión.');
                if (typeof Notify !== 'undefined') {
                    Notify.error('Problema de conexión. Verifica tu internet para mantener la sesión activa.');
                }
            }
        });
    }
    
    // Detectar actividad del usuario
    const activityEvents = ['mousedown', 'mousemove', 'keypress', 'scroll', 'touchstart', 'click'];
    activityEvents.forEach(event => {
        document.addEventListener(event, function() {
            lastActivity = Date.now();
        }, { passive: true });
    });
    
    // Iniciar keep-alive cuando la página carga
    function startKeepAlive() {
        // Renovación inicial
        renewSession();
        
        // RENOVACIÓN CONTINUA: Renueva SIEMPRE cada 10 minutos, independientemente de la actividad
        // Esto asegura que la sesión nunca expire durante la jornada laboral
        keepAliveInterval = setInterval(function() {
            renewSession();
        }, KEEP_ALIVE_INTERVAL);
        
    }
    
    // Iniciar cuando el DOM esté listo
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', startKeepAlive);
    } else {
        startKeepAlive();
    }
    
    // Limpiar intervalo cuando la página se descarga
    window.addEventListener('beforeunload', function() {
        if (keepAliveInterval) {
            clearInterval(keepAliveInterval);
        }
    });
    
    // Detectar cuando la pestaña vuelve a estar visible (después de estar en segundo plano)
    document.addEventListener('visibilitychange', function() {
        if (!document.hidden) {
            // Pestaña visible de nuevo, renovar inmediatamente
            renewSession();
        }
    });
})();

