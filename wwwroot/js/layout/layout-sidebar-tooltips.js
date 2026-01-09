/**
 * Layout Sidebar Tooltips
 * Muestra tooltips tipo banner cuando el sidebar está colapsado
 */

(function() {
    'use strict';
    
    let tooltipElement = null;
    let tooltipArrow = null;
    
    // Crear elemento tooltip
    function createTooltip() {
        if (tooltipElement) return;
        
        tooltipElement = document.createElement('div');
        tooltipElement.id = 'sidebar-tooltip';
        tooltipElement.style.cssText = `
            position: fixed;
            padding: 0.5rem 0.875rem;
            background: hsl(var(--b1));
            color: hsl(var(--bc));
            border: 1px solid hsl(var(--b3));
            border-radius: 0.5rem;
            white-space: nowrap;
            z-index: 9999;
            font-size: 0.875rem;
            font-weight: 500;
            pointer-events: none;
            opacity: 0;
            transform: translateX(-10px);
            transition: opacity 0.2s ease, transform 0.2s ease;
            box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15), 0 2px 4px rgba(0, 0, 0, 0.1);
            backdrop-filter: blur(8px);
            display: none;
        `;
        
        tooltipArrow = document.createElement('div');
        tooltipArrow.id = 'sidebar-tooltip-arrow';
        tooltipArrow.style.cssText = `
            position: fixed;
            width: 0;
            height: 0;
            border-top: 6px solid transparent;
            border-bottom: 6px solid transparent;
            border-right: 6px solid hsl(var(--b1));
            z-index: 10000;
            pointer-events: none;
            opacity: 0;
            transition: opacity 0.2s ease;
            display: none;
        `;
        
        document.body.appendChild(tooltipElement);
        document.body.appendChild(tooltipArrow);
    }
    
    // Mostrar tooltip
    function showTooltip(element, text) {
        if (!tooltipElement) createTooltip();
        
        const sidebar = document.getElementById('sidebar');
        if (!sidebar || !sidebar.classList.contains('sidebar-collapsed')) {
            return;
        }
        
        // Obtener posición del elemento
        const rect = element.getBoundingClientRect();
        const sidebarRect = sidebar.getBoundingClientRect();
        
        // Calcular posición del tooltip (al lado derecho del sidebar)
        // 0.75rem ≈ 12px (asumiendo 16px base)
        const tooltipLeft = sidebarRect.right + 12;
        const tooltipTop = rect.top + (rect.height / 2);
        
        // Posición de la flecha
        const arrowLeft = sidebarRect.right;
        const arrowTop = rect.top + (rect.height / 2);
        
        // Aplicar posiciones
        tooltipElement.textContent = text;
        tooltipElement.style.left = tooltipLeft + 'px';
        tooltipElement.style.top = tooltipTop + 'px';
        tooltipElement.style.transform = 'translateY(-50%) translateX(0)';
        tooltipElement.style.display = 'block';
        
        tooltipArrow.style.left = arrowLeft + 'px';
        tooltipArrow.style.top = arrowTop + 'px';
        tooltipArrow.style.transform = 'translateY(-50%) translateX(-6px)';
        tooltipArrow.style.display = 'block';
        
        // Animar entrada
        requestAnimationFrame(() => {
            tooltipElement.style.opacity = '1';
            tooltipArrow.style.opacity = '1';
        });
    }
    
    // Ocultar tooltip
    function hideTooltip() {
        if (!tooltipElement || !tooltipArrow) return;
        
        // Ocultar inmediatamente
        tooltipElement.style.opacity = '0';
        tooltipArrow.style.opacity = '0';
        tooltipElement.style.transform = 'translateY(-50%) translateX(-10px)';
        
        // Remover después de la animación
        setTimeout(() => {
            if (tooltipElement) {
                tooltipElement.style.display = 'none';
            }
            if (tooltipArrow) {
                tooltipArrow.style.display = 'none';
            }
        }, 200);
    }
    
    // Configurar tooltips en los items del sidebar
    function setupTooltips() {
        const sidebar = document.getElementById('sidebar');
        if (!sidebar) return;
        
        // Tooltips para items del menú
        const sidebarItems = sidebar.querySelectorAll('.sidebar-item');
        sidebarItems.forEach(function(item) {
            const tooltipText = item.getAttribute('data-tooltip');
            if (!tooltipText) return;
            
            // Mostrar tooltip al pasar el mouse
            item.addEventListener('mouseenter', function(e) {
                const isCollapsed = sidebar.classList.contains('sidebar-collapsed');
                if (isCollapsed) {
                    showTooltip(e.currentTarget, tooltipText);
                }
            });
            
            // Ocultar tooltip al quitar el mouse
            item.addEventListener('mouseleave', function() {
                hideTooltip();
            });
            
            // Ocultar tooltip al hacer click
            item.addEventListener('click', function() {
                hideTooltip();
            });
            
            // Ocultar tooltip si el mouse sale del sidebar
            item.addEventListener('mouseout', function(e) {
                // Verificar si el mouse realmente salió del elemento
                if (!item.contains(e.relatedTarget)) {
                    hideTooltip();
                }
            });
        });
        
        // Tooltip para el logo
        const logoLink = sidebar.querySelector('.sidebar-logo-link');
        if (logoLink) {
            logoLink.addEventListener('mouseenter', function(e) {
                const isCollapsed = sidebar.classList.contains('sidebar-collapsed');
                if (isCollapsed) {
                    showTooltip(e.currentTarget, 'Inicio');
                }
            });
            
            logoLink.addEventListener('mouseleave', function() {
                hideTooltip();
            });
            
            logoLink.addEventListener('click', function() {
                hideTooltip();
            });
            
            logoLink.addEventListener('mouseout', function(e) {
                if (!logoLink.contains(e.relatedTarget)) {
                    hideTooltip();
                }
            });
        }
        
        // Ocultar tooltip cuando el mouse sale completamente del sidebar
        sidebar.addEventListener('mouseleave', function() {
            hideTooltip();
        });
    }
    
    // Actualizar posición del tooltip cuando se hace scroll o resize
    function updateTooltipPosition() {
        if (!tooltipElement || tooltipElement.style.display === 'none') return;
        
        const sidebar = document.getElementById('sidebar');
        if (!sidebar || !sidebar.classList.contains('sidebar-collapsed')) {
            hideTooltip();
            return;
        }
        
        // Buscar el elemento activo (el que tiene el hover)
        const activeItem = sidebar.querySelector('.sidebar-item:hover, .sidebar-logo-link:hover');
        if (activeItem) {
            const tooltipText = activeItem.getAttribute('data-tooltip') || 'Inicio';
            showTooltip(activeItem, tooltipText);
        }
    }
    
    // Inicializar
    function init() {
        createTooltip();
        setupTooltips();
        
        // Actualizar posición en scroll y resize
        window.addEventListener('scroll', updateTooltipPosition, true);
        window.addEventListener('resize', updateTooltipPosition);
    }
    
    // Ejecutar cuando el DOM esté listo
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
    
    // Reconfigurar cuando el sidebar cambia de estado
    function setupSidebarObserver() {
        const sidebar = document.getElementById('sidebar');
        if (sidebar) {
            const observer = new MutationObserver(function(mutations) {
                mutations.forEach(function(mutation) {
                    if (mutation.type === 'attributes' && mutation.attributeName === 'class') {
                        // Ocultar tooltip cuando el sidebar se expande
                        if (!sidebar.classList.contains('sidebar-collapsed')) {
                            hideTooltip();
                        } else {
                            // Reconfigurar tooltips cuando se colapsa
                            setupTooltips();
                        }
                    }
                });
            });
            
            observer.observe(sidebar, {
                attributes: true,
                attributeFilter: ['class']
            });
        }
    }
    
    // Configurar observer después de que el DOM esté listo
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', setupSidebarObserver);
    } else {
        setupSidebarObserver();
    }
    
    // También ocultar tooltip cuando se hace click en cualquier parte del documento
    document.addEventListener('click', function(e) {
        const sidebar = document.getElementById('sidebar');
        if (sidebar && sidebar.classList.contains('sidebar-collapsed')) {
            // Solo ocultar si el click no fue en un item del sidebar
            const clickedItem = e.target.closest('.sidebar-item, .sidebar-logo-link');
            if (!clickedItem) {
                hideTooltip();
            }
        } else {
            hideTooltip();
        }
    });
    
})();

