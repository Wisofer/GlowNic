/**
 * Sistema de Notificaciones Personalizado
 * Minimalista y elegante para reemplazar alerts nativos
 */

class NotificationSystem {
    constructor() {
        this.container = null;
        this.sounds = {
            success: null,
            error: null
        };
        this.init();
        this.initSounds();
    }

    init() {
        // Crear contenedor de notificaciones si no existe
        if (!document.getElementById('notification-container')) {
            this.container = document.createElement('div');
            this.container.id = 'notification-container';
            this.container.className = 'notification-container';
            document.body.appendChild(this.container);
        } else {
            this.container = document.getElementById('notification-container');
        }
    }

    /**
     * Inicializa los sonidos del sistema
     */
    initSounds() {
        try {
            this.sounds.success = new Audio('/sounds/success.mp3');
            this.sounds.success.volume = 0.5;
            this.sounds.success.preload = 'auto';
            
            this.sounds.error = new Audio('/sounds/error.mp3');
            this.sounds.error.volume = 0.5;
            this.sounds.error.preload = 'auto';
        } catch (e) {
            console.warn('No se pudieron cargar los sonidos:', e);
        }
    }

    /**
     * Reproduce un sonido según el tipo de notificación
     */
    playSound(type) {
        // Verificar si los sonidos están habilitados en localStorage
        const sonidosHabilitados = localStorage.getItem('sonidosHabilitados');
        if (sonidosHabilitados === 'false') {
            return; // No reproducir sonido si están deshabilitados
        }
        
        try {
            if (type === 'success' && this.sounds.success) {
                // Clonar el audio para permitir múltiples reproducciones simultáneas
                const audio = this.sounds.success.cloneNode();
                audio.volume = 0.5;
                audio.play().catch(err => {
                    // Ignorar errores de autoplay (requiere interacción del usuario)
                    console.debug('No se pudo reproducir sonido (requiere interacción del usuario):', err);
                });
            } else if ((type === 'error' || type === 'warning') && this.sounds.error) {
                const audio = this.sounds.error.cloneNode();
                audio.volume = 0.5;
                audio.play().catch(err => {
                    console.debug('No se pudo reproducir sonido (requiere interacción del usuario):', err);
                });
            }
        } catch (e) {
            console.debug('Error al reproducir sonido:', e);
        }
    }

    /**
     * Muestra una notificación
     * @param {string} message - Mensaje a mostrar
     * @param {string} type - Tipo: 'success', 'error', 'warning', 'info'
     * @param {number} duration - Duración en milisegundos (0 = permanente hasta cerrar)
     */
    show(message, type = 'info', duration = 4000) {
        const notification = document.createElement('div');
        notification.className = `notification notification-${type}`;
        
        // Iconos según el tipo
        const icons = {
            success: '✓',
            error: '✕',
            warning: '⚠',
            info: 'ℹ'
        };

        // Colores según el tipo
        const colors = {
            success: '#10b981',
            error: '#ef4444',
            warning: '#f59e0b',
            info: '#3b82f6'
        };

        notification.innerHTML = `
            <div class="notification-content">
                <div class="notification-icon" style="background: ${colors[type]}20; color: ${colors[type]}">
                    ${icons[type]}
                </div>
                <div class="notification-message">${this.formatMessage(message)}</div>
                <button class="notification-close" onclick="this.parentElement.parentElement.remove()">
                    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                        <line x1="18" y1="6" x2="6" y2="18"></line>
                        <line x1="6" y1="6" x2="18" y2="18"></line>
                    </svg>
                </button>
            </div>
            <div class="notification-progress" style="background: ${colors[type]}"></div>
        `;

        this.container.appendChild(notification);

        // Reproducir sonido según el tipo
        this.playSound(type);

        // Animación de entrada
        setTimeout(() => {
            notification.classList.add('notification-show');
        }, 10);

        // Auto-cerrar si tiene duración
        if (duration > 0) {
            const progressBar = notification.querySelector('.notification-progress');
            progressBar.style.animation = `notification-progress ${duration}ms linear forwards`;

            setTimeout(() => {
                this.remove(notification);
            }, duration);
        }
    }

    /**
     * Formatea el mensaje (soporta saltos de línea)
     */
    formatMessage(message) {
        return message
            .split('\n')
            .map(line => line.trim())
            .filter(line => line)
            .map(line => `<div>${line}</div>`)
            .join('');
    }

    /**
     * Remueve una notificación con animación
     */
    remove(notification) {
        notification.classList.remove('notification-show');
        notification.classList.add('notification-hide');
        setTimeout(() => {
            if (notification.parentNode) {
                notification.parentNode.removeChild(notification);
            }
        }, 300);
    }

    // Métodos de conveniencia
    success(message, duration = 4000) {
        this.show(message, 'success', duration);
    }

    error(message, duration = 5000) {
        this.show(message, 'error', duration);
    }

    warning(message, duration = 4000) {
        this.show(message, 'warning', duration);
    }

    info(message, duration = 4000) {
        this.show(message, 'info', duration);
    }
}

// Instancia global
const Notify = new NotificationSystem();

// Función de compatibilidad para reemplazar alert()
window.showNotification = function(message, type = 'info') {
    // Detectar tipo automáticamente por el contenido
    if (typeof message === 'string') {
        if (message.includes('✅') || message.includes('exitos') || message.includes('correctamente')) {
            type = 'success';
        } else if (message.includes('❌') || message.includes('Error') || message.includes('error')) {
            type = 'error';
        } else if (message.includes('⚠️') || message.includes('Advertencia') || message.includes('advertencia')) {
            type = 'warning';
        }
        
        // Limpiar emojis para el mensaje
        message = message.replace(/✅|❌|⚠️|ℹ️/g, '').trim();
    }
    
    Notify.show(message, type);
};

// Reemplazar alert nativo (opcional, para compatibilidad)
const originalAlert = window.alert;
window.alert = function(message) {
    showNotification(message, 'info');
};

/**
 * Sistema de Confirmación Personalizado
 * Reemplaza confirm() nativo con un modal bonito
 */
class ConfirmSystem {
    constructor() {
        this.container = null;
        this.init();
    }

    init() {
        // Crear contenedor de confirmación si no existe
        if (!document.getElementById('confirm-container')) {
            this.container = document.createElement('div');
            this.container.id = 'confirm-container';
            this.container.className = 'confirm-container';
            document.body.appendChild(this.container);
        } else {
            this.container = document.getElementById('confirm-container');
        }
    }

    /**
     * Muestra un diálogo de confirmación
     * @param {string} message - Mensaje a mostrar
     * @param {string} title - Título (opcional)
     * @returns {Promise<boolean>} - true si confirma, false si cancela
     */
    show(message, title = 'Confirmar') {
        return new Promise((resolve) => {
            const overlay = document.createElement('div');
            overlay.className = 'confirm-overlay';
            
            const modal = document.createElement('div');
            modal.className = 'confirm-modal';
            
            // Función para cerrar y resolver
            const cerrarYResolver = (resultado) => {
                overlay.classList.remove('confirm-show');
                setTimeout(() => {
                    overlay.remove();
                    resolve(resultado);
                }, 200);
            };
            
            modal.innerHTML = `
                <div class="confirm-header">
                    <h3 class="confirm-title">${title}</h3>
                    <button class="confirm-close" type="button">
                        <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                            <line x1="18" y1="6" x2="6" y2="18"></line>
                            <line x1="6" y1="6" x2="18" y2="18"></line>
                        </svg>
                    </button>
                </div>
                <div class="confirm-body">
                    <div class="confirm-message">${message}</div>
                </div>
                <div class="confirm-footer">
                    <button class="confirm-btn confirm-btn-cancel" type="button">
                        Cancelar
                    </button>
                    <button class="confirm-btn confirm-btn-confirm" type="button">
                        Confirmar
                    </button>
                </div>
            `;
            
            overlay.appendChild(modal);
            this.container.appendChild(overlay);
            
            // Event listeners para los botones
            const btnCerrar = modal.querySelector('.confirm-close');
            const btnCancelar = modal.querySelector('.confirm-btn-cancel');
            const btnConfirmar = modal.querySelector('.confirm-btn-confirm');
            
            btnCerrar.addEventListener('click', () => cerrarYResolver(false));
            btnCancelar.addEventListener('click', () => cerrarYResolver(false));
            btnConfirmar.addEventListener('click', () => cerrarYResolver(true));
            
            // Cerrar al hacer click en el overlay (fuera del modal)
            overlay.addEventListener('click', (e) => {
                if (e.target === overlay) {
                    cerrarYResolver(false);
                }
            });
            
            // Animación de entrada
            setTimeout(() => {
                overlay.classList.add('confirm-show');
            }, 10);
            
            // Cerrar con ESC
            const escHandler = (e) => {
                if (e.key === 'Escape') {
                    cerrarYResolver(false);
                    document.removeEventListener('keydown', escHandler);
                }
            };
            document.addEventListener('keydown', escHandler);
        });
    }
}

// Instancia global
const Confirm = new ConfirmSystem();

// Función de compatibilidad para reemplazar confirm()
window.showConfirm = async function(message, title = 'Confirmar') {
    return await Confirm.show(message, title);
};

// Reemplazar confirm nativo (opcional, para compatibilidad)
const originalConfirm = window.confirm;
window.confirm = function(message) {
    // Para compatibilidad síncrona, usar confirm nativo pero mostrar notificación
    const result = originalConfirm(message);
    return result;
};

