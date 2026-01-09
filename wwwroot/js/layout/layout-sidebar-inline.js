/**
 * Layout Sidebar Inline Script
 * Script que se ejecuta ANTES del render para evitar flash visual del sidebar
 * Este script debe cargarse en el <head> antes de que se renderice el body
 */

(function() {
    'use strict';
    
    try {
        // Guardar estado antes de navegar (si hay un enlace siendo clickeado)
        document.addEventListener('click', function(e) {
            const link = e.target.closest('a.sidebar-item');
            if (link) {
                const sidebar = document.getElementById('sidebar');
                if (sidebar) {
                    const isCollapsed = sidebar.classList.contains('sidebar-collapsed');
                    try {
                        localStorage.setItem('sidebarCollapsed', isCollapsed ? 'true' : 'false');
                    } catch(err) {
                        // Silenciar errores de localStorage
                    }
                }
            }
        }, true); // Usar capture phase para ejecutar antes
        
        const sidebarState = localStorage.getItem('sidebarState');
        const isDesktop = window.innerWidth >= 1024;
        
        if (isDesktop && sidebarState === 'open') {
            // Leer estado colapsado PRIMERO para usar el ancho correcto desde el inicio
            const sidebarCollapsed = localStorage.getItem('sidebarCollapsed') === 'true';
            const isMobile = window.innerWidth < 640;
            
            // Calcular ancho usando lógica simple (las utils pueden no estar cargadas aún)
            const sidebarWidth = sidebarCollapsed 
                ? (isMobile ? '3rem' : '3.5rem')
                : (isMobile ? '16rem' : '14rem');
            
            // Crear y aplicar estilo inmediatamente con el ancho correcto
            const style = document.createElement('style');
            style.id = 'sidebar-initial-state';
            style.textContent = `
                #sidebar {
                    transform: translateX(0) !important;
                    transition: none !important;
                    width: ${sidebarWidth} !important;
                    min-width: ${sidebarWidth} !important;
                    max-width: ${sidebarWidth} !important;
                }
                #mainContent {
                    margin-left: ${sidebarWidth} !important;
                    transition: none !important;
                }
                /* Forzar ancho según estado colapsado */
                ${sidebarCollapsed ? `
                #sidebar.sidebar-collapsed,
                #sidebar.sidebar-expanded,
                #sidebar[class*="sidebar"],
                #sidebar {
                    width: ${sidebarWidth} !important;
                    min-width: ${sidebarWidth} !important;
                    max-width: ${sidebarWidth} !important;
                }
                #mainContent.sidebar-collapsed-margin,
                #mainContent[class*="sidebar"],
                #mainContent {
                    margin-left: ${sidebarWidth} !important;
                }
                ` : `
                #sidebar.sidebar-expanded,
                #sidebar.sidebar-collapsed,
                #sidebar[class*="sidebar"],
                #sidebar {
                    width: ${sidebarWidth} !important;
                    min-width: ${sidebarWidth} !important;
                    max-width: ${sidebarWidth} !important;
                }
                `}
            `;
            (document.head || document.getElementsByTagName('head')[0]).appendChild(style);
            
            // Aplicar clases inmediatamente cuando el DOM esté disponible
            function applyClasses() {
                const sidebar = document.getElementById('sidebar');
                const main = document.getElementById('mainContent');
                
                if (sidebar) {
                    sidebar.classList.remove('sidebar-collapsed', 'sidebar-expanded');
                    sidebar.classList.add(sidebarCollapsed ? 'sidebar-collapsed' : 'sidebar-expanded');
                    
                    // Forzar ancho inmediatamente
                    sidebar.style.setProperty('width', sidebarWidth, 'important');
                    sidebar.style.setProperty('min-width', sidebarWidth, 'important');
                    sidebar.style.setProperty('max-width', sidebarWidth, 'important');
                }
                
                if (main) {
                    if (sidebarCollapsed) {
                        main.classList.add('sidebar-collapsed-margin');
                    } else {
                        main.classList.remove('sidebar-collapsed-margin');
                    }
                    main.style.setProperty('margin-left', sidebarWidth, 'important');
                }
            }
            
            // Intentar aplicar inmediatamente si el DOM ya está listo
            if (document.body) {
                applyClasses();
            } else {
                document.addEventListener('DOMContentLoaded', applyClasses, { once: true });
            }
            
            // Aplicar en el siguiente tick para asegurar
            setTimeout(applyClasses, 0);
            setTimeout(applyClasses, 10);
            setTimeout(applyClasses, 50);
            
            // Remover el estilo después de que se cargue el JavaScript principal
            setTimeout(function() {
                const initialStyle = document.getElementById('sidebar-initial-state');
                if (initialStyle) {
                    initialStyle.remove();
                    // Limpiar estilos inline importantes del sidebar para permitir interacciones
                    const sidebar = document.getElementById('sidebar');
                    const main = document.getElementById('mainContent');
                    if (sidebar) {
                        sidebar.style.removeProperty('width');
                        sidebar.style.removeProperty('min-width');
                        sidebar.style.removeProperty('max-width');
                    }
                    if (main) {
                        main.style.removeProperty('margin-left');
                    }
                }
            }, 300);
        }
    } catch (e) {
        // Silenciar errores si localStorage no está disponible
    }
})();

