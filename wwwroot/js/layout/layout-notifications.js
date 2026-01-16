// Integración de notificaciones en el layout
// Este archivo se carga en todas las páginas para mostrar notificaciones del sistema

document.addEventListener('DOMContentLoaded', function() {
    // Verificar si hay mensajes de TempData para mostrar
    if (window.tempDataMessages && window.tempDataMessages.length > 0) {
        window.tempDataMessages.forEach(msg => {
            if (window.NotificationSystem) {
                window.NotificationSystem.show(msg.message, msg.type || 'info');
        }
        });
    }
    
    // Escuchar eventos de notificaciones desde otros scripts
    window.addEventListener('showNotification', function(event) {
        if (window.NotificationSystem && event.detail) {
            window.NotificationSystem.show(
                event.detail.message,
                event.detail.type || 'info',
                event.detail.duration
            );
        }
    });
});

// Función helper global para mostrar notificaciones desde cualquier parte
window.showNotification = function(message, type = 'info', duration = 5000) {
    if (window.NotificationSystem) {
        window.NotificationSystem.show(message, type, duration);
    } else {
        console.warn('NotificationSystem no está disponible');
        alert(message);
    }
};
