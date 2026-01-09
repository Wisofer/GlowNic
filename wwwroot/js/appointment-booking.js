/**
 * Sistema de agendamiento de citas para perfil público del salón
 */

(function() {
    'use strict';

    // Variables globales
    let selectedDate = null;
    let selectedTime = null;
    let slug = null;

    /**
     * Inicialización del sistema
     */
    function init(barberSlug) {
        slug = barberSlug;
        generateDateOptions();
        setupEventListeners();
    }

    /**
     * Configurar event listeners
     */
    function setupEventListeners() {
        // Búsqueda de servicios
        const searchInput = document.getElementById('serviceSearch');
        if (searchInput) {
            searchInput.addEventListener('input', filterServices);
        }
        
        // Checkboxes de servicios
        const checkboxes = document.querySelectorAll('.service-checkbox');
        checkboxes.forEach(checkbox => {
            checkbox.addEventListener('change', updateServiceSelection);
        });
        
        // Click en tarjetas de servicios
        document.querySelectorAll('.service-card').forEach(card => {
            card.addEventListener('click', function(e) {
                if (e.target.type !== 'checkbox' && e.target.tagName !== 'INPUT') {
                    const checkbox = this.querySelector('.service-checkbox');
                    checkbox.checked = !checkbox.checked;
                    updateServiceSelection();
                }
            });
        });

        // Formulario de cita
        const appointmentForm = document.getElementById('appointmentForm');
        if (appointmentForm) {
            appointmentForm.addEventListener('submit', handleFormSubmit);
        }

        // Cerrar modales al hacer clic fuera
        const successModal = document.getElementById('successModal');
        const errorModal = document.getElementById('errorModal');
        if (successModal) {
            successModal.addEventListener('click', function(e) {
                if (e.target === this) closeSuccessModal();
            });
        }
        if (errorModal) {
            errorModal.addEventListener('click', function(e) {
                if (e.target === this) closeErrorModal();
            });
        }
    }

    /**
     * Actualizar selección de servicios y calcular total
     */
    function updateServiceSelection() {
        const checkboxes = document.querySelectorAll('.service-checkbox');
        const totalDiv = document.getElementById('serviceTotal');
        let totalPrice = 0;
        let totalDuration = 0;
        let selectedCount = 0;
        
        checkboxes.forEach(checkbox => {
            const card = checkbox.closest('.service-card');
            if (checkbox.checked) {
                card.classList.add('selected');
                const price = parseFloat(card.dataset.servicePrice);
                const duration = parseInt(card.dataset.serviceDuration);
                totalPrice += price;
                totalDuration += duration;
                selectedCount++;
            } else {
                card.classList.remove('selected');
            }
        });
        
        // Mostrar/ocultar total
        if (totalDiv) {
            if (selectedCount > 0) {
                totalDiv.style.display = 'block';
                document.getElementById('totalAmount').textContent = `C$${totalPrice.toFixed(2)}`;
                document.getElementById('totalDuration').textContent = `${totalDuration} min`;
            } else {
                totalDiv.style.display = 'none';
            }
        }
    }

    /**
     * Filtrar servicios por búsqueda
     */
    function filterServices() {
        const searchInput = document.getElementById('serviceSearch');
        if (!searchInput) return;
        
        const searchTerm = searchInput.value.toLowerCase().trim();
        const serviceCards = document.querySelectorAll('.service-card');
        
        let visibleCount = 0;
        serviceCards.forEach(card => {
            const serviceName = card.dataset.serviceName || '';
            if (serviceName.includes(searchTerm)) {
                card.style.display = 'flex';
                visibleCount++;
            } else {
                card.style.display = 'none';
            }
        });
        
        // Mostrar mensaje si no hay resultados
        const container = document.getElementById('servicesContainer');
        if (!container) return;
        
        let noResults = container.querySelector('.no-results');
        if (visibleCount === 0 && searchTerm.length > 0) {
            if (!noResults) {
                noResults = document.createElement('div');
                noResults.className = 'no-results';
                noResults.style.cssText = 'text-align: center; padding: 40px 20px; color: #6c757d;';
                noResults.innerHTML = `
                    <div style="font-size: 18px; font-weight: 600; color: #495057; margin-bottom: 8px;">No se encontraron servicios</div>
                    <div style="font-size: 14px;">Intenta con otro término de búsqueda</div>
                `;
                container.appendChild(noResults);
            }
            noResults.style.display = 'block';
        } else if (noResults) {
            noResults.style.display = 'none';
        }
    }

    /**
     * Generar opciones de fecha (próximos 7 días)
     */
    function generateDateOptions() {
        const container = document.getElementById('dateSelector');
        if (!container) return;

        const days = ['Dom', 'Lun', 'Mar', 'Mié', 'Jue', 'Vie', 'Sáb'];
        const months = ['Ene', 'Feb', 'Mar', 'Abr', 'May', 'Jun', 'Jul', 'Ago', 'Sep', 'Oct', 'Nov', 'Dic'];
        
        container.innerHTML = '';
        
        // Crear array de fechas ordenadas (próximos 7 días)
        const dates = [];
        for (let i = 0; i < 7; i++) {
            const date = new Date();
            date.setDate(date.getDate() + i);
            dates.push(date);
        }
        
        dates.forEach((date, index) => {
            const dayName = days[date.getDay()];
            const dayNumber = date.getDate();
            const month = months[date.getMonth()];
            const dateStr = date.toISOString().split('T')[0];
            
            const option = document.createElement('div');
            option.className = 'date-option' + (index === 0 ? ' selected' : '');
            option.dataset.date = dateStr;
            
            option.innerHTML = `
                <div class="date-day">${dayName}</div>
                <div class="date-number">${dayNumber}</div>
                <div class="date-month">${month}</div>
            `;
            
            option.onclick = () => selectDate(dateStr, option);
            container.appendChild(option);
        });
        
        // Seleccionar fecha de hoy automáticamente
        if (container.children.length > 0) {
            const todayStr = new Date().toISOString().split('T')[0];
            selectDate(todayStr, container.children[0]);
        }
    }

    /**
     * Seleccionar fecha
     */
    function selectDate(dateStr, element) {
        // Remover selección anterior
        document.querySelectorAll('.date-option').forEach(opt => {
            opt.classList.remove('selected');
        });
        
        // Seleccionar nueva fecha
        element.classList.add('selected');
        selectedDate = dateStr;
        selectedTime = null;
        
        const submitBtn = document.getElementById('submitBtn');
        if (submitBtn) {
            submitBtn.disabled = true;
        }
        
        // Cargar horarios disponibles
        loadAvailability(dateStr);
    }

    /**
     * Cargar horarios disponibles para una fecha
     */
    async function loadAvailability(dateStr) {
        const timeSlotsContainer = document.getElementById('timeSlots');
        if (!timeSlotsContainer) return;

        timeSlotsContainer.innerHTML = '<div class="loading"><div class="spinner"></div><p>Cargando horarios...</p></div>';
        
        try {
            const response = await fetch(`/b/${slug}/availability?date=${dateStr}`);
            
            if (!response.ok) {
                throw new Error('Error al obtener disponibilidad');
            }
            
            const data = await response.json();
            
            if (data.error) {
                timeSlotsContainer.innerHTML = `
                    <div class="no-availability">
                        <div class="no-availability-title">No disponible</div>
                        <div class="no-availability-message">${data.error}</div>
                    </div>
                `;
                return;
            }
            
            if (data.availableSlots && data.availableSlots.length > 0) {
                const availableOnly = data.availableSlots.filter(slot => slot.isAvailable);
                
                if (availableOnly.length > 0) {
                    timeSlotsContainer.innerHTML = '';
                    availableOnly.forEach(slot => {
                        const timeStr = slot.startTime.substring(0, 5);
                        
                        const button = document.createElement('div');
                        button.className = 'time-slot';
                        button.textContent = timeStr;
                        button.dataset.time = slot.startTime;
                        
                        button.onclick = function() {
                            document.querySelectorAll('.time-slot').forEach(btn => {
                                btn.classList.remove('selected');
                            });
                            this.classList.add('selected');
                            selectedTime = this.dataset.time;
                            
                            const submitBtn = document.getElementById('submitBtn');
                            if (submitBtn) {
                                submitBtn.disabled = false;
                            }
                        };
                        
                        timeSlotsContainer.appendChild(button);
                    });
                } else {
                    showNoAvailability(timeSlotsContainer);
                }
            } else {
                showNoAvailability(timeSlotsContainer);
            }
        } catch (error) {
            console.error('Error:', error);
            timeSlotsContainer.innerHTML = `
                <div class="no-availability">
                    <div class="no-availability-title">Error al cargar</div>
                    <div class="no-availability-message">Por favor intenta más tarde</div>
                </div>
            `;
        }
    }

    /**
     * Mostrar mensaje de no disponibilidad
     */
    function showNoAvailability(container) {
        container.innerHTML = `
            <div class="no-availability">
                <div class="no-availability-title">No hay horarios disponibles</div>
                <div class="no-availability-message">El salón no trabaja este día</div>
            </div>
        `;
    }

    /**
     * Manejar envío del formulario
     */
    async function handleFormSubmit(e) {
        e.preventDefault();
        
        // Obtener datos del formulario
        const selectedServices = Array.from(document.querySelectorAll('.service-checkbox:checked'))
            .map(cb => parseInt(cb.value));
        const clientName = document.getElementById('clientName').value;
        const clientPhone = document.getElementById('clientPhone').value;
        
        if (!clientName || !clientPhone || !selectedDate || !selectedTime) {
            showError('Por favor completa todos los campos y selecciona un horario');
            return;
        }
        
        const submitBtn = document.getElementById('submitBtn');
        const submitText = document.getElementById('submitText');
        const submitLoading = document.getElementById('submitLoading');
        
        if (submitBtn) submitBtn.disabled = true;
        if (submitText) submitText.textContent = 'Agendando...';
        if (submitLoading) submitLoading.style.display = 'inline-block';
        
        try {
            let timeOnly = selectedTime;
            if (!timeOnly.includes(':')) {
                timeOnly = `${timeOnly}:00`;
            } else if (timeOnly.split(':').length === 2) {
                timeOnly = `${timeOnly}:00`;
            }
            
            const response = await fetch(`/b/${slug}/appointment`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    barberSlug: slug,
                    serviceIds: selectedServices.length > 0 ? selectedServices : null,
                    clientName: clientName,
                    clientPhone: clientPhone,
                    date: selectedDate,
                    time: timeOnly
                })
            });
            
            const result = await response.json();
            
            if (result.success) {
                playSound('success');
                
                const successMessage = document.getElementById('successMessage');
                const successModal = document.getElementById('successModal');
                if (successMessage) {
                    successMessage.textContent = result.message || 'Tu cita ha sido agendada exitosamente. Te contactaremos pronto.';
                }
                if (successModal) {
                    successModal.classList.add('show');
                }
                
                clearAppointmentForm();
            } else {
                playSound('error');
                showError(result.message || 'Error al agendar la cita');
            }
        } catch (error) {
            console.error('Error:', error);
            showError('Error al agendar la cita. Por favor intenta nuevamente.');
        } finally {
            if (submitBtn) submitBtn.disabled = false;
            if (submitText) submitText.textContent = 'Agendar Cita';
            if (submitLoading) submitLoading.style.display = 'none';
        }
    }

    /**
     * Mostrar error
     */
    function showError(message) {
        playSound('error');
        const errorMessage = document.getElementById('errorMessage');
        const errorModal = document.getElementById('errorModal');
        if (errorMessage) {
            errorMessage.textContent = message;
        }
        if (errorModal) {
            errorModal.classList.add('show');
        }
    }

    /**
     * Reproducir sonidos
     */
    function playSound(type) {
        try {
            const audio = new Audio(`/sounds/${type}.mp3`);
            audio.volume = 0.5; // Volumen al 50%
            audio.play().catch(err => {
                console.log('No se pudo reproducir el sonido:', err);
            });
        } catch (error) {
            console.log('Error al reproducir sonido:', error);
        }
    }

    /**
     * Limpiar completamente el formulario
     */
    function clearAppointmentForm() {
        // Limpiar formulario básico
        const appointmentForm = document.getElementById('appointmentForm');
        if (appointmentForm) {
            appointmentForm.reset();
        }
        
        // Deseleccionar todos los servicios
        const checkboxes = document.querySelectorAll('.service-checkbox');
        checkboxes.forEach(checkbox => {
            checkbox.checked = false;
            const card = checkbox.closest('.service-card');
            if (card) {
                card.classList.remove('selected');
            }
        });
        
        // Limpiar búsqueda de servicios
        const serviceSearch = document.getElementById('serviceSearch');
        if (serviceSearch) {
            serviceSearch.value = '';
            filterServices();
        }
        
        // Ocultar y resetear el total
        const totalDiv = document.getElementById('serviceTotal');
        if (totalDiv) {
            totalDiv.style.display = 'none';
            const totalAmount = document.getElementById('totalAmount');
            const totalDuration = document.getElementById('totalDuration');
            if (totalAmount) totalAmount.textContent = 'C$0.00';
            if (totalDuration) totalDuration.textContent = '0 min';
        }
        
        // Limpiar selección de fecha y hora
        selectedDate = null;
        selectedTime = null;
        
        // Regenerar opciones de fecha
        generateDateOptions();
        
        // Deshabilitar botón de envío
        const submitBtn = document.getElementById('submitBtn');
        if (submitBtn) {
            submitBtn.disabled = true;
        }
    }

    /**
     * Cerrar modal de éxito
     */
    function closeSuccessModal() {
        const successModal = document.getElementById('successModal');
        if (successModal) {
            successModal.classList.remove('show');
        }
    }

    /**
     * Cerrar modal de error
     */
    function closeErrorModal() {
        const errorModal = document.getElementById('errorModal');
        if (errorModal) {
            errorModal.classList.remove('show');
        }
    }

    // Exponer funciones globales necesarias
    window.AppointmentBooking = {
        init: init,
        closeSuccessModal: closeSuccessModal,
        closeErrorModal: closeErrorModal
    };

    // Auto-inicializar cuando el DOM esté listo
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', function() {
            const slugElement = document.querySelector('[data-barber-slug]');
            if (slugElement) {
                init(slugElement.dataset.barberSlug);
            }
        });
    } else {
        const slugElement = document.querySelector('[data-barber-slug]');
        if (slugElement) {
            init(slugElement.dataset.barberSlug);
        }
    }

})();

