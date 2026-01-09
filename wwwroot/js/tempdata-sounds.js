/**
 * Sistema de Sonidos para TempData Alerts
 * Reproduce sonidos automáticamente cuando aparecen mensajes de TempData
 */

document.addEventListener('DOMContentLoaded', function() {
    // Crear instancias de audio
    let audioSuccess = null;
    let audioError = null;
    
    // Set para rastrear alerts que ya reprodujeron sonido
    const alertsSonados = new WeakSet();
    
    try {
        audioSuccess = new Audio('/sounds/success.mp3');
        audioSuccess.volume = 0.3; // Reducir volumen de 0.5 a 0.3
        audioSuccess.preload = 'auto';
        
        audioError = new Audio('/sounds/error.mp3');
        audioError.volume = 0.3; // Reducir volumen de 0.5 a 0.3
        audioError.preload = 'auto';
    } catch (e) {
        console.warn('No se pudieron cargar los sonidos de TempData:', e);
    }
    
    // Variable para evitar múltiples reproducciones simultáneas
    let ultimoSonido = 0;
    const INTERVALO_MINIMO = 500; // 500ms entre sonidos
    
    /**
     * Reproduce sonido según el tipo de alert
     */
    function reproducirSonido(tipo) {
        // Verificar si los sonidos están habilitados en localStorage
        const sonidosHabilitados = localStorage.getItem('sonidosHabilitados');
        if (sonidosHabilitados === 'false') {
            return; // No reproducir sonido si están deshabilitados
        }
        
        const ahora = Date.now();
        
        // Evitar reproducir múltiples sonidos muy seguidos
        if (ahora - ultimoSonido < INTERVALO_MINIMO) {
            console.debug('⏸️ Sonido ignorado (demasiado pronto)');
            return;
        }
        
        try {
            if (tipo === 'success' && audioSuccess) {
                const audio = audioSuccess.cloneNode();
                audio.volume = 0.3;
                audio.play().catch(err => {
                    console.debug('No se pudo reproducir sonido (requiere interacción del usuario):', err);
                });
                ultimoSonido = ahora;
            } else if (tipo === 'error' && audioError) {
                const audio = audioError.cloneNode();
                audio.volume = 0.3;
                audio.play().catch(err => {
                    console.debug('No se pudo reproducir sonido (requiere interacción del usuario):', err);
                });
                ultimoSonido = ahora;
            }
        } catch (e) {
            console.debug('Error al reproducir sonido:', e);
        }
    }
    
    /**
     * Verifica y reproduce sonido para un alert si no se ha reproducido antes
     */
    function verificarYReproducirAlert(alertElement, tipo) {
        if (!alertsSonados.has(alertElement)) {
            alertsSonados.add(alertElement);
            reproducirSonido(tipo);
            return true;
        }
        return false;
    }
    
    // Buscar y reproducir sonido SOLO PARA EL PRIMER alert de cada tipo al cargar
    const alertsSuccess = document.querySelectorAll('.alert-success');
    if (alertsSuccess.length > 0) {
        verificarYReproducirAlert(alertsSuccess[0], 'success');
    }
    
    const alertsError = document.querySelectorAll('.alert-error');
    if (alertsError.length > 0) {
        if (alertsSuccess.length === 0) { // Solo si no hay success
            verificarYReproducirAlert(alertsError[0], 'error');
        }
    }
    
    const alertsWarning = document.querySelectorAll('.alert-warning');
    if (alertsWarning.length > 0) {
        if (alertsSuccess.length === 0 && alertsError.length === 0) { // Solo si no hay otros
            verificarYReproducirAlert(alertsWarning[0], 'error');
        }
    }
    
    // Observer para detectar nuevos alerts que se agregan dinámicamente
    const observer = new MutationObserver(function(mutations) {
        mutations.forEach(function(mutation) {
            mutation.addedNodes.forEach(function(node) {
                if (node.nodeType === 1) { // ELEMENT_NODE
                    // Verificar si el nodo agregado es un alert
                    if (node.classList) {
                        if (node.classList.contains('alert-success')) {
                            verificarYReproducirAlert(node, 'success');
                        } else if (node.classList.contains('alert-error')) {
                            verificarYReproducirAlert(node, 'error');
                        } else if (node.classList.contains('alert-warning')) {
                            verificarYReproducirAlert(node, 'error');
                        }
                    }
                    
                    // Buscar alerts dentro del nodo agregado (SOLO EL PRIMERO)
                    if (node.querySelectorAll) {
                        const successAlerts = node.querySelectorAll('.alert-success');
                        if (successAlerts.length > 0) {
                            verificarYReproducirAlert(successAlerts[0], 'success');
                        }
                        
                        const errorAlerts = node.querySelectorAll('.alert-error');
                        if (errorAlerts.length > 0 && successAlerts.length === 0) {
                            verificarYReproducirAlert(errorAlerts[0], 'error');
                        }
                        
                        const warningAlerts = node.querySelectorAll('.alert-warning');
                        if (warningAlerts.length > 0 && successAlerts.length === 0 && errorAlerts.length === 0) {
                            verificarYReproducirAlert(warningAlerts[0], 'error');
                        }
                    }
                }
            });
        });
    });
    
    // Observar cambios en el body
    observer.observe(document.body, {
        childList: true,
        subtree: true
    });
    
});

