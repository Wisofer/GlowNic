/**
 * Layout Sidebar Management
 * Manejo del sidebar (abrir/cerrar, colapsar/expandir)
 * Refactorizado para usar utilidades y eliminar duplicación
 */

(function() {
    'use strict';
    
    // Verificar que las utilidades estén disponibles
    if (!window.SidebarUtils) {
        console.error('SidebarUtils no está disponible. Asegúrate de cargar layout-sidebar-utils.js primero.');
        return;
    }
    
    let sidebarOpen = false;
    let sidebarCollapsed = false;
    let sidebarStateRestored = false;
    const STORAGE_KEY = 'sidebarState';
    const COLLAPSE_STORAGE_KEY = 'sidebarCollapsed';
    
    // ==================== Funciones de LocalStorage ====================
    
    function saveSidebarState(state) {
        try {
            localStorage.setItem(STORAGE_KEY, state ? 'open' : 'closed');
        } catch (e) {
            console.warn('No se pudo guardar el estado del sidebar:', e);
        }
    }
    
    function getSavedSidebarState() {
        try {
            const state = localStorage.getItem(STORAGE_KEY);
            return state === 'open';
        } catch (e) {
            console.warn('No se pudo leer el estado del sidebar:', e);
            return null;
        }
    }
    
    function getCollapsedState() {
        try {
            const state = localStorage.getItem(COLLAPSE_STORAGE_KEY);
            return state === 'true';
        } catch (e) {
            return false;
        }
    }
    
    function saveCollapsedState(collapsed) {
        try {
            localStorage.setItem(COLLAPSE_STORAGE_KEY, collapsed ? 'true' : 'false');
        } catch (e) {
            console.warn('No se pudo guardar el estado de colapso:', e);
        }
    }
    
    // ==================== Funciones de Estado del Sidebar ====================
    
    /**
     * Aplica el estado del sidebar (abierto/cerrado)
     */
    function applySidebarState(isOpen, sidebar, main, overlay) {
        if (!sidebar) return;
        
        sidebarOpen = isOpen;
        const screenSize = window.SidebarUtils.getScreenSize();
        const isCollapsed = sidebar.classList.contains('sidebar-collapsed');
        const width = window.SidebarUtils.getSidebarWidth(isCollapsed, screenSize.isMobile);
        
        if (isOpen) {
            // Abrir sidebar
            sidebar.style.setProperty('transform', 'translateX(0)', 'important');
            sidebar.style.setProperty('transition', 'transform 0.3s ease-in-out, width 0.3s ease-in-out', 'important');
            window.SidebarUtils.applySidebarWidth(sidebar, width, true);
            
            // Aplicar margen en desktop y tablet
            if (main && (screenSize.isDesktop || screenSize.isTablet)) {
                main.classList.remove('sidebar-open-expanded', 'sidebar-open-collapsed', 'sidebar-collapsed-margin');
                
                if (isCollapsed) {
                    main.classList.add('sidebar-open-collapsed', 'sidebar-collapsed-margin');
                } else {
                    main.classList.add('sidebar-open-expanded');
                }
                
                requestAnimationFrame(() => {
                    main.style.setProperty('margin-left', width, 'important');
                    main.style.setProperty('transition', 'margin-left 0.3s ease-in-out', 'important');
                });
            }
            
            // Mostrar overlay solo en móvil
            if (overlay && screenSize.isMobile) {
                overlay.style.display = 'block';
                requestAnimationFrame(() => {
                    overlay.style.opacity = '1';
                    overlay.style.transition = 'opacity 0.3s ease-in-out';
                });
            }
        } else {
            // Cerrar sidebar
            sidebar.style.setProperty('transform', 'translateX(-100%)', 'important');
            sidebar.style.setProperty('transition', 'transform 0.3s ease-in-out, width 0.3s ease-in-out', 'important');
            
            if (main) {
                main.classList.remove('sidebar-open-expanded', 'sidebar-open-collapsed', 'sidebar-collapsed-margin');
                requestAnimationFrame(() => {
                    main.style.setProperty('margin-left', '0', 'important');
                    main.style.setProperty('transition', 'margin-left 0.3s ease-in-out', 'important');
                });
            }
            
            if (overlay) {
                overlay.style.opacity = '0';
                overlay.style.transition = 'opacity 0.3s ease-in-out';
                setTimeout(() => {
                    overlay.style.display = 'none';
                }, 300);
            }
        }
    }
    
    /**
     * Aplica el estado colapsado/expandido del sidebar
     */
    function applyCollapsedState() {
        const sidebar = document.getElementById('sidebar');
        const main = document.getElementById('mainContent');
        
        if (!sidebar) return;
        
        const screenSize = window.SidebarUtils.getScreenSize();
        sidebarCollapsed = getCollapsedState();
        const width = window.SidebarUtils.getSidebarWidth(sidebarCollapsed, screenSize.isMobile);
        
        window.SidebarUtils.applySidebarStateClasses(sidebar, main, sidebarCollapsed, width, true);
        
        // Aplicar margen si el sidebar está abierto
        if (sidebarOpen && (screenSize.isDesktop || screenSize.isTablet) && main) {
            main.style.setProperty('margin-left', width, 'important');
        }
    }
    
    /**
     * Restaura el estado del sidebar desde localStorage
     */
    function restoreSidebarState(sidebar, main, overlay) {
        if (sidebarStateRestored) return;
        
        const screenSize = window.SidebarUtils.getScreenSize();
        const savedState = getSavedSidebarState();
        sidebarCollapsed = getCollapsedState();
        const sidebarWidth = window.SidebarUtils.getSidebarWidth(sidebarCollapsed, screenSize.isMobile);
        
        // Aplicar estado colapsado/expandido
        if (sidebarCollapsed) {
            sidebar.classList.remove('sidebar-expanded');
            sidebar.classList.add('sidebar-collapsed');
        } else {
            sidebar.classList.remove('sidebar-collapsed');
            sidebar.classList.add('sidebar-expanded');
        }
        window.SidebarUtils.applySidebarWidth(sidebar, sidebarWidth, false);
        
        // Determinar si debe estar abierto
        let shouldBeOpen = false;
        if (screenSize.isDesktop) {
            shouldBeOpen = savedState !== null ? savedState : true;
        } else {
            shouldBeOpen = false;
        }
        
        if (shouldBeOpen) {
            sidebar.style.transition = 'none';
            sidebar.style.transform = 'translateX(0)';
            
            if (main && (screenSize.isDesktop || screenSize.isTablet)) {
                main.classList.remove('sidebar-open-expanded', 'sidebar-open-collapsed', 'sidebar-collapsed-margin');
                if (sidebarCollapsed) {
                    main.classList.add('sidebar-open-collapsed', 'sidebar-collapsed-margin');
                } else {
                    main.classList.add('sidebar-open-expanded');
                }
                main.style.transition = 'none';
                main.style.marginLeft = sidebarWidth;
            }
            
            if (overlay && screenSize.isMobile) {
                overlay.style.display = 'block';
                overlay.style.opacity = '1';
            }
            
            setTimeout(() => {
                sidebar.style.transition = 'transform 0.3s ease-in-out, width 0.3s ease-in-out';
                if (main) main.style.transition = 'margin-left 0.3s ease-in-out';
            }, 50);
        } else {
            sidebar.style.transition = 'none';
            sidebar.style.transform = 'translateX(-100%)';
            if (main) {
                main.style.transition = 'none';
                main.style.marginLeft = '0';
            }
            if (overlay) overlay.style.display = 'none';
            
            setTimeout(() => {
                sidebar.style.transition = 'transform 0.3s ease-in-out, width 0.3s ease-in-out';
                if (main) main.style.transition = 'margin-left 0.3s ease-in-out';
            }, 50);
        }
        
        sidebarOpen = shouldBeOpen;
        sidebarStateRestored = true;
    }
    
    // ==================== Funciones de Toggle ====================
    
    function toggleSidebar() {
        const sidebar = document.getElementById('sidebar');
        const main = document.getElementById('mainContent');
        const overlay = document.getElementById('sidebarOverlay');
        
        if (!sidebar) return;
        
        const isCollapsed = sidebar.classList.contains('sidebar-collapsed');
        const screenSize = window.SidebarUtils.getScreenSize();
        
        if (screenSize.isDesktop) {
            if (sidebarOpen) {
                toggleSidebarCollapse();
            } else {
                // Abrir y expandir
                sidebarCollapsed = false;
                sidebar.classList.remove('sidebar-collapsed');
                sidebar.classList.add('sidebar-expanded');
                
                const expandedWidth = window.SidebarUtils.getSidebarWidth(false, screenSize.isMobile);
                window.SidebarUtils.applySidebarWidth(sidebar, expandedWidth, true);
                saveCollapsedState(false);
                
                sidebarOpen = true;
                applySidebarState(true, sidebar, main, overlay);
                saveSidebarState(true);
            }
        } else {
            sidebarOpen = !sidebarOpen;
            applySidebarState(sidebarOpen, sidebar, main, overlay);
            saveSidebarState(sidebarOpen);
        }
    }
    
    function closeSidebar() {
        const sidebar = document.getElementById('sidebar');
        const main = document.getElementById('mainContent');
        const overlay = document.getElementById('sidebarOverlay');
        
        sidebarOpen = false;
        applySidebarState(false, sidebar, main, overlay);
        saveSidebarState(false);
    }
    
    function toggleSidebarCollapse() {
        const sidebar = document.getElementById('sidebar');
        const main = document.getElementById('mainContent');
        
        if (!sidebar) return;
        
        sidebarCollapsed = !sidebarCollapsed;
        const screenSize = window.SidebarUtils.getScreenSize();
        const width = window.SidebarUtils.getSidebarWidth(sidebarCollapsed, screenSize.isMobile);
        
        window.SidebarUtils.applySidebarStateClasses(sidebar, main, sidebarCollapsed, width, true);
        
        // Aplicar margen si está abierto
        if (sidebarOpen && (screenSize.isDesktop || screenSize.isTablet) && main) {
            main.style.setProperty('margin-left', width, 'important');
        }
        
        saveCollapsedState(sidebarCollapsed);
    }
    
    // ==================== Inicialización ====================
    
    /**
     * Prevenir que los enlaces del sidebar expandan el sidebar cuando está colapsado
     */
    function setupSidebarLinkListeners(sidebar, main) {
        const sidebarLinks = sidebar.querySelectorAll('.sidebar-item');
        sidebarLinks.forEach(function(link) {
            link.addEventListener('click', function(e) {
                const isCollapsed = sidebar.classList.contains('sidebar-collapsed');
                if (isCollapsed) {
                    const screenSize = window.SidebarUtils.getScreenSize();
                    const collapsedWidth = window.SidebarUtils.getSidebarWidth(true, screenSize.isMobile);
                    
                    // Forzar que el estado colapsado se mantenga
                    sidebar.classList.remove('sidebar-expanded');
                    sidebar.classList.add('sidebar-collapsed');
                    window.SidebarUtils.applySidebarWidth(sidebar, collapsedWidth, true);
                    
                    // Guardar el estado colapsado antes de navegar
                    saveCollapsedState(true);
                    
                    // También guardar en el mainContent
                    if (main) {
                        main.classList.add('sidebar-collapsed-margin');
                        main.style.setProperty('margin-left', collapsedWidth, 'important');
                    }
                }
            }, true); // Usar capture phase
        });
    }
    
    function init() {
        const sidebar = document.getElementById('sidebar');
        const main = document.getElementById('mainContent');
        const overlay = document.getElementById('sidebarOverlay');
        
        if (!sidebar || !main) {
            // Esperar a que el DOM esté listo
            const observer = new MutationObserver(function(mutations, obs) {
                const sb = document.getElementById('sidebar');
                const m = document.getElementById('mainContent');
                const ov = document.getElementById('sidebarOverlay');
                
                if (sb && m) {
                    restoreSidebarState(sb, m, ov);
                    obs.disconnect();
                }
            });
            
            observer.observe(document.documentElement, {
                childList: true,
                subtree: true
            });
            
            setTimeout(() => observer.disconnect(), 5000);
            return;
        }
        
        // Aplicar estados
        applyCollapsedState();
        restoreSidebarState(sidebar, main, overlay);
        
        // Configurar botón hamburguesa
        const btn = document.getElementById('hamburgerBtn');
        if (btn) {
            btn.onclick = function(e) {
                e.preventDefault();
                e.stopPropagation();
                toggleSidebar();
                return false;
            };
        }
        
        // Configurar listeners de enlaces
        setupSidebarLinkListeners(sidebar, main);
        
        // Guardar estado antes de cerrar la página
        window.addEventListener('beforeunload', function() {
            const isCollapsed = sidebar.classList.contains('sidebar-collapsed');
            saveCollapsedState(isCollapsed);
        });
    }
    
    // ==================== Event Listeners Globales ====================
    
    // Funciones globales
    window.toggleSidebar = toggleSidebar;
    window.closeSidebar = closeSidebar;
    window.toggleSidebarCollapse = toggleSidebarCollapse;
    
    // Ejecutar inicialización
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
        setTimeout(init, 0);
    } else {
        init();
    }
    
    // Respaldo cuando la página esté completamente cargada
    window.addEventListener('load', function() {
        if (sidebarStateRestored) return;
        
        const sidebar = document.getElementById('sidebar');
        const main = document.getElementById('mainContent');
        const overlay = document.getElementById('sidebarOverlay');
        if (sidebar && main) {
            restoreSidebarState(sidebar, main, overlay);
        }
    });
    
    // Aplicar estado colapsado como respaldo
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', function() {
            setTimeout(function() {
                const sidebar = document.getElementById('sidebar');
                if (sidebar && !sidebar.classList.contains('sidebar-collapsed') && !sidebar.classList.contains('sidebar-expanded')) {
                    applyCollapsedState();
                }
            }, 150);
        });
    } else {
        setTimeout(function() {
            const sidebar = document.getElementById('sidebar');
            if (sidebar && !sidebar.classList.contains('sidebar-collapsed') && !sidebar.classList.contains('sidebar-expanded')) {
                applyCollapsedState();
            }
        }, 150);
    }
    
    // Manejar resize y zoom
    let resizeTimeout;
    window.addEventListener('resize', function() {
        clearTimeout(resizeTimeout);
        resizeTimeout = setTimeout(function() {
            const sidebar = document.getElementById('sidebar');
            const main = document.getElementById('mainContent');
            const overlay = document.getElementById('sidebarOverlay');
            
            if (sidebar && main) {
                applyCollapsedState();
                if (sidebarOpen) {
                    applySidebarState(true, sidebar, main, overlay);
                }
            }
        }, 150);
    });
    
    // Manejar cambios de zoom usando ResizeObserver
    if (window.ResizeObserver) {
        const resizeObserver = new ResizeObserver(function(entries) {
            const sidebar = document.getElementById('sidebar');
            const main = document.getElementById('mainContent');
            const overlay = document.getElementById('sidebarOverlay');
            
            if (sidebar && main) {
                clearTimeout(resizeTimeout);
                resizeTimeout = setTimeout(function() {
                    applyCollapsedState();
                    if (sidebarOpen) {
                        applySidebarState(true, sidebar, main, overlay);
                    }
                }, 150);
            }
        });
        
        if (document.body) {
            resizeObserver.observe(document.body);
        }
    }
})();
