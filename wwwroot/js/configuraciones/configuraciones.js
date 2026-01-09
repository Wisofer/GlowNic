/**
 * Configuraciones Management
 * Gestión de usuarios, temas, tipo de cambio y alertas
 */

// Gestión de Usuarios
function abrirModalEditarUsuario(id, nombreUsuario, nombreCompleto, rol, activo) {
    document.getElementById('editarId').value = id;
    document.getElementById('editarNombreUsuario').value = nombreUsuario;
    document.getElementById('editarNombreCompleto').value = nombreCompleto;
    document.getElementById('editarRol').value = rol;
    document.getElementById('editarActivo').checked = activo;
    modalEditarUsuario.showModal();
}

async function crearUsuario(e) {
    e.preventDefault();
    const formData = new FormData(e.target);
    const data = {
        nombreUsuario: formData.get('nombreUsuario'),
        contrasena: formData.get('contrasena'),
        nombreCompleto: formData.get('nombreCompleto'),
        rol: formData.get('rol'),
        activo: formData.get('activo') === 'on'
    };

    if (window.Loader) {
        window.Loader.show('Creando usuario...', 'Guardando la información');
    }

    const response = await fetch('/configuraciones/usuarios/crear', {
        method: 'POST',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
        body: new URLSearchParams(data)
    });

    const result = await response.json();
    if (window.Loader) window.Loader.hide();
    Notify.info(result.message);
    if (result.success) location.reload();
}

async function editarUsuario(e) {
    e.preventDefault();
    const formData = new FormData(e.target);
    const data = {
        id: formData.get('id'),
        nombreUsuario: formData.get('nombreUsuario'),
        contrasena: formData.get('contrasena'),
        nombreCompleto: formData.get('nombreCompleto'),
        rol: formData.get('rol'),
        activo: formData.get('activo') === 'on'
    };

    if (window.Loader) {
        window.Loader.show('Actualizando usuario...', 'Guardando los cambios');
    }

    const response = await fetch('/configuraciones/usuarios/editar', {
        method: 'POST',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
        body: new URLSearchParams(data)
    });

    const result = await response.json();
    if (window.Loader) window.Loader.hide();
    Notify.info(result.message);
    if (result.success) location.reload();
}

async function eliminarUsuario(id, nombreUsuario) {
    const confirmado = await showConfirm(
        `¿Estás seguro de eliminar al usuario "${nombreUsuario}"?`,
        'Eliminar Usuario'
    );
    if (!confirmado) return;

    if (window.Loader) {
        window.Loader.show('Eliminando usuario...', 'Procesando la eliminación');
    }

    const formData = new FormData();
    formData.append('id', id);

    const response = await fetch('/configuraciones/usuarios/eliminar', {
        method: 'POST',
        body: formData
    });

    const result = await response.json();
    if (window.Loader) window.Loader.hide();
    Notify.info(result.message);
    if (result.success) location.reload();
}

// Gestión de Tema con DaisyUI
function cambiarTema(tema) {
    document.documentElement.setAttribute('data-theme', tema);
    localStorage.setItem('tema', tema);
    actualizarBotonesTema(tema);
    
    fetch('/configuraciones/tema', {
        method: 'POST',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
        body: new URLSearchParams({ tema: tema })
    }).catch(err => console.warn('No se pudo guardar tema en servidor:', err));
}

function actualizarBotonesTema(tema) {
    const botones = {
        'business': document.getElementById('btnTemaOscuro'),
        'corporate': document.getElementById('btnTemaClaro'),
        'night': document.getElementById('btnTemaNight'),
        'luxury': document.getElementById('btnTemaLuxury')
    };
    
    Object.entries(botones).forEach(([key, btn]) => {
        if (!btn) return;
        if (key === tema) {
            btn.classList.remove('btn-outline');
            btn.classList.add('btn-primary');
        } else {
            btn.classList.add('btn-outline');
            btn.classList.remove('btn-primary');
        }
    });
}

// Inicializar tema al cargar
document.addEventListener('DOMContentLoaded', function() {
    const temaGuardado = localStorage.getItem('tema') || 'business';
    document.documentElement.setAttribute('data-theme', temaGuardado);
    actualizarBotonesTema(temaGuardado);
    
    // Inicializar estado de sonidos desde el servidor/ViewBag
    const sonidosToggle = document.getElementById('toggleSonidosNotificacion');
    if (sonidosToggle) {
        const sonidosHabilitados = sonidosToggle.checked;
        localStorage.setItem('sonidosHabilitados', sonidosHabilitados.toString());
    }
});

// Tipo de Cambio
async function actualizarTipoCambio(e) {
    e.preventDefault();
    const formData = new FormData(e.target);
    let valor = formData.get('valor').toString().trim().replace(',', '.');
    const valorDecimal = parseFloat(valor);
    
    if (isNaN(valorDecimal) || valorDecimal <= 0 || valorDecimal > 100) {
        Notify.warning('El tipo de cambio debe ser un valor válido entre 0.01 y 100.');
        return;
    }

    try {
        const response = await fetch('/configuraciones/tipo-cambio/actualizar', {
            method: 'POST',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
            body: new URLSearchParams({ valor: valor })
        });

        const result = await response.json();
        Notify.info(result.message);
        if (result.success) location.reload();
    } catch (error) {
        Notify.error('Error al actualizar: ' + error.message);
    }
}

// Alertas de Stock Mínimo
async function toggleAlertasStockMinimo(habilitadas) {
    try {
        const response = await fetch('/configuraciones/alertas-stock-minimo/toggle', {
            method: 'POST',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
            body: new URLSearchParams({ habilitadas: habilitadas.toString() })
        });

        const result = await response.json();
        if (result.success) {
            Notify.success(result.message);
            const labelText = document.querySelector('#toggleAlertasStock').closest('.label').querySelector('.label-text');
            const estadoText = labelText.querySelector('.font-bold');
            const descText = labelText.querySelector('.text-sm');
            
            if (habilitadas) {
                estadoText.textContent = 'Activadas';
                descText.textContent = 'Las alertas se mostrarán cuando los productos lleguen a su stock mínimo';
            } else {
                estadoText.textContent = 'Desactivadas';
                descText.textContent = 'Las alertas están desactivadas y no se mostrarán';
            }
        } else {
            Notify.error(result.message);
            document.getElementById('toggleAlertasStock').checked = !habilitadas;
        }
    } catch (error) {
        Notify.error('Error al actualizar: ' + error.message);
        document.getElementById('toggleAlertasStock').checked = !habilitadas;
    }
}

// Bordes de Color en Motos del POS
async function toggleBordesColorMotosPOS(habilitadas) {
    try {
        const response = await fetch('/configuraciones/bordes-color-motos-pos/toggle', {
            method: 'POST',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
            body: new URLSearchParams({ habilitadas: habilitadas.toString() })
        });

        const result = await response.json();
        if (result.success) {
            Notify.success(result.message);
            const labelText = document.querySelector('#toggleBordesColorMotos').closest('.label').querySelector('.label-text');
            const estadoText = labelText.querySelector('.font-bold');
            const descText = labelText.querySelector('.text-sm');
            
            if (habilitadas) {
                estadoText.textContent = 'Activados';
                descText.textContent = 'Las motos con productos mostrarán bordes de color para indicar que tienen items pendientes';
            } else {
                estadoText.textContent = 'Desactivados';
                descText.textContent = 'Los bordes de color están desactivados y no se mostrarán en las motos';
            }
        } else {
            Notify.error(result.message);
            document.getElementById('toggleBordesColorMotos').checked = !habilitadas;
        }
    } catch (error) {
        Notify.error('Error al actualizar: ' + error.message);
        document.getElementById('toggleBordesColorMotos').checked = !habilitadas;
    }
}

// Sonidos de Notificación
async function toggleSonidosNotificacion(habilitadas) {
    try {
        const response = await fetch('/configuraciones/sonidos-notificacion/toggle', {
            method: 'POST',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
            body: new URLSearchParams({ habilitadas: habilitadas.toString() })
        });

        const result = await response.json();
        if (result.success) {
            Notify.success(result.message);
            const labelText = document.querySelector('#toggleSonidosNotificacion').closest('.label').querySelector('.label-text');
            const estadoText = labelText.querySelector('.font-bold');
            const descText = labelText.querySelector('.text-sm');
            
            if (habilitadas) {
                estadoText.textContent = 'Activados';
                descText.textContent = 'Se reproducirán sonidos al mostrar alertas de éxito, error y advertencias';
            } else {
                estadoText.textContent = 'Desactivados';
                descText.textContent = 'Los sonidos de notificación están silenciados';
            }
            
            // Guardar en localStorage para que los scripts lo lean
            localStorage.setItem('sonidosHabilitados', habilitadas.toString());
        } else {
            Notify.error(result.message);
            document.getElementById('toggleSonidosNotificacion').checked = !habilitadas;
        }
    } catch (error) {
        Notify.error('Error al actualizar: ' + error.message);
        document.getElementById('toggleSonidosNotificacion').checked = !habilitadas;
    }
}

// Hacer funciones globales
window.abrirModalEditarUsuario = abrirModalEditarUsuario;
window.crearUsuario = crearUsuario;
window.editarUsuario = editarUsuario;
window.eliminarUsuario = eliminarUsuario;
window.cambiarTema = cambiarTema;
window.actualizarTipoCambio = actualizarTipoCambio;
window.toggleAlertasStockMinimo = toggleAlertasStockMinimo;
window.toggleBordesColorMotosPOS = toggleBordesColorMotosPOS;
window.toggleSonidosNotificacion = toggleSonidosNotificacion;

