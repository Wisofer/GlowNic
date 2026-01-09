/**
 * Layout Sidebar Utilities
 * Funciones auxiliares reutilizables para el manejo del sidebar
 */

(function() {
    'use strict';
    
    // Constantes para anchos del sidebar
    const SIDEBAR_WIDTHS = {
        COLLAPSED_MOBILE: '3rem',
        COLLAPSED_DESKTOP: '3.5rem',
        EXPANDED_MOBILE: '16rem',
        EXPANDED_DESKTOP: '14rem'
    };
    
    // Breakpoints
    const BREAKPOINTS = {
        MOBILE: 640,
        DESKTOP: 1024
    };
    
    /**
     * Obtiene el ancho del sidebar según su estado y tamaño de pantalla
     * @param {boolean} isCollapsed - Si el sidebar está colapsado
     * @param {boolean} isMobile - Si es móvil
     * @returns {string} Ancho del sidebar
     */
    function getSidebarWidth(isCollapsed, isMobile) {
        if (isCollapsed) {
            return isMobile ? SIDEBAR_WIDTHS.COLLAPSED_MOBILE : SIDEBAR_WIDTHS.COLLAPSED_DESKTOP;
        }
        return isMobile ? SIDEBAR_WIDTHS.EXPANDED_MOBILE : SIDEBAR_WIDTHS.EXPANDED_DESKTOP;
    }
    
    /**
     * Detecta el tamaño de pantalla actual
     * @returns {Object} Objeto con flags isMobile, isTablet, isDesktop
     */
    function getScreenSize() {
        const width = window.innerWidth;
        return {
            isMobile: width < BREAKPOINTS.MOBILE,
            isTablet: width >= BREAKPOINTS.MOBILE && width < BREAKPOINTS.DESKTOP,
            isDesktop: width >= BREAKPOINTS.DESKTOP
        };
    }
    
    /**
     * Aplica estilos de ancho al sidebar
     * @param {HTMLElement} element - Elemento al que aplicar estilos
     * @param {string} width - Ancho a aplicar
     * @param {boolean} useImportant - Si usar !important
     */
    function applySidebarWidth(element, width, useImportant = false) {
        if (!element) return;
        
        if (useImportant) {
            element.style.setProperty('width', width, 'important');
            element.style.setProperty('min-width', width, 'important');
            element.style.setProperty('max-width', width, 'important');
        } else {
            element.style.width = width;
            element.style.minWidth = width;
            element.style.maxWidth = width;
        }
    }
    
    /**
     * Aplica clases y estilos al sidebar según su estado
     * @param {HTMLElement} sidebar - Elemento sidebar
     * @param {HTMLElement} main - Elemento mainContent
     * @param {boolean} isCollapsed - Si está colapsado
     * @param {string} width - Ancho a aplicar
     * @param {boolean} useImportant - Si usar !important
     */
    function applySidebarStateClasses(sidebar, main, isCollapsed, width, useImportant = false) {
        if (!sidebar) return;
        
        // Aplicar clases
        sidebar.classList.remove('sidebar-collapsed', 'sidebar-expanded');
        sidebar.classList.add(isCollapsed ? 'sidebar-collapsed' : 'sidebar-expanded');
        
        // Aplicar ancho
        applySidebarWidth(sidebar, width, useImportant);
        
        // Aplicar margen al mainContent si es necesario
        if (main) {
            if (isCollapsed) {
                main.classList.add('sidebar-collapsed-margin');
            } else {
                main.classList.remove('sidebar-collapsed-margin');
            }
            
            const screenSize = getScreenSize();
            if (screenSize.isDesktop || screenSize.isTablet) {
                if (useImportant) {
                    main.style.setProperty('margin-left', width, 'important');
                } else {
                    main.style.marginLeft = width;
                }
            }
        }
    }
    
    // Exportar funciones al objeto window para uso global
    window.SidebarUtils = {
        getSidebarWidth: getSidebarWidth,
        getScreenSize: getScreenSize,
        applySidebarWidth: applySidebarWidth,
        applySidebarStateClasses: applySidebarStateClasses,
        WIDTHS: SIDEBAR_WIDTHS,
        BREAKPOINTS: BREAKPOINTS
    };
})();

