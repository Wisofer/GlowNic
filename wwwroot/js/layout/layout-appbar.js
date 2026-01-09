/**
 * Layout App Bar Controls
 * Manejo de controles del app bar: zoom y tema
 */

(function() {
    'use strict';
    
    // ==================== Zoom ====================
    
    const btnZoomIn = document.getElementById('btnZoomIn');
    const btnZoomOut = document.getElementById('btnZoomOut');
    const zoomIndicator = document.getElementById('zoomIndicator');
    
    const ZOOM_STORAGE_KEY = 'appZoomLevel';
    const ZOOM_MIN = 50;
    const ZOOM_MAX = 200;
    const ZOOM_STEP = 10;
    const ZOOM_DEFAULT = 100;
    
    function getZoomLevel() {
        try {
            const saved = localStorage.getItem(ZOOM_STORAGE_KEY);
            return saved ? parseInt(saved, 10) : ZOOM_DEFAULT;
        } catch (e) {
            return ZOOM_DEFAULT;
        }
    }
    
    function saveZoomLevel(level) {
        try {
            localStorage.setItem(ZOOM_STORAGE_KEY, level.toString());
        } catch (e) {
            console.warn('No se pudo guardar el nivel de zoom:', e);
        }
    }
    
    function applyZoom(level) {
        // Limitar el nivel de zoom
        level = Math.max(ZOOM_MIN, Math.min(ZOOM_MAX, level));
        
        // Aplicar zoom usando CSS transform en el body
        document.body.style.zoom = level + '%';
        
        // Actualizar indicador
        if (zoomIndicator) {
            zoomIndicator.textContent = level + '%';
        }
        
        // Guardar preferencia
        saveZoomLevel(level);
        
        // Actualizar estado de botones
        if (btnZoomIn) {
            btnZoomIn.disabled = level >= ZOOM_MAX;
        }
        if (btnZoomOut) {
            btnZoomOut.disabled = level <= ZOOM_MIN;
        }
    }
    
    function zoomIn() {
        const current = getZoomLevel();
        applyZoom(current + ZOOM_STEP);
    }
    
    function zoomOut() {
        const current = getZoomLevel();
        applyZoom(current - ZOOM_STEP);
    }
    
    function resetZoom() {
        applyZoom(ZOOM_DEFAULT);
    }
    
    // Event listeners para zoom
    if (btnZoomIn) {
        btnZoomIn.addEventListener('click', zoomIn);
    }
    
    if (btnZoomOut) {
        btnZoomOut.addEventListener('click', zoomOut);
    }
    
    // Atajos de teclado para zoom (Ctrl + Plus/Minus)
    document.addEventListener('keydown', function(e) {
        if ((e.ctrlKey || e.metaKey) && (e.key === '+' || e.key === '=')) {
            e.preventDefault();
            zoomIn();
        } else if ((e.ctrlKey || e.metaKey) && e.key === '-') {
            e.preventDefault();
            zoomOut();
        } else if ((e.ctrlKey || e.metaKey) && e.key === '0') {
            e.preventDefault();
            resetZoom();
        }
    });
    
    // Aplicar zoom guardado al cargar
    function initZoom() {
        const savedZoom = getZoomLevel();
        if (savedZoom !== ZOOM_DEFAULT) {
            applyZoom(savedZoom);
        } else {
            // Inicializar botones
            if (btnZoomIn) btnZoomIn.disabled = false;
            if (btnZoomOut) btnZoomOut.disabled = false;
        }
    }
    
    // ==================== Toggle Tema ====================
    
    const btnToggleTheme = document.getElementById('btnToggleTheme');
    const iconThemeLight = document.getElementById('iconThemeLight');
    const iconThemeDark = document.getElementById('iconThemeDark');
    
    function getCurrentTheme() {
        try {
            return localStorage.getItem('tema') || 'business';
        } catch (e) {
            return 'business';
        }
    }
    
    function isDarkTheme(theme) {
        // Temas oscuros comunes en DaisyUI
        const darkThemes = ['dark', 'night', 'business', 'forest', 'black', 'luxury', 'dracula', 'halloween'];
        return darkThemes.includes(theme);
    }
    
    function updateThemeIcon() {
        const currentTheme = getCurrentTheme();
        const isDark = isDarkTheme(currentTheme);
        
        if (isDark) {
            if (iconThemeLight) iconThemeLight.classList.add('hidden');
            if (iconThemeDark) iconThemeDark.classList.remove('hidden');
            if (btnToggleTheme) {
                btnToggleTheme.setAttribute('data-tip', 'Cambiar a Tema Claro');
            }
        } else {
            if (iconThemeLight) iconThemeLight.classList.remove('hidden');
            if (iconThemeDark) iconThemeDark.classList.add('hidden');
            if (btnToggleTheme) {
                btnToggleTheme.setAttribute('data-tip', 'Cambiar a Tema Oscuro');
            }
        }
    }
    
    function toggleTheme() {
        const currentTheme = getCurrentTheme();
        const isDark = isDarkTheme(currentTheme);
        
        // Alternar entre tema oscuro y claro
        // Si está en oscuro, cambiar a claro (light)
        // Si está en claro, cambiar a oscuro (business)
        const newTheme = isDark ? 'light' : 'business';
        
        if (window.aplicarTema) {
            window.aplicarTema(newTheme);
        } else {
            document.documentElement.setAttribute('data-theme', newTheme);
            try {
                localStorage.setItem('tema', newTheme);
            } catch (e) {
                console.warn('Error al guardar tema:', e);
            }
        }
        
        updateThemeIcon();
    }
    
    // Event listener para toggle de tema
    if (btnToggleTheme) {
        btnToggleTheme.addEventListener('click', toggleTheme);
    }
    
    // ==================== Inicialización ====================
    
    function init() {
        // Inicializar zoom
        initZoom();
        
        // Inicializar iconos
        updateThemeIcon();
        
        // Observar cambios en el atributo data-theme para actualizar el icono
        const observer = new MutationObserver(function(mutations) {
            mutations.forEach(function(mutation) {
                if (mutation.type === 'attributes' && mutation.attributeName === 'data-theme') {
                    updateThemeIcon();
                }
            });
        });
        
        observer.observe(document.documentElement, {
            attributes: true,
            attributeFilter: ['data-theme']
        });
    }
    
    // Ejecutar inicialización
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
    
    // Exportar funciones globales si es necesario
    window.zoomIn = zoomIn;
    window.zoomOut = zoomOut;
    window.resetZoom = resetZoom;
    window.toggleAppTheme = toggleTheme;
    
})();
