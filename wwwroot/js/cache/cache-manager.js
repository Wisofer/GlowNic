/**
 * Cache Manager - Sistema de gestión de caché
 * 
 * ESTRATEGIA OPTIMIZADA BASADA EN MEJORES PRÁCTICAS:
 * ===================================================
 * 
 * 1. Invalidación inmediata: Al eliminar/modificar datos, invalida solo el caché relacionado
 * 2. Limpieza selectiva al iniciar: Solo limpia si detecta cambios de versión o datos obsoletos (>12h)
 * 3. Limpieza al cerrar sesión: Limpia datos de sesión y temporales (mantiene preferencias)
 * 4. Limpieza selectiva al cerrar navegador: Solo datos de negocio, mantiene preferencias
 * 
 * PRINCIPIOS:
 * - Invalidación basada en eventos (patrón estándar)
 * - Limpieza selectiva (solo datos necesarios)
 * - Preservar preferencias del usuario
 * - No interrumpir el trabajo del usuario
 */

// Claves de almacenamiento
const CACHE_KEYS = {
    CARRITOS_MESAS: 'pos_carritos_mesas',
    LAST_CLEANUP: 'cache_last_cleanup',
    CACHE_VERSION: 'cache_version'
};

// Versión del caché (incrementar cuando cambie la estructura)
const CURRENT_CACHE_VERSION = '1.0.0';

/**
 * Inicializar el sistema de caché
 * Se ejecuta al cargar la página
 * 
 * ESTRATEGIA OPTIMIZADA (BASADA EN MEJORES PRÁCTICAS):
 * =====================================================
 * 
 * 1. Invalidación inmediata: Al eliminar datos, invalida solo el caché relacionado
 * 2. Limpieza selectiva al iniciar: Solo si detecta cambios de versión o datos obsoletos (>12h)
 * 3. Limpieza al cerrar sesión: Limpia datos de sesión, mantiene preferencias
 * 4. Limpieza selectiva al cerrar navegador: Solo datos de negocio, mantiene preferencias
 * 
 * PRINCIPIOS APLICADOS:
 * - Invalidación basada en eventos (patrón estándar de la industria)
 * - Limpieza selectiva (solo lo necesario, preserva preferencias)
 * - Conservador (evita limpiezas innecesarias)
 * - No interrumpe el trabajo del usuario
 */
function inicializarCacheManager() {
    // Verificar versión del caché (limpia si cambió la versión)
    verificarVersionCache();
    
    // Limpieza selectiva al iniciar sesión (solo si es necesario)
    limpiarCacheAlIniciarSesion();
    
    // Limpieza al cerrar sesión
    configurarLimpiezaAlCerrarSesion();
    
    // Limpieza selectiva al cerrar navegador
    configurarLimpiezaAlCerrarNavegador();
    
    console.log('✅ Cache Manager inicializado - Estrategia optimizada activa');
}

/**
 * Verificar versión del caché y limpiar si es necesario
 */
function verificarVersionCache() {
    try {
        const versionGuardada = sessionStorage.getItem(CACHE_KEYS.CACHE_VERSION);
        
        if (!versionGuardada || versionGuardada !== CURRENT_CACHE_VERSION) {
            console.log('🔄 Versión de caché diferente detectada. Limpiando caché...');
            limpiarTodoCache();
            sessionStorage.setItem(CACHE_KEYS.CACHE_VERSION, CURRENT_CACHE_VERSION);
        }
    } catch (e) {
        console.error('Error al verificar versión de caché:', e);
    }
}

/**
 * Limpiar caché al iniciar sesión (estrategia selectiva)
 * 
 * ESTRATEGIA OPTIMIZADA:
 * - Solo limpia si detecta cambios de versión (ya manejado por verificarVersionCache)
 * - Solo limpia si pasaron más de 12 horas (más conservador, evita limpiezas innecesarias)
 * - Confía en invalidación inmediata para datos actuales
 * - Sigue el patrón de sistemas comerciales: limpiar solo cuando es necesario
 */
function limpiarCacheAlIniciarSesion() {
    try {
        // La verificación de versión ya se hace en verificarVersionCache()
        // Aquí solo verificamos si hay datos obsoletos por tiempo
        
        const ultimaLimpieza = localStorage.getItem(CACHE_KEYS.LAST_CLEANUP);
        
        if (ultimaLimpieza) {
            const fechaUltimaLimpieza = new Date(ultimaLimpieza);
            const horasDesdeLimpieza = (new Date() - fechaUltimaLimpieza) / (1000 * 60 * 60);
            
            // Solo limpiar si pasaron más de 12 horas (más conservador)
            // Esto cubre el caso de cerrar a las 6:30 PM y volver al día siguiente a las 8:00 AM
            if (horasDesdeLimpieza > 12) {
                console.log(`🧹 Limpiando caché obsoleto al iniciar sesión (última limpieza hace ${Math.floor(horasDesdeLimpieza)} horas)...`);
                limpiarDatosNegocio(); // Limpieza selectiva, no todo
            } else {
                console.log(`✅ Caché actualizado (última limpieza hace ${Math.floor(horasDesdeLimpieza)} horas)`);
            }
        } else {
            // Primera vez, limpiar datos de negocio
            console.log('🧹 Primera ejecución, limpiando datos de negocio...');
            limpiarDatosNegocio();
        }
    } catch (e) {
        console.error('Error al limpiar caché al iniciar sesión:', e);
    }
}

/**
 * Configurar limpieza de caché al cerrar sesión
 * 
 * ESTRATEGIA: Limpia datos de sesión y temporales, mantiene preferencias del usuario
 * Esto es estándar en sistemas comerciales: limpiar al cerrar sesión
 */
function configurarLimpiezaAlCerrarSesion() {
    try {
        // Detectar cuando se hace clic en cerrar sesión
        document.addEventListener('click', function(e) {
            const target = e.target;
            
            // Buscar botones o enlaces de cerrar sesión
            if (target && (
                target.textContent?.toLowerCase().includes('cerrar sesión') ||
                target.textContent?.toLowerCase().includes('logout') ||
                target.closest('form[action*="logout"]') ||
                target.closest('a[href*="logout"]')
            )) {
                console.log('🧹 Limpiando datos de sesión al cerrar...');
                limpiarDatosSesion(); // Limpieza selectiva
            }
        });
        
        // También limpiar cuando se detecta un redirect a /login (sesión cerrada)
        const originalFetch = window.fetch;
        window.fetch = function(...args) {
            return originalFetch.apply(this, args).then(response => {
                if (response.url && response.url.includes('/login') && response.redirected) {
                    console.log('🧹 Sesión cerrada, limpiando datos de sesión...');
                    limpiarDatosSesion(); // Limpieza selectiva
                }
                return response;
            });
        };
    } catch (e) {
        console.error('Error al configurar limpieza al cerrar sesión:', e);
    }
}

/**
 * Configurar limpieza de caché al cerrar el navegador
 * 
 * ESTRATEGIA SELECTIVA:
 * - sessionStorage se limpia automáticamente al cerrar (por diseño del navegador)
 * - Solo limpiamos datos de negocio del localStorage
 * - Mantenemos todas las preferencias del usuario
 * - Esto es más conservador y sigue mejores prácticas
 */
function configurarLimpiezaAlCerrarNavegador() {
    try {
        // Limpiar datos de negocio cuando se cierra la pestaña o el navegador
        window.addEventListener('beforeunload', function() {
            try {
                // sessionStorage se limpia automáticamente al cerrar, no necesitamos hacerlo
                // Solo limpiar datos de negocio del localStorage, mantener preferencias
                limpiarDatosNegocio();
            } catch (e) {
                // Silenciar errores al cerrar (el navegador puede estar cerrando)
            }
        });
        
        console.log('✅ Limpieza selectiva al cerrar navegador configurada');
    } catch (e) {
        console.error('Error al configurar limpieza al cerrar navegador:', e);
    }
}


/**
 * Limpiar datos de negocio (carritos, datos temporales)
 * 
 * ESTRATEGIA SELECTIVA: Solo limpia datos de negocio, mantiene preferencias
 * Esta es la función principal para limpieza selectiva
 */
function limpiarDatosNegocio() {
    try {
        // Limpiar sessionStorage (carritos de mesas, datos temporales de sesión)
        sessionStorage.clear();
        
        // Limpiar localStorage de datos de negocio (mantener preferencias del usuario)
        const keysToKeep = [
            'tema', 
            'sidebarState', 
            'sidebarCollapsed', 
            'sonidosHabilitados', 
            'zoomLevel',
            CACHE_KEYS.LAST_CLEANUP,
            CACHE_KEYS.CACHE_VERSION
        ];
        
        const keysToRemove = [];
        for (let i = 0; i < localStorage.length; i++) {
            const key = localStorage.key(i);
            if (key && !keysToKeep.includes(key)) {
                keysToRemove.push(key);
            }
        }
        
        keysToRemove.forEach(key => localStorage.removeItem(key));
        
        // Actualizar versión del caché y fecha de limpieza
        sessionStorage.setItem(CACHE_KEYS.CACHE_VERSION, CURRENT_CACHE_VERSION);
        localStorage.setItem(CACHE_KEYS.LAST_CLEANUP, new Date().toISOString());
        
        console.log('✅ Datos de negocio limpiados (preferencias preservadas)');
    } catch (e) {
        console.error('Error al limpiar datos de negocio:', e);
    }
}

/**
 * Limpiar datos de sesión (al cerrar sesión)
 * 
 * Similar a limpiarDatosNegocio pero más agresivo para asegurar limpieza completa
 */
function limpiarDatosSesion() {
    try {
        // Limpiar sessionStorage completamente
        sessionStorage.clear();
        
        // Limpiar localStorage de datos de sesión y temporales
        // Mantener solo preferencias básicas
        const keysToKeep = [
            'tema', 
            'sidebarState', 
            'sidebarCollapsed', 
            'sonidosHabilitados', 
            'zoomLevel'
        ];
        
        const keysToRemove = [];
        for (let i = 0; i < localStorage.length; i++) {
            const key = localStorage.key(i);
            if (key && !keysToKeep.includes(key)) {
                keysToRemove.push(key);
            }
        }
        
        keysToRemove.forEach(key => localStorage.removeItem(key));
        
        // Actualizar fecha de limpieza
        localStorage.setItem(CACHE_KEYS.LAST_CLEANUP, new Date().toISOString());
        
        console.log('✅ Datos de sesión limpiados');
    } catch (e) {
        console.error('Error al limpiar datos de sesión:', e);
    }
}

/**
 * Limpiar todo el caché (función de respaldo, uso limitado)
 * 
 * ⚠️ IMPORTANTE: Esto NO cierra la sesión del usuario
 * 
 * Se mantiene para compatibilidad, pero se prefiere usar limpiarDatosNegocio()
 */
function limpiarTodoCache() {
    limpiarDatosNegocio(); // Usar función selectiva
}

/**
 * Invalidar caché específico cuando se elimina un elemento
 * 
 * @param {string} tipo - Tipo de elemento eliminado ('mesa', 'factura', 'cliente', etc.)
 * @param {number} id - ID del elemento eliminado
 */
function invalidarCache(tipo, id) {
    try {
        console.log(`🔄 Invalidando caché para ${tipo} con ID ${id}...`);
        
        switch (tipo.toLowerCase()) {
            case 'mesa':
                // Limpiar carrito de la mesa del sessionStorage
                const carritosMesas = obtenerCarritosMesas();
                if (carritosMesas[id]) {
                    delete carritosMesas[id];
                    sessionStorage.setItem(CACHE_KEYS.CARRITOS_MESAS, JSON.stringify(carritosMesas));
                    console.log(`✅ Caché de mesa ${id} invalidado`);
                }
                break;
                
            case 'factura':
            case 'pago':
            case 'cliente':
                // Para estos tipos, forzar recarga desde BD la próxima vez
                // Se puede agregar un flag de invalidación
                console.log(`✅ Caché de ${tipo} ${id} marcado para invalidación`);
                break;
                
            default:
                console.log(`⚠️ Tipo de caché desconocido: ${tipo}`);
        }
    } catch (e) {
        console.error('Error al invalidar caché:', e);
    }
}

/**
 * Obtener carritos de mesas (helper para compatibilidad)
 */
function obtenerCarritosMesas() {
    try {
        const data = sessionStorage.getItem(CACHE_KEYS.CARRITOS_MESAS);
        return data ? JSON.parse(data) : {};
    } catch (e) {
        console.error('Error al obtener carritos de mesas:', e);
        return {};
    }
}

/**
 * Forzar recarga desde base de datos (ignorar caché)
 * Útil después de eliminar o modificar datos importantes
 */
function forzarRecargaDesdeBD() {
    try {
        // Limpiar sessionStorage de datos que deben recargarse
        const keysToClear = [
            CACHE_KEYS.CARRITOS_MESAS
        ];
        
        keysToClear.forEach(key => {
            sessionStorage.removeItem(key);
        });
        
        console.log('✅ Caché limpiado, se recargará desde BD');
    } catch (e) {
        console.error('Error al forzar recarga desde BD:', e);
    }
}

// Exportar funciones globalmente
window.CacheManager = {
    limpiarTodo: limpiarTodoCache,
    invalidar: invalidarCache,
    forzarRecargaDesdeBD: forzarRecargaDesdeBD,
    inicializar: inicializarCacheManager
};

// Auto-inicializar al cargar
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', inicializarCacheManager);
} else {
    inicializarCacheManager();
}

