/**
 * Layout Theme Management
 * Manejo de temas DaisyUI
 */

(function() {
    'use strict';
    
    // Aplicar tema desde localStorage al cargar
    function applyTheme() {
        try {
            const theme = localStorage.getItem('tema') || 'business';
            document.documentElement.setAttribute('data-theme', theme);
        } catch(e) {
            console.warn('Error al aplicar tema:', e);
        }
    }
    
    // Función global para aplicar tema (usada desde Configuraciones y Settings)
    window.aplicarTema = function(tema) {
        document.documentElement.setAttribute('data-theme', tema);
        try {
            localStorage.setItem('tema', tema);
        } catch (e) {
            console.warn('Error al guardar tema:', e);
        }
    };
    
    // Función para obtener el tema actual
    window.obtenerTema = function() {
        try {
            return localStorage.getItem('tema') || 'business';
        } catch (e) {
            return 'business';
        }
    };
    
    // Aplicar tema inmediatamente
    applyTheme();
    
    // También aplicar cuando el DOM esté listo (respaldo)
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', applyTheme);
    } else {
        applyTheme();
    }
})();

