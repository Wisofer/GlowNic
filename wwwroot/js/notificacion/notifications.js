// Sistema de gestión de notificaciones push
let templates = [];
let salons = [];
let selectedSalonIds = [];

// Inicializar cuando se carga la página
document.addEventListener('DOMContentLoaded', function() {
    loadTemplates();
    loadSalons();
    setupFormHandlers();
});

// Cambiar entre tabs
function showTab(tabName) {
    // Ocultar todos los tabs
    document.querySelectorAll('.tab-content').forEach(tab => {
        tab.classList.add('hidden');
    });
    
    // Remover active de todos los tabs
    document.querySelectorAll('.tab').forEach(tab => {
        tab.classList.remove('tab-active');
    });
    
    // Mostrar el tab seleccionado
    document.getElementById(`tab-${tabName}`).classList.remove('hidden');
    
    // Activar el tab button
    event.target.classList.add('tab-active');
    
    // Cargar datos según el tab
    if (tabName === 'logs') {
        loadLogs();
    }
}

// Cargar plantillas
async function loadTemplates() {
    try {
        const response = await fetch('/admin/notifications/templates');
        if (response.ok) {
            templates = await response.json();
            renderTemplates();
            populateTemplateSelect();
        } else {
            const error = await response.json();
            console.error('Error al cargar plantillas:', error);
            showNotification(error.message || 'Error al cargar plantillas', 'error');
        }
    } catch (error) {
        console.error('Error al cargar plantillas:', error);
        showNotification('Error al cargar plantillas', 'error');
    }
}

// Renderizar plantillas
function renderTemplates() {
    const container = document.getElementById('templates-list');
    if (templates.length === 0) {
        container.innerHTML = '<p class="text-gray-400 text-center py-4">No hay plantillas creadas</p>';
        return;
    }
    
    container.innerHTML = templates.map(template => `
        <div class="bg-base-100 rounded-lg p-3 border border-base-300">
            <div class="flex justify-between items-start">
                <div class="flex-1">
                    <h3 class="font-semibold">${escapeHtml(template.title)}</h3>
                    <p class="text-sm text-gray-400 mt-1">${escapeHtml(template.body)}</p>
                    ${template.name ? `<p class="text-xs text-gray-500 mt-1">📝 ${escapeHtml(template.name)}</p>` : ''}
                </div>
                <div class="flex gap-2">
                    <button class="btn btn-sm btn-ghost" onclick="editTemplate(${template.id})">
                        ✏️ Editar
                    </button>
                    <button class="btn btn-sm btn-error" onclick="deleteTemplate(${template.id})">
                        🗑️ Eliminar
                    </button>
                </div>
            </div>
        </div>
    `).join('');
}

// Cargar salones
async function loadSalons() {
    try {
        const response = await fetch('/admin/notifications/salons');
        if (response.ok) {
            salons = await response.json();
            renderSalonsTable();
        }
    } catch (error) {
        console.error('Error al cargar salones:', error);
    }
}

// Renderizar tabla de salones
function renderSalonsTable() {
    const tbody = document.getElementById('salons-table-body');
    if (salons.length === 0) {
        tbody.innerHTML = '<tr><td colspan="3" class="text-center text-gray-400 py-4">No hay salones con dispositivos registrados. Los salones necesitan registrar al menos un dispositivo con token FCM para recibir notificaciones.</td></tr>';
        return;
    }
    
    tbody.innerHTML = salons.map(salon => `
        <tr>
            <td>
                <input type="checkbox" class="checkbox checkbox-primary checkbox-sm salon-checkbox" 
                       value="${salon.id}" onchange="updateSelectedSalons()">
            </td>
            <td class="font-medium">${escapeHtml(salon.name || 'Sin nombre')}</td>
            <td class="text-sm text-gray-500">${escapeHtml(salon.businessName || 'Sin nombre de negocio')} ${salon.deviceCount ? `<span class="badge badge-success badge-xs ml-1">${salon.deviceCount} dispositivo${salon.deviceCount > 1 ? 's' : ''}</span>` : ''}</td>
        </tr>
    `).join('');
    
    updateSelectedCount();
}

// Seleccionar/Deseleccionar todos
function toggleSelectAll() {
    const selectAllCheckbox = document.getElementById('select-all-checkbox');
    const selectAllLabel = document.getElementById('select-all-salons');
    
    // Determinar el estado: si el que se clickeó está checked, marcar todos; si no, desmarcar todos
    let isChecked;
    if (event.target.id === 'select-all-checkbox') {
        isChecked = selectAllCheckbox.checked;
    } else if (event.target.id === 'select-all-salons') {
        isChecked = selectAllLabel.checked;
    } else {
        // Si se llamó desde otro lugar, usar el estado del checkbox principal
        isChecked = selectAllCheckbox.checked;
    }
    
    // Sincronizar ambos checkboxes
    selectAllCheckbox.checked = isChecked;
    selectAllLabel.checked = isChecked;
    selectAllCheckbox.indeterminate = false; // Limpiar estado indeterminado
    
    // Seleccionar/deseleccionar todos los checkboxes de salones
    document.querySelectorAll('.salon-checkbox').forEach(checkbox => {
        checkbox.checked = isChecked;
    });
    
    updateSelectedSalons();
}

// Actualizar salones seleccionados
function updateSelectedSalons() {
    const checkboxes = document.querySelectorAll('.salon-checkbox:checked');
    selectedSalonIds = Array.from(checkboxes).map(cb => parseInt(cb.value));
    updateSelectedCount();
    
    // Actualizar estado de "Seleccionar todo"
    const totalCheckboxes = document.querySelectorAll('.salon-checkbox').length;
    const checkedCount = checkboxes.length;
    const selectAllCheckbox = document.getElementById('select-all-checkbox');
    const selectAllLabel = document.getElementById('select-all-salons');
    
    if (checkedCount === 0) {
        selectAllCheckbox.checked = false;
        selectAllCheckbox.indeterminate = false;
        selectAllLabel.checked = false;
    } else if (checkedCount === totalCheckboxes) {
        selectAllCheckbox.checked = true;
        selectAllCheckbox.indeterminate = false;
        selectAllLabel.checked = true;
    } else {
        // Estado parcial: algunos seleccionados
        selectAllCheckbox.checked = false;
        selectAllCheckbox.indeterminate = true;
        selectAllLabel.checked = false;
    }
}

// Actualizar contador de seleccionados
function updateSelectedCount() {
    const count = selectedSalonIds.length;
    const countElement = document.getElementById('selected-count');
    countElement.textContent = `${count} salón${count !== 1 ? 'es' : ''} seleccionado${count !== 1 ? 's' : ''}`;
}

// Configurar handlers de formularios
function setupFormHandlers() {
    // Formulario de plantilla
    document.getElementById('template-form').addEventListener('submit', async (e) => {
        e.preventDefault();
        await saveTemplate();
    });
    
    // Formulario de envío
    document.getElementById('send-notification-form').addEventListener('submit', async (e) => {
        e.preventDefault();
        await sendNotification();
    });
}

// Abrir modal de crear plantilla
function openCreateTemplateModal() {
    document.getElementById('modal-title').textContent = 'Nueva Plantilla';
    document.getElementById('template-form').reset();
    document.getElementById('template-id').value = '';
    document.getElementById('template-modal').classList.add('modal-open');
}

// Cerrar modal
function closeTemplateModal() {
    document.getElementById('template-modal').classList.remove('modal-open');
}

// Editar plantilla
function editTemplate(id) {
    const template = templates.find(t => t.id === id);
    if (!template) return;
    
    document.getElementById('modal-title').textContent = 'Editar Plantilla';
    document.getElementById('template-id').value = template.id;
    document.getElementById('template-name').value = template.name || '';
    document.getElementById('template-title').value = template.title;
    document.getElementById('template-body').value = template.body;
    document.getElementById('template-image').value = template.imageUrl || '';
    document.getElementById('template-modal').classList.add('modal-open');
}

// Guardar plantilla
async function saveTemplate() {
    const id = document.getElementById('template-id').value;
    const data = {
        name: document.getElementById('template-name').value,
        title: document.getElementById('template-title').value,
        body: document.getElementById('template-body').value,
        imageUrl: document.getElementById('template-image').value || null
    };
    
    try {
        const url = id ? `/admin/notifications/templates/${id}` : '/admin/notifications/templates';
        const method = id ? 'PUT' : 'POST';
        
        const response = await fetch(url, {
            method: method,
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(data)
        });
        
        const result = await response.json();
        
        if (response.ok) {
            showNotification(id ? 'Plantilla actualizada' : 'Plantilla creada', 'success');
            closeTemplateModal();
            loadTemplates();
        } else {
            showNotification(result.message || 'Error al guardar plantilla', 'error');
        }
    } catch (error) {
        console.error('Error:', error);
        showNotification('Error al guardar plantilla', 'error');
    }
}

// Eliminar plantilla
async function deleteTemplate(id) {
    if (!confirm('¿Estás segura de eliminar esta plantilla?')) return;
    
    try {
        const response = await fetch(`/admin/notifications/templates/${id}`, {
            method: 'DELETE'
        });
        
        const result = await response.json();
        
        if (response.ok && result.success) {
            showNotification('Plantilla eliminada', 'success');
            loadTemplates();
        } else {
            showNotification(result.message || 'Error al eliminar plantilla', 'error');
        }
    } catch (error) {
        console.error('Error:', error);
        showNotification('Error al eliminar plantilla', 'error');
    }
}

// Poblar select de plantillas
function populateTemplateSelect() {
    const select = document.getElementById('template-select');
    select.innerHTML = '<option value="">-- Seleccionar plantilla --</option>' +
        templates.map(t => `<option value="${t.id}">${escapeHtml(t.title)}</option>`).join('');
}

// Cargar vista previa de plantilla
function loadTemplatePreview() {
    const templateId = document.getElementById('template-select').value;
    const previewDiv = document.getElementById('template-preview');
    
    if (!templateId) {
        previewDiv.classList.add('hidden');
        return;
    }
    
    const template = templates.find(t => t.id == templateId);
    if (!template) {
        previewDiv.classList.add('hidden');
        return;
    }
    
    // Mostrar vista previa
    document.getElementById('preview-title').textContent = template.title;
    document.getElementById('preview-body').textContent = template.body;
    
    // Mostrar imagen si existe
    const imageContainer = document.getElementById('preview-image-container');
    const previewImage = document.getElementById('preview-image');
    if (template.imageUrl) {
        previewImage.src = template.imageUrl;
        imageContainer.classList.remove('hidden');
    } else {
        imageContainer.classList.add('hidden');
    }
    
    previewDiv.classList.remove('hidden');
}

// Enviar notificación
async function sendNotification() {
    const templateId = document.getElementById('template-select').value;
    
    // Validaciones
    if (!templateId) {
        showNotification('Debes seleccionar una plantilla', 'error');
        return;
    }
    
    if (selectedSalonIds.length === 0) {
        showNotification('Debes seleccionar al menos un destinatario', 'error');
        return;
    }
    
    const template = templates.find(t => t.id == templateId);
    if (!template) {
        showNotification('Plantilla no encontrada', 'error');
        return;
    }
    
    // Enviar los IDs de salones seleccionados
    // El backend los convertirá a UserIds automáticamente
    const data = {
        templateId: parseInt(templateId),
        userIds: selectedSalonIds, // IDs de salones (BarberId), el backend los convierte a UserIds
        extraData: {},
        dataOnly: false
    };
    
    try {
        const btnSend = document.getElementById('btn-send-notification');
        btnSend.disabled = true;
        btnSend.textContent = '⏳ Enviando...';
        
        const response = await fetch('/admin/notifications/send', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(data)
        });
        
        const result = await response.json();
        
        if (response.ok && result.success) {
            showNotification(
                `✅ Notificación enviada a ${result.userCount} usuario(s)`, 
                'success'
            );
            // Limpiar selección
            document.getElementById('template-select').value = '';
            document.getElementById('template-preview').classList.add('hidden');
            document.querySelectorAll('.salon-checkbox').forEach(cb => cb.checked = false);
            document.getElementById('select-all-checkbox').checked = false;
            document.getElementById('select-all-salons').checked = false;
            updateSelectedSalons();
        } else {
            showNotification(result.message || 'Error al enviar notificación', 'error');
        }
    } catch (error) {
        console.error('Error:', error);
        showNotification('Error al enviar notificación', 'error');
    } finally {
        const btnSend = document.getElementById('btn-send-notification');
        btnSend.disabled = false;
        btnSend.textContent = '📤 Enviar Notificación';
    }
}

// Cargar logs
async function loadLogs() {
    try {
        const response = await fetch('/admin/notifications/logs?page=1&pageSize=50');
        if (response.ok) {
            const logs = await response.json();
            renderLogs(logs);
        }
    } catch (error) {
        console.error('Error al cargar logs:', error);
    }
}

// Renderizar logs
function renderLogs(logs) {
    const container = document.getElementById('logs-list');
    if (logs.length === 0) {
        container.innerHTML = '<p class="text-gray-400 text-center py-4">No hay logs disponibles</p>';
        return;
    }
    
    container.innerHTML = logs.map(log => `
        <div class="bg-base-100 rounded-lg p-3 border border-base-300">
            <div class="flex justify-between items-start">
                <div class="flex-1">
                    <div class="flex items-center gap-2">
                        <span class="badge ${log.status === 'sent' ? 'badge-success' : 'badge-error'}">
                            ${log.status}
                        </span>
                        <span class="text-xs text-gray-400">
                            ${new Date(log.sentAt).toLocaleString()}
                        </span>
                    </div>
                    ${log.payload ? `<p class="text-sm mt-2 text-gray-300">${escapeHtml(log.payload.substring(0, 100))}...</p>` : ''}
                </div>
            </div>
        </div>
    `).join('');
}

// Utilidades
function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

function showNotification(message, type = 'info') {
    // Usar el sistema de notificaciones global si existe
    if (window.NotificationSystem) {
        window.NotificationSystem.show(message, type);
    } else {
        alert(message);
    }
}
