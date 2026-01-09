/**
 * Sistema de Registro de Pagos - JavaScript Modular
 * Organizado por funcionalidades
 */

// ============================================
// CONFIGURACIÓN GLOBAL
// ============================================
const PagoSystem = {
    facturasCargadas: [],
    tipoCambio: 36.80,
    clienteSeleccionado: null,
    
    init() {
        this.setupEventListeners();
        this.inicializarCampos();
    },
    
    setupEventListeners() {
        // Inicializar validación para todos los inputs numéricos
        Utilidades.inicializarValidacionInputsNumericos();
        
        // Event listeners para campos de moneda (después de la validación)
        ['MontoCordobasFisico', 'MontoDolaresFisico', 'MontoCordobasElectronico', 'MontoDolaresElectronico'].forEach(id => {
            const campo = document.getElementById(id);
            if (campo) {
                campo.addEventListener('input', () => {
                    // Si es modo Mixto y se cambió un campo físico, calcular automáticamente el electrónico
                    const tipoPago = document.querySelector('input[name="TipoPago"]:checked')?.value;
                    if (tipoPago === 'Mixto' && (id === 'MontoCordobasFisico' || id === 'MontoDolaresFisico')) {
                        CamposPagoManager.calcularMontoElectronicoAutomatico();
                    }
                    
                    // Usar directamente PagoManager para evitar problemas de contexto
                    PagoManager.calcularTotalPago();
                    if (id.includes('Fisico')) {
                        PagoManager.calcularVueltoFisico();
                    }
                });
            }
        });
        
        // Event listeners para monto recibido (ya está incluido en inicializarValidacionInputsNumericos, pero agregamos cálculo)
        const montoRecibidoFisico = document.getElementById('MontoRecibidoFisico');
        if (montoRecibidoFisico) {
            montoRecibidoFisico.addEventListener('input', () => {
                PagoManager.calcularVueltoFisico();
            });
        }
        
        // Event listeners para campos de recibido en ambas monedas (cuando es "Ambos")
        const montoRecibidoCordobasFisico = document.getElementById('MontoRecibidoCordobasFisico');
        if (montoRecibidoCordobasFisico) {
            montoRecibidoCordobasFisico.addEventListener('input', () => {
                // Si es "Ambos", calcular el faltante y prellenar Monto ($)
                const moneda = document.getElementById('Moneda')?.value;
                const tipoPago = document.querySelector('input[name="TipoPago"]:checked')?.value;
                if (moneda === 'Ambos' && tipoPago === 'Fisico') {
                    PagoManager.calcularMontoFaltanteDesdeRecibido();
                }
                PagoManager.calcularVueltoFisico();
            });
        }
        
        const montoRecibidoDolaresFisico = document.getElementById('MontoRecibidoDolaresFisico');
        if (montoRecibidoDolaresFisico) {
            montoRecibidoDolaresFisico.addEventListener('input', () => {
                // Si es "Ambos", calcular el faltante y prellenar Monto (C$)
                const moneda = document.getElementById('Moneda')?.value;
                const tipoPago = document.querySelector('input[name="TipoPago"]:checked')?.value;
                if (moneda === 'Ambos' && tipoPago === 'Fisico') {
                    PagoManager.calcularMontoFaltanteDesdeRecibido();
                }
                PagoManager.calcularVueltoFisico();
            });
        }
    },
    
    inicializarCampos() {
        if (window.mostrarCamposPago) {
            window.mostrarCamposPago();
        }
    }
};

// ============================================
// GESTIÓN DE CLIENTES
// ============================================
const ClienteManager = {
    todosLosClientes: [],
    clienteSeleccionado: null,
    selectedIndex: -1,
    
    init(clientesJson) {
        this.todosLosClientes = clientesJson;
        this.setupEventListeners();
    },
    
    setupEventListeners() {
        const clienteSearch = document.getElementById('clienteSearch');
        const clearClienteBtn = document.getElementById('clearClienteBtn');
        
        if (!clienteSearch) return;
        
        clienteSearch.addEventListener('input', (e) => this.buscarCliente(e.target.value));
        clienteSearch.addEventListener('focus', () => {
            if (clienteSearch.value.trim() && !this.clienteSeleccionado) {
                this.buscarCliente(clienteSearch.value);
            }
        });
        clienteSearch.addEventListener('keydown', (e) => this.manejarTeclado(e));
        
        if (clearClienteBtn) {
            clearClienteBtn.addEventListener('click', () => this.limpiarSeleccion());
        }
        
        // Cerrar dropdown al hacer click fuera
        document.addEventListener('click', (e) => {
            const clienteDropdown = document.getElementById('clienteDropdown');
            if (!clienteSearch.contains(e.target) && !clienteDropdown?.contains(e.target)) {
                clienteDropdown?.classList.add('hidden');
            }
        });
    },
    
    buscarCliente(termino) {
        if (!termino || termino.trim() === '') {
            document.getElementById('clienteDropdown')?.classList.add('hidden');
            return;
        }
        
        const clientesFiltrados = this.filtrarClientes(termino);
        this.mostrarResultados(clientesFiltrados);
    },
    
    filtrarClientes(termino) {
        const terminoLower = termino.toLowerCase().trim();
        return this.todosLosClientes.filter(cliente => {
            const codigo = (cliente.codigo || '').toLowerCase();
            const nombre = (cliente.nombre || '').toLowerCase();
            return codigo.includes(terminoLower) || nombre.includes(terminoLower);
        });
    },
    
    mostrarResultados(clientesFiltrados) {
        const clienteDropdownContent = document.getElementById('clienteDropdownContent');
        const clienteDropdown = document.getElementById('clienteDropdown');
        
        if (!clienteDropdownContent || !clienteDropdown) return;
        
        clienteDropdownContent.innerHTML = '';
        
        if (clientesFiltrados.length === 0) {
            const noResults = document.createElement('div');
            noResults.className = 'px-4 py-3 text-sm text-gray-500 dark:text-gray-400 text-center';
            noResults.textContent = 'No se encontraron clientes';
            clienteDropdownContent.appendChild(noResults);
            clienteDropdown.classList.remove('hidden');
            return;
        }
        
        clientesFiltrados.forEach((cliente, index) => {
            const item = document.createElement('div');
            item.className = 'px-4 py-2 hover:bg-gray-100 dark:hover:bg-gray-700 cursor-pointer transition-colors cliente-item';
            item.dataset.clienteId = cliente.id;
            item.dataset.index = index;
            
            const codigo = document.createElement('span');
            codigo.className = 'font-medium text-gray-900 dark:text-white';
            codigo.textContent = cliente.codigo + ' - ';
            
            const nombre = document.createElement('span');
            nombre.className = 'text-gray-700 dark:text-gray-300';
            nombre.textContent = cliente.nombre;
            
            item.appendChild(codigo);
            item.appendChild(nombre);
            
            item.addEventListener('click', () => this.seleccionarCliente(cliente));
            item.addEventListener('mouseenter', () => {
                clienteDropdownContent.querySelectorAll('.cliente-item').forEach(el => {
                    el.classList.remove('bg-blue-50', 'dark:bg-blue-900');
                });
                item.classList.add('bg-blue-50', 'dark:bg-blue-900');
                this.selectedIndex = index;
            });
            
            clienteDropdownContent.appendChild(item);
        });
        
        clienteDropdown.classList.remove('hidden');
        this.selectedIndex = -1;
    },
    
    seleccionarCliente(cliente) {
        this.clienteSeleccionado = cliente;
        document.getElementById('clienteId').value = cliente.id;
        document.getElementById('clienteSearch').value = cliente.codigo + ' - ' + cliente.nombre;
        document.getElementById('clienteDropdown')?.classList.add('hidden');
        document.getElementById('clearClienteBtn')?.classList.remove('hidden');
        
        if (window.cargarFacturas) {
            window.cargarFacturas(cliente.id);
        }
    },
    
    limpiarSeleccion() {
        this.clienteSeleccionado = null;
        document.getElementById('clienteId').value = '';
        document.getElementById('clienteSearch').value = '';
        document.getElementById('clienteDropdown')?.classList.add('hidden');
        document.getElementById('clearClienteBtn')?.classList.add('hidden');
        FacturaManager.limpiarFacturas();
    },
    
    manejarTeclado(e) {
        const items = document.querySelectorAll('.cliente-item');
        if (items.length === 0) return;
        
        if (e.key === 'ArrowDown') {
            e.preventDefault();
            this.selectedIndex = (this.selectedIndex + 1) % items.length;
            items[this.selectedIndex].scrollIntoView({ block: 'nearest', behavior: 'smooth' });
            items[this.selectedIndex].classList.add('bg-blue-50', 'dark:bg-blue-900');
            items.forEach((item, idx) => {
                if (idx !== this.selectedIndex) {
                    item.classList.remove('bg-blue-50', 'dark:bg-blue-900');
                }
            });
        } else if (e.key === 'ArrowUp') {
            e.preventDefault();
            this.selectedIndex = this.selectedIndex <= 0 ? items.length - 1 : this.selectedIndex - 1;
            items[this.selectedIndex].scrollIntoView({ block: 'nearest', behavior: 'smooth' });
            items[this.selectedIndex].classList.add('bg-blue-50', 'dark:bg-blue-900');
            items.forEach((item, idx) => {
                if (idx !== this.selectedIndex) {
                    item.classList.remove('bg-blue-50', 'dark:bg-blue-900');
                }
            });
        } else if (e.key === 'Enter') {
            e.preventDefault();
            if (this.selectedIndex >= 0 && items[this.selectedIndex]) {
                const clienteId = items[this.selectedIndex].dataset.clienteId;
                const cliente = this.todosLosClientes.find(c => c.id == clienteId);
                if (cliente) {
                    this.seleccionarCliente(cliente);
                }
            }
        } else if (e.key === 'Escape') {
            document.getElementById('clienteDropdown')?.classList.add('hidden');
        }
    }
};

// ============================================
// GESTIÓN DE FACTURAS
// ============================================
const FacturaManager = {
    facturasCargadas: [],
    
    cargarFacturas(clienteId) {
        if (!clienteId) {
            this.limpiarFacturas();
            return;
        }
        
        fetch(`/pagos/facturas-cliente/${clienteId}`)
            .then(response => response.json())
            .then(data => this.procesarFacturas(data))
            .catch(error => {
                console.error('Error al cargar facturas:', error);
                document.getElementById('sinFacturasMensaje').textContent = 'Error al cargar las facturas';
                document.getElementById('sinFacturasMensaje').classList.remove('hidden');
            });
    },
    
    procesarFacturas(data) {
        const facturasList = document.getElementById('facturasList');
        const sinFacturasMensaje = document.getElementById('sinFacturasMensaje');
        const facturasContainer = document.getElementById('facturasContainer');
        
        facturasList.innerHTML = '';
        this.facturasCargadas = [];
        
        const facturasConSaldo = data.filter(f => f.puedePagar);
        
        if (facturasConSaldo.length === 0) {
            facturasList.classList.add('hidden');
            sinFacturasMensaje.classList.remove('hidden');
            sinFacturasMensaje.textContent = 'No hay facturas con saldo pendiente para este cliente';
            PagoManager.actualizarMonto();
            return;
        }
        
        sinFacturasMensaje.classList.add('hidden');
        facturasList.classList.remove('hidden');
        
        // Crear header con botón "Seleccionar todas"
        const headerDiv = document.createElement('div');
        headerDiv.className = 'flex items-center justify-between mb-2 pb-2 border-b border-gray-200 dark:border-gray-600';
        
        const spanFacturas = document.createElement('span');
        spanFacturas.className = 'text-xs font-medium text-gray-700 dark:text-gray-300';
        spanFacturas.textContent = 'Facturas';
        
        const btnSeleccionarTodas = document.createElement('button');
        btnSeleccionarTodas.type = 'button';
        btnSeleccionarTodas.id = 'btnSeleccionarTodas';
        btnSeleccionarTodas.className = 'text-xs px-2 py-1 bg-blue-100 dark:bg-blue-900 text-blue-700 dark:text-blue-300 rounded hover:bg-blue-200 dark:hover:bg-blue-800';
        btnSeleccionarTodas.textContent = 'Seleccionar todas';
        btnSeleccionarTodas.addEventListener('click', (e) => {
            e.preventDefault();
            e.stopPropagation();
            FacturaManager.seleccionarTodas();
        });
        
        headerDiv.appendChild(spanFacturas);
        headerDiv.appendChild(btnSeleccionarTodas);
        facturasList.appendChild(headerDiv);
        
        // Crear contenedor para facturas
        const facturasContainerList = document.createElement('div');
        facturasContainerList.id = 'facturasContainerList';
        facturasContainerList.className = 'space-y-2';
        facturasList.appendChild(facturasContainerList);
        
        facturasConSaldo.forEach(factura => {
            const facturaId = parseInt(factura.id) || factura.id;
            const saldoPendiente = parseFloat(factura.saldoPendiente) || 0;
            const totalPagado = parseFloat(factura.totalPagado) || 0;
            const montoTotal = parseFloat(factura.monto) || 0;
            
            this.facturasCargadas.push({
                id: facturaId,
                numero: factura.numero,
                montoTotal: montoTotal,
                totalPagado: totalPagado,
                saldoPendiente: saldoPendiente
            });
            
            const facturaItem = this.crearItemFactura(factura, facturaId, saldoPendiente, totalPagado, montoTotal);
            facturasContainerList.appendChild(facturaItem);
        });
        
        PagoManager.actualizarMonto();
    },
    
    crearItemFactura(factura, facturaId, saldoPendiente, totalPagado, montoTotal) {
        const facturaItem = document.createElement('div');
        facturaItem.className = 'flex items-start space-x-2 p-2 rounded border border-gray-200 dark:border-gray-600 hover:border-blue-400 dark:hover:border-blue-500 hover:bg-blue-50 dark:hover:bg-blue-900 transition-all cursor-pointer';
        
        const checkbox = document.createElement('input');
        checkbox.type = 'checkbox';
        checkbox.name = 'facturaCheckbox';
        checkbox.value = facturaId;
        checkbox.id = `factura_${facturaId}`;
        checkbox.className = 'mt-0.5 w-4 h-4 text-blue-600 bg-gray-100 border border-gray-300 rounded focus:ring-blue-500 dark:focus:ring-blue-600 focus:ring-1 dark:bg-gray-700 dark:border-gray-600 cursor-pointer';
        checkbox.addEventListener('change', function() {
            // Usar función normal para mantener el contexto
            setTimeout(function() {
                PagoManager.actualizarMonto();
            }, 50);
        });
        
        const label = document.createElement('label');
        label.htmlFor = `factura_${facturaId}`;
        label.className = 'flex-1 cursor-pointer';
        
        let textoFactura = `${factura.numero} - ${formatearPrecioConSimbolo(montoTotal)}`;
        if (totalPagado > 0) {
            textoFactura += ` (Pagado: ${formatearPrecioConSimbolo(totalPagado)}, Pendiente: ${formatearPrecioConSimbolo(saldoPendiente)})`;
        } else {
            textoFactura += ` (Pendiente: ${formatearPrecioConSimbolo(saldoPendiente)})`;
        }
        
        label.innerHTML = `
            <div class="text-xs font-medium text-gray-900 dark:text-white">${textoFactura}</div>
            <div class="text-xs text-gray-500 dark:text-gray-400 mt-0.5">Saldo: ${formatearPrecioConSimbolo(saldoPendiente)}</div>
        `;
        
        // Hacer que todo el item sea clickeable
        facturaItem.addEventListener('click', (e) => {
            if (e.target !== checkbox && e.target !== label && !label.contains(e.target)) {
                checkbox.checked = !checkbox.checked;
                checkbox.dispatchEvent(new Event('change'));
            }
        });
        
        facturaItem.appendChild(checkbox);
        facturaItem.appendChild(label);
        
        return facturaItem;
    },
    
    seleccionarTodas() {
        const checkboxes = document.querySelectorAll('input[name="facturaCheckbox"]');
        if (checkboxes.length === 0) return;
        
        const todasSeleccionadas = Array.from(checkboxes).every(cb => cb.checked);
        
        checkboxes.forEach(checkbox => {
            checkbox.checked = !todasSeleccionadas;
            // Disparar evento change para actualizar el monto
            const event = new Event('change', { bubbles: true });
            checkbox.dispatchEvent(event);
        });
        
        const btnSeleccionarTodas = document.getElementById('btnSeleccionarTodas');
        if (btnSeleccionarTodas) {
            btnSeleccionarTodas.textContent = todasSeleccionadas ? 'Seleccionar todas' : 'Deseleccionar todas';
            btnSeleccionarTodas.className = todasSeleccionadas 
                ? 'text-xs px-2 py-1 bg-blue-100 dark:bg-blue-900 text-blue-700 dark:text-blue-300 rounded hover:bg-blue-200 dark:hover:bg-blue-800'
                : 'text-xs px-2 py-1 bg-red-100 dark:bg-red-900 text-red-700 dark:text-red-300 rounded hover:bg-red-200 dark:hover:bg-red-800';
        }
        
        // Actualizar monto después de un pequeño delay
        setTimeout(() => {
            PagoManager.actualizarMonto();
        }, 50);
    },
    
    limpiarFacturas() {
        document.getElementById('facturasList').innerHTML = '';
        document.getElementById('facturasList').classList.add('hidden');
        document.getElementById('sinFacturasMensaje').classList.remove('hidden');
        document.getElementById('sinFacturasMensaje').textContent = 'Selecciona un cliente para ver las facturas disponibles';
        document.getElementById('facturasSeleccionadasCount').textContent = '(0 seleccionadas)';
        document.getElementById('Monto').value = '';
        document.getElementById('Monto').readOnly = true;
        this.facturasCargadas = [];
    }
};

// ============================================
// GESTIÓN DE PAGOS
// ============================================
const PagoManager = {
    actualizarMonto() {
        const checkboxes = document.querySelectorAll('input[name="facturaCheckbox"]:checked');
        const montoInput = document.getElementById('Monto');
        const facturasSeleccionadasCount = document.getElementById('facturasSeleccionadasCount');
        
        if (!montoInput) {
            console.error('Monto input no encontrado');
            return;
        }
        
        let montoTotal = 0;
        const facturaIds = [];
        
        checkboxes.forEach(checkbox => {
            const facturaId = parseInt(checkbox.value);
            if (isNaN(facturaId)) return;
            
            const factura = FacturaManager.facturasCargadas.find(f => {
                const fId = typeof f.id === 'number' ? f.id : parseInt(f.id);
                return fId === facturaId;
            });
            
            if (factura) {
                const saldo = parseFloat(factura.saldoPendiente) || 0;
                if (!isNaN(saldo) && saldo > 0) {
                    montoTotal += saldo;
                    facturaIds.push(facturaId);
                }
            }
        });
        
        const count = facturaIds.length;
        
        // Actualizar contador de facturas seleccionadas
        if (facturasSeleccionadasCount) {
            facturasSeleccionadasCount.textContent = `(${count})`;
        }
        
        // Actualizar campos hidden - CRÍTICO para la validación
        const facturaIdHidden = document.getElementById('FacturaId');
        const facturaIdsHidden = document.getElementById('FacturaIds');
        
        if (facturaIdHidden && facturaIdsHidden) {
            if (count === 1) {
                facturaIdHidden.value = facturaIds[0].toString();
                facturaIdsHidden.value = '';
            } else if (count > 1) {
                facturaIdHidden.value = '';
                facturaIdsHidden.value = facturaIds.join(',');
            } else {
                facturaIdHidden.value = '';
                facturaIdsHidden.value = '';
            }
        } else {
            console.error('Campos hidden de facturas no encontrados');
        }
        
        // Actualizar monto
        if (montoTotal > 0 && montoTotal <= 1000000) {
            const montoFormateado = Number(montoTotal.toFixed(2));
            montoInput.value = montoFormateado;
            montoInput.readOnly = true;
            montoInput.setAttribute('data-monto-esperado', montoFormateado);
            
            // Actualizar campos de pago si hay tipo de pago seleccionado
            if (window.mostrarCamposPago) {
                window.mostrarCamposPago();
            }
        } else if (count === 0) {
            montoInput.value = '';
            montoInput.readOnly = true;
            montoInput.removeAttribute('data-monto-esperado');
        } else {
            montoInput.value = '';
            montoInput.removeAttribute('data-monto-esperado');
        }
        
        // Debug: mostrar en consola para verificar
    },
    
    calcularTotalPago() {
        const tipoPago = document.querySelector('input[name="TipoPago"]:checked')?.value;
        const moneda = document.getElementById('Moneda')?.value;
        const tipoCambioValor = PagoSystem.tipoCambio;
        
        let totalFisico = 0;
        let totalElectronico = 0;
        
        // Calcular total físico
        if (tipoPago === 'Fisico' || tipoPago === 'Mixto') {
            const montoCordobasFisicoInput = document.getElementById('MontoCordobasFisico');
            const montoDolaresFisicoInput = document.getElementById('MontoDolaresFisico');
            
            // Función auxiliar para convertir formato español (coma) a formato estándar (punto)
            const parseNumero = (valor) => {
                if (!valor || !valor.trim()) return 0;
                // Reemplazar coma por punto para formato español
                const valorNormalizado = valor.trim().replace(',', '.');
                return parseFloat(valorNormalizado) || 0;
            };
            
            // Leer valores, normalizando formato de coma a punto
            const montoCordobasFisico = parseNumero(montoCordobasFisicoInput?.value);
            const montoDolaresFisico = parseNumero(montoDolaresFisicoInput?.value);
            
            if (moneda === '$') {
                // Solo dólares: convertir a córdobas (ignorar completamente el campo de córdobas)
                totalFisico = montoDolaresFisico * tipoCambioValor;
            } else if (moneda === 'C$') {
                // Solo córdobas (ignorar completamente el campo de dólares)
                totalFisico = montoCordobasFisico;
            } else {
                // Ambos: sumar ambos
                totalFisico = montoCordobasFisico + (montoDolaresFisico * tipoCambioValor);
            }
            
            const totalFisicoValor = document.getElementById('totalFisicoValor');
            if (totalFisicoValor) {
                totalFisicoValor.textContent = formatearPrecioConSimbolo(totalFisico);
            }
        }
        
        // Calcular total electrónico
        if (tipoPago === 'Electronico' || tipoPago === 'Mixto') {
            const montoCordobasElectronicoInput = document.getElementById('MontoCordobasElectronico');
            const montoDolaresElectronicoInput = document.getElementById('MontoDolaresElectronico');
            
            // Función auxiliar para convertir formato español (coma) a formato estándar (punto)
            const parseNumero = (valor) => {
                if (!valor || !valor.trim()) return 0;
                // Reemplazar coma por punto para formato español
                const valorNormalizado = valor.trim().replace(',', '.');
                return parseFloat(valorNormalizado) || 0;
            };
            
            // Leer valores, normalizando formato de coma a punto
            const montoCordobasElectronico = parseNumero(montoCordobasElectronicoInput?.value);
            const montoDolaresElectronico = parseNumero(montoDolaresElectronicoInput?.value);
            
            if (moneda === '$') {
                totalElectronico = montoDolaresElectronico * tipoCambioValor;
            } else if (moneda === 'C$') {
                totalElectronico = montoCordobasElectronico;
            } else {
                totalElectronico = montoCordobasElectronico + (montoDolaresElectronico * tipoCambioValor);
            }
            
            const totalElectronicoValor = document.getElementById('totalElectronicoValor');
            if (totalElectronicoValor) {
                totalElectronicoValor.textContent = formatearPrecioConSimbolo(totalElectronico);
            }
        }
        
        // Mostrar resumen total para pago mixto
        const totalGeneral = totalFisico + totalElectronico;
        const totalGeneralValor = document.getElementById('totalGeneralValor');
        if (totalGeneralValor) {
            totalGeneralValor.textContent = formatearPrecioConSimbolo(totalGeneral);
        }
        
        const resumenTotal = document.getElementById('resumenTotal');
        if (resumenTotal) {
            if (tipoPago === 'Mixto' && totalGeneral > 0) {
                resumenTotal.style.display = 'block';
            } else {
                resumenTotal.style.display = 'none';
            }
        }
    },
    
    calcularVueltoFisico() {
        const tipoPago = document.querySelector('input[name="TipoPago"]:checked')?.value;
        if (tipoPago !== 'Fisico' && tipoPago !== 'Mixto') return;
        
        const parseNumero = (valor) => {
            if (!valor || !valor.trim()) return 0;
            const valorNormalizado = valor.trim().replace(',', '.');
            return parseFloat(valorNormalizado) || 0;
        };
        
        const moneda = document.getElementById('Moneda')?.value;
        const montoCordobasFisicoInput = document.getElementById('MontoCordobasFisico');
        const montoDolaresFisicoInput = document.getElementById('MontoDolaresFisico');
        let montoCordobasFisico = parseNumero(montoCordobasFisicoInput?.value);
        const montoDolaresFisico = parseNumero(montoDolaresFisicoInput?.value);
        const tipoCambioValor = PagoSystem.tipoCambio;
        const montoInput = document.getElementById('Monto');
        const montoDebeTotal = parseNumero(montoInput?.value); // Siempre en C$
        
        // Obtener montos recibidos según la moneda seleccionada
        let montoRecibidoFisico = 0;
        let montoRecibidoCordobasFisico = 0;
        let montoRecibidoDolaresFisico = 0;
        
        if (moneda === 'Ambos') {
            // Leer ambos campos de recibido
            montoRecibidoCordobasFisico = parseNumero(document.getElementById('MontoRecibidoCordobasFisico')?.value);
            montoRecibidoDolaresFisico = parseNumero(document.getElementById('MontoRecibidoDolaresFisico')?.value);
            // Convertir todo a córdobas para el total recibido
            montoRecibidoFisico = montoRecibidoCordobasFisico + (montoRecibidoDolaresFisico * tipoCambioValor);
            // Debug: verificar cálculo
                montoRecibidoCordobasFisico,
                montoRecibidoDolaresFisico,
                tipoCambioValor,
                montoRecibidoFisico
            });
        } else {
            // Usar el campo único de recibido
            montoRecibidoFisico = parseNumero(document.getElementById('MontoRecibidoFisico')?.value);
        }
        
        // Ocultar error por defecto
        const errorRecibidoFisico = document.getElementById('errorRecibidoFisico');
        const errorRecibidoFisicoAmbos = document.getElementById('errorRecibidoFisicoAmbos');
        const inputRecibidoFisico = document.getElementById('MontoRecibidoFisico');
        const inputRecibidoCordobasFisico = document.getElementById('MontoRecibidoCordobasFisico');
        const inputRecibidoDolaresFisico = document.getElementById('MontoRecibidoDolaresFisico');
        
        let vueltoFisico = 0;
        
        // Si estamos en mixto y el usuario solo escribe "Recibido" pero dejó el Monto físico vacío,
        // asumir que el monto físico es lo que está recibiendo (hasta el máximo que debe).
        if (tipoPago === 'Mixto' && moneda === 'C$' && montoRecibidoFisico > 0 && (!montoCordobasFisico || montoCordobasFisico === 0)) {
            const montoAsignado = Math.min(montoRecibidoFisico, montoDebeTotal);
            montoCordobasFisico = montoAsignado;
            if (montoCordobasFisicoInput) {
                montoCordobasFisicoInput.value = montoAsignado.toFixed(2);
            }
        }
        
        // Determinar contra qué monto se calcula el vuelto:
        // - En Mixto con C$, el vuelto debe tomar como base SOLO el monto físico.
        // - En Mixto con $, el vuelto debe tomar como base SOLO el monto físico (convertido a C$).
        // - En Mixto con Ambos, el vuelto debe tomar como base SOLO el monto físico (suma de ambos).
        // - En Físico puro con C$, usa el monto físico en córdobas.
        // - En Físico puro con $, usa el monto físico en dólares convertido a córdobas.
        // - En Físico puro con Ambos, usa el monto físico (suma de ambos en C$).
        let baseParaVuelto = montoDebeTotal; // Por defecto, el total en córdobas
        if (tipoPago === 'Mixto') {
            if (moneda === 'C$' && montoCordobasFisico > 0) {
                baseParaVuelto = montoCordobasFisico;
            } else if (moneda === '$' && montoDolaresFisico > 0) {
                // Convertir monto físico en dólares a córdobas
                baseParaVuelto = montoDolaresFisico * tipoCambioValor;
            } else if (moneda === 'Ambos') {
                // Sumar ambos montos físicos en córdobas
                baseParaVuelto = montoCordobasFisico + (montoDolaresFisico * tipoCambioValor);
            }
        } else if (tipoPago === 'Fisico') {
            // Para pago físico puro, usar el monto físico ingresado
            if (moneda === 'C$' && montoCordobasFisico > 0) {
                baseParaVuelto = montoCordobasFisico;
            } else if (moneda === '$' && montoDolaresFisico > 0) {
                // Convertir monto físico en dólares a córdobas
                baseParaVuelto = montoDolaresFisico * tipoCambioValor;
            } else if (moneda === 'Ambos') {
                // Sumar ambos montos físicos en córdobas
                baseParaVuelto = montoCordobasFisico + (montoDolaresFisico * tipoCambioValor);
            }
        }
        
        // Validar que el monto recibido sea mayor o igual al monto a pagar
        if (montoRecibidoFisico > 0 || montoRecibidoCordobasFisico > 0 || montoRecibidoDolaresFisico > 0) {
            // Validación específica para "Ambos": validar Recibido ($) >= Monto ($) cuando hay Monto ($)
            if (moneda === 'Ambos' && tipoPago === 'Fisico' && montoDolaresFisico > 0 && montoRecibidoDolaresFisico > 0) {
                // Validar que Recibido ($) sea mayor o igual a Monto ($)
                if (montoRecibidoDolaresFisico < montoDolaresFisico - 0.01) {
                    // Mostrar error específico para dólares
                    if (errorRecibidoFisicoAmbos) {
                        errorRecibidoFisicoAmbos.textContent = `El monto recibido en dólares debe ser mayor o igual a $ ${montoDolaresFisico.toFixed(2).replace('.', ',')}`;
                        errorRecibidoFisicoAmbos.classList.remove('hidden');
                    }
                    if (inputRecibidoDolaresFisico) {
                        inputRecibidoDolaresFisico.classList.add('border-red-500', 'dark:border-red-500');
                        inputRecibidoDolaresFisico.classList.remove('border-gray-300', 'dark:border-gray-600');
                    }
                    vueltoFisico = 0;
                    // No continuar con el resto de la validación si hay error en dólares
                    return;
                } else {
                    // Si Recibido ($) es válido, ocultar error de dólares
                    if (inputRecibidoDolaresFisico) {
                        inputRecibidoDolaresFisico.classList.remove('border-red-500', 'dark:border-red-500');
                        inputRecibidoDolaresFisico.classList.add('border-gray-300', 'dark:border-gray-600');
                    }
                }
            }
            
            // Convertir monto recibido a córdobas para comparar
            let montoRecibidoEnCordobas = montoRecibidoFisico;
            if (moneda === 'Ambos') {
                // Ya está calculado arriba: suma de ambos en córdobas
                montoRecibidoEnCordobas = montoRecibidoCordobasFisico + (montoRecibidoDolaresFisico * tipoCambioValor);
            } else if (moneda === '$') {
                // Recibido se interpreta en dólares, convertir a C$
                montoRecibidoEnCordobas = montoRecibidoFisico * tipoCambioValor;
            }
            
            // Validación: el monto recibido debe ser >= al monto a pagar (ambos en córdobas)
            // Para "Ambos", si Recibido ($) >= Monto ($), validar contra el monto total, no contra baseParaVuelto
            if (moneda === 'Ambos' && tipoPago === 'Fisico' && montoDolaresFisico > 0 && montoRecibidoDolaresFisico >= montoDolaresFisico - 0.01) {
                // Si Recibido ($) >= Monto ($), validar contra el monto total original
                baseParaVuelto = montoDebeTotal;
            }
            
            // Usar tolerancia de 0.01 para manejar errores de precisión de punto flotante
            const diferencia = montoRecibidoEnCordobas - baseParaVuelto;
            // Debug: verificar validación
                moneda,
                montoRecibidoEnCordobas,
                baseParaVuelto,
                diferencia,
                debeMostrarError: diferencia < -0.01
            });
            if (diferencia < -0.01) {
                // Mostrar error solo si el usuario ya ingresó valores en ambos campos o si ya ingresó algo en el campo que falta
                // Si está en "Ambos" y solo ingresó Recibido (C$), no mostrar error hasta que también ingrese Recibido ($)
                let debeMostrarError = true;
                if (moneda === 'Ambos' && tipoPago === 'Fisico') {
                    // Si solo ingresó Recibido (C$) pero no Recibido ($), no mostrar error todavía
                    // El usuario puede estar ingresando los valores por partes
                    if (montoRecibidoCordobasFisico > 0 && montoRecibidoDolaresFisico === 0) {
                        debeMostrarError = false;
                    }
                    // Si solo ingresó Recibido ($) pero no Recibido (C$), no mostrar error todavía
                    else if (montoRecibidoDolaresFisico > 0 && montoRecibidoCordobasFisico === 0) {
                        debeMostrarError = false;
                    }
                }
                
                if (debeMostrarError) {
                    // Mostrar error
                    if (moneda === 'Ambos') {
                        if (errorRecibidoFisicoAmbos) {
                            const montoCordobas = montoCordobasFisico;
                            const montoDolares = montoDolaresFisico;
                            if (montoCordobas > 0 && montoDolares > 0) {
                                errorRecibidoFisicoAmbos.textContent = `El monto recibido debe ser mayor o igual a C$ ${montoCordobas.toFixed(2).replace('.', ',')} + $ ${montoDolares.toFixed(2).replace('.', ',')}`;
                            } else if (montoCordobas > 0) {
                                errorRecibidoFisicoAmbos.textContent = `El monto recibido debe ser mayor o igual a C$ ${montoCordobas.toFixed(2).replace('.', ',')}`;
                            } else if (montoDolares > 0) {
                                errorRecibidoFisicoAmbos.textContent = `El monto recibido debe ser mayor o igual a $ ${montoDolares.toFixed(2).replace('.', ',')}`;
                            } else {
                                errorRecibidoFisicoAmbos.textContent = `El monto recibido debe ser mayor o igual a C$ ${baseParaVuelto.toFixed(2).replace('.', ',')}`;
                            }
                            errorRecibidoFisicoAmbos.classList.remove('hidden');
                        }
                        if (inputRecibidoCordobasFisico) {
                            inputRecibidoCordobasFisico.classList.add('border-red-500', 'dark:border-red-500');
                            inputRecibidoCordobasFisico.classList.remove('border-gray-300', 'dark:border-gray-600');
                        }
                        if (inputRecibidoDolaresFisico) {
                            inputRecibidoDolaresFisico.classList.add('border-red-500', 'dark:border-red-500');
                            inputRecibidoDolaresFisico.classList.remove('border-gray-300', 'dark:border-gray-600');
                        }
                    } else {
                        const monedaSimbolo = moneda === '$' ? '$' : 'C$';
                        // Convertir baseParaVuelto a la moneda seleccionada para el mensaje
                        let montoFormateado;
                        if (moneda === '$') {
                            montoFormateado = (baseParaVuelto / tipoCambioValor).toFixed(2).replace('.', ',');
                        } else {
                            montoFormateado = baseParaVuelto.toFixed(2).replace('.', ',');
                        }
                        if (errorRecibidoFisico) {
                            errorRecibidoFisico.textContent = `El monto recibido debe ser mayor o igual a ${monedaSimbolo} ${montoFormateado}`;
                            errorRecibidoFisico.classList.remove('hidden');
                        }
                        if (inputRecibidoFisico) {
                            inputRecibidoFisico.classList.add('border-red-500', 'dark:border-red-500');
                            inputRecibidoFisico.classList.remove('border-gray-300', 'dark:border-gray-600');
                        }
                    }
                    vueltoFisico = 0;
                } else {
                    // Si no se debe mostrar error (porque el usuario está ingresando por partes), ocultar error
                    if (moneda === 'Ambos') {
                        if (errorRecibidoFisicoAmbos) {
                            errorRecibidoFisicoAmbos.classList.add('hidden');
                        }
                        if (inputRecibidoCordobasFisico) {
                            inputRecibidoCordobasFisico.classList.remove('border-red-500', 'dark:border-red-500');
                            inputRecibidoCordobasFisico.classList.add('border-gray-300', 'dark:border-gray-600');
                        }
                        if (inputRecibidoDolaresFisico) {
                            inputRecibidoDolaresFisico.classList.remove('border-red-500', 'dark:border-red-500');
                            inputRecibidoDolaresFisico.classList.add('border-gray-300', 'dark:border-gray-600');
                        }
                    }
                    vueltoFisico = 0;
                }
            } else {
                // Ocultar error si es válido
                if (moneda === 'Ambos') {
                    if (errorRecibidoFisicoAmbos) {
                        errorRecibidoFisicoAmbos.classList.add('hidden');
                    }
                    if (inputRecibidoCordobasFisico) {
                        inputRecibidoCordobasFisico.classList.remove('border-red-500', 'dark:border-red-500');
                        inputRecibidoCordobasFisico.classList.add('border-gray-300', 'dark:border-gray-600');
                    }
                    if (inputRecibidoDolaresFisico) {
                        inputRecibidoDolaresFisico.classList.remove('border-red-500', 'dark:border-red-500');
                        inputRecibidoDolaresFisico.classList.add('border-gray-300', 'dark:border-gray-600');
                    }
                } else {
                    if (errorRecibidoFisico) {
                        errorRecibidoFisico.classList.add('hidden');
                    }
                    if (inputRecibidoFisico) {
                        inputRecibidoFisico.classList.remove('border-red-500', 'dark:border-red-500');
                        inputRecibidoFisico.classList.add('border-gray-300', 'dark:border-gray-600');
                    }
                }
                // Calcular vuelto
                vueltoFisico = montoRecibidoEnCordobas > baseParaVuelto ? montoRecibidoEnCordobas - baseParaVuelto : 0;
                // Debug: verificar cálculo del vuelto
                if (moneda === 'Ambos') {
                        montoRecibidoCordobasFisico,
                        montoRecibidoDolaresFisico,
                        tipoCambioValor,
                        montoRecibidoEnCordobas,
                        baseParaVuelto,
                        vueltoFisico
                    });
                }
            }
        } else {
            // Si no se ingresó "Recibido", ocultar error
            if (moneda === 'Ambos') {
                if (errorRecibidoFisicoAmbos) {
                    errorRecibidoFisicoAmbos.classList.add('hidden');
                }
                if (inputRecibidoCordobasFisico) {
                    inputRecibidoCordobasFisico.classList.remove('border-red-500', 'dark:border-red-500');
                    inputRecibidoCordobasFisico.classList.add('border-gray-300', 'dark:border-gray-600');
                }
                if (inputRecibidoDolaresFisico) {
                    inputRecibidoDolaresFisico.classList.remove('border-red-500', 'dark:border-red-500');
                    inputRecibidoDolaresFisico.classList.add('border-gray-300', 'dark:border-gray-600');
                }
            } else {
                if (errorRecibidoFisico) {
                    errorRecibidoFisico.classList.add('hidden');
                }
                if (inputRecibidoFisico) {
                    inputRecibidoFisico.classList.remove('border-red-500', 'dark:border-red-500');
                    inputRecibidoFisico.classList.add('border-gray-300', 'dark:border-gray-600');
                }
            }
            vueltoFisico = 0;
        }
        
        // Redondeo inteligente: si el vuelto es mayor a 5, redondear a valores completos
        let vueltoFinal = vueltoFisico;
        if (vueltoFisico > 5) {
            vueltoFinal = Math.round(vueltoFisico);
        } else {
            vueltoFinal = Math.round(vueltoFisico * 100) / 100; // Mantener 2 decimales si es <= 5
        }
        
        const vueltoFisicoInput = document.getElementById('VueltoFisico');
        if (vueltoFisicoInput) {
            // Mantener punto decimal para el servidor, pero mostrar formateado
            vueltoFisicoInput.value = vueltoFinal.toFixed(2);
        }
    },
    
    actualizarEtiquetaMontoRecibido() {
        const monedaRecibidoLabel = document.getElementById('monedaRecibidoLabel');
        if (!monedaRecibidoLabel) return;
        
        const montoCordobasFisico = parseFloat(document.getElementById('MontoCordobasFisico')?.value || 0);
        const montoDolaresFisico = parseFloat(document.getElementById('MontoDolaresFisico')?.value || 0);
        const monedaSeleccionada = document.getElementById('Moneda')?.value || '';
        
        if ((monedaSeleccionada === '$' && montoDolaresFisico > 0) || (montoDolaresFisico > 0 && montoCordobasFisico === 0)) {
            monedaRecibidoLabel.textContent = '($)';
        } else {
            monedaRecibidoLabel.textContent = '(C$)';
        }
    },
    
    /**
     * Calcula el monto faltante y prellena automáticamente Monto ($) o Monto (C$)
     * cuando el usuario ingresa Recibido (C$) o Recibido ($) en modo "Ambos"
     */
    calcularMontoFaltanteDesdeRecibido() {
        const moneda = document.getElementById('Moneda')?.value;
        if (moneda !== 'Ambos') return;
        
        const tipoPago = document.querySelector('input[name="TipoPago"]:checked')?.value;
        if (tipoPago !== 'Fisico') return;
        
        const montoInput = document.getElementById('Monto');
        if (!montoInput || !montoInput.value) return;
        
        const parseNumero = (valor) => {
            if (!valor || !valor.trim()) return 0;
            const valorNormalizado = valor.trim().replace(',', '.');
            return parseFloat(valorNormalizado) || 0;
        };
        
        const montoTotal = parseNumero(montoInput.value); // Siempre en C$
        const tipoCambioValor = PagoSystem.tipoCambio;
        
        const montoRecibidoCordobasFisicoInput = document.getElementById('MontoRecibidoCordobasFisico');
        const montoRecibidoDolaresFisicoInput = document.getElementById('MontoRecibidoDolaresFisico');
        const montoCordobasFisicoInput = document.getElementById('MontoCordobasFisico');
        const montoDolaresFisicoInput = document.getElementById('MontoDolaresFisico');
        
        if (!montoRecibidoCordobasFisicoInput || !montoRecibidoDolaresFisicoInput || 
            !montoCordobasFisicoInput || !montoDolaresFisicoInput) return;
        
        const montoRecibidoCordobasFisico = parseNumero(montoRecibidoCordobasFisicoInput.value);
        const montoRecibidoDolaresFisico = parseNumero(montoRecibidoDolaresFisicoInput.value);
        const montoCordobasFisico = parseNumero(montoCordobasFisicoInput.value);
        const montoDolaresFisico = parseNumero(montoDolaresFisicoInput.value);
        
        // Caso 1: Usuario ingresa Recibido (C$)
        if (montoRecibidoCordobasFisico > 0 && montoRecibidoDolaresFisico === 0) {
            // Calcular faltante en córdobas desde el monto total
            const faltaCordoba = montoTotal - montoRecibidoCordobasFisico;
            
            // Actualizar Monto (C$) con el valor recibido (lo que se paga en C$)
            montoCordobasFisicoInput.value = montoRecibidoCordobasFisico.toFixed(2);
            
            // Si hay faltante, convertir a dólares y prellenar Monto ($)
            if (faltaCordoba > 0.01) {
                const faltaDolar = faltaCordoba / tipoCambioValor;
                montoDolaresFisicoInput.value = faltaDolar.toFixed(2);
            } else {
                // Si no hay faltante, limpiar Monto ($)
                montoDolaresFisicoInput.value = '0.00';
            }
        }
        // Caso 2: Usuario ingresa Recibido ($)
        else if (montoRecibidoDolaresFisico > 0 && montoRecibidoCordobasFisico === 0) {
            // Convertir recibido en dólares a córdobas
            const recibidoCordobaEquivalente = montoRecibidoDolaresFisico * tipoCambioValor;
            
            // Calcular faltante en córdobas
            const faltaCordoba = montoTotal - recibidoCordobaEquivalente;
            
            // Monto ($) = Recibido ($) (lo que se debe en dólares es lo que se pagó)
            montoDolaresFisicoInput.value = montoRecibidoDolaresFisico.toFixed(2);
            
            // Si hay faltante, prellenar Monto (C$)
            if (faltaCordoba > 0.01) {
                // Prellenar Monto (C$) con el faltante
                montoCordobasFisicoInput.value = faltaCordoba.toFixed(2);
            } else {
                // Si no hay faltante, limpiar Monto (C$)
                montoCordobasFisicoInput.value = '0.00';
            }
        }
        // Si ambos tienen valores recibidos, no hacer nada (el usuario los está ingresando manualmente)
    }
};

// ============================================
// GESTIÓN DE CAMPOS DE PAGO
// ============================================
const CamposPagoManager = {
    mostrarCamposPago() {
        const tipoPago = document.querySelector('input[name="TipoPago"]:checked')?.value;
        const moneda = document.getElementById('Moneda')?.value;
        const montoInput = document.getElementById('Monto');
        const montoTotal = parseFloat(montoInput?.value || 0);
        const tipoCambioValor = PagoSystem.tipoCambio;
        const detallesPago = document.getElementById('detallesPago');
        
        const camposFisico = document.getElementById('camposFisico');
        const camposElectronico = document.getElementById('camposElectronico');
        const camposVueltoFisico = document.getElementById('camposVueltoFisico');
        const banco = document.getElementById('Banco');
        const tipoCuenta = document.getElementById('TipoCuenta');
        const resumenTotal = document.getElementById('resumenTotal');
        
        // Mostrar sección de detalles si hay tipo de pago seleccionado
        if (tipoPago && detallesPago) {
            detallesPago.classList.remove('hidden');
        }
        
        // Mostrar/ocultar secciones según tipo de pago
        if (tipoPago === 'Fisico') {
            if (camposFisico) camposFisico.style.display = 'block';
            if (camposElectronico) camposElectronico.style.display = 'none';
            if (camposVueltoFisico) camposVueltoFisico.style.display = 'grid';
            if (banco) banco.removeAttribute('required');
            if (tipoCuenta) tipoCuenta.removeAttribute('required');
            if (resumenTotal) resumenTotal.style.display = 'none';
            
            this.preLlenarCamposFisicos(moneda, montoTotal, tipoCambioValor);
        } else if (tipoPago === 'Electronico') {
            if (camposFisico) camposFisico.style.display = 'none';
            if (camposElectronico) camposElectronico.style.display = 'block';
            if (banco) banco.setAttribute('required', 'required');
            if (tipoCuenta) tipoCuenta.setAttribute('required', 'required');
            if (resumenTotal) resumenTotal.style.display = 'none';
            
            this.preLlenarCamposElectronicos(moneda, montoTotal, tipoCambioValor);
        } else if (tipoPago === 'Mixto') {
            if (camposFisico) camposFisico.style.display = 'block';
            if (camposElectronico) camposElectronico.style.display = 'block';
            if (camposVueltoFisico) camposVueltoFisico.style.display = 'grid';
            if (banco) banco.setAttribute('required', 'required');
            if (tipoCuenta) tipoCuenta.setAttribute('required', 'required');
            if (resumenTotal) resumenTotal.style.display = 'block';
            
            // Para pago mixto, NO pre-llenar automáticamente
            // El usuario debe ingresar manualmente cuánto paga en físico y cuánto en electrónico
            // Limpiar campos si ambos tienen el monto total completo (fueron pre-llenados automáticamente)
            this.limpiarCamposMixtoSiNecesario(moneda, montoTotal, tipoCambioValor);
            
            // Si ya hay un monto físico ingresado, calcular automáticamente el electrónico
            setTimeout(() => {
                this.calcularMontoElectronicoAutomatico();
            }, 100);
        } else {
            if (detallesPago) detallesPago.classList.add('hidden');
            if (camposFisico) camposFisico.style.display = 'none';
            if (camposElectronico) camposElectronico.style.display = 'none';
            if (banco) banco.removeAttribute('required');
            if (tipoCuenta) tipoCuenta.removeAttribute('required');
            if (resumenTotal) resumenTotal.style.display = 'none';
        }
        
        PagoManager.calcularTotalPago();
        PagoManager.actualizarEtiquetaMontoRecibido();
    },
    
    preLlenarCamposFisicos(moneda, montoTotal, tipoCambioValor) {
        if (montoTotal <= 0) return;
        
        const montoCordobasFisico = document.getElementById('MontoCordobasFisico');
        const montoDolaresFisico = document.getElementById('MontoDolaresFisico');
        
        if (moneda === '$') {
            // Si la moneda es dólares, SIEMPRE limpiar córdobas y pre-llenar dólares
            if (montoCordobasFisico) {
                montoCordobasFisico.value = '';
            }
            if (montoDolaresFisico && (!montoDolaresFisico.value || parseFloat(montoDolaresFisico.value) === 0)) {
                montoDolaresFisico.value = (montoTotal / tipoCambioValor).toFixed(2);
            }
        } else if (moneda === 'C$') {
            // Si la moneda es córdobas, SIEMPRE limpiar dólares y pre-llenar córdobas
            if (montoDolaresFisico) {
                montoDolaresFisico.value = '';
            }
            if (montoCordobasFisico && (!montoCordobasFisico.value || parseFloat(montoCordobasFisico.value) === 0)) {
                montoCordobasFisico.value = montoTotal.toFixed(2);
            }
        } else if (moneda === 'Ambos') {
            // Si la moneda es "Ambos", pre-llenar córdobas con el monto total
            // El usuario puede ajustar después y agregar dólares si quiere
            if (montoCordobasFisico && (!montoCordobasFisico.value || parseFloat(montoCordobasFisico.value) === 0)) {
                montoCordobasFisico.value = montoTotal.toFixed(2);
            }
            // Dejar dólares vacío para que el usuario lo complete si quiere
        }
    },
    
    preLlenarCamposElectronicos(moneda, montoTotal, tipoCambioValor) {
        if (montoTotal <= 0) return;
        
        const montoCordobasElectronico = document.getElementById('MontoCordobasElectronico');
        const montoDolaresElectronico = document.getElementById('MontoDolaresElectronico');
        
        if (moneda === '$') {
            // Si la moneda es dólares, SIEMPRE limpiar córdobas y pre-llenar dólares
            if (montoCordobasElectronico) {
                montoCordobasElectronico.value = '';
            }
            if (montoDolaresElectronico && (!montoDolaresElectronico.value || parseFloat(montoDolaresElectronico.value) === 0)) {
                montoDolaresElectronico.value = (montoTotal / tipoCambioValor).toFixed(2);
            }
        } else if (moneda === 'C$') {
            // Si la moneda es córdobas, SIEMPRE limpiar dólares y pre-llenar córdobas
            if (montoDolaresElectronico) {
                montoDolaresElectronico.value = '';
            }
            if (montoCordobasElectronico && (!montoCordobasElectronico.value || parseFloat(montoCordobasElectronico.value) === 0)) {
                montoCordobasElectronico.value = montoTotal.toFixed(2);
            }
        }
    },
    
    limpiarCamposMixtoSiNecesario(moneda, montoTotal, tipoCambioValor) {
        // Para pago mixto, si ambos campos tienen el monto total completo, limpiarlos
        // para que el usuario pueda ingresar la distribución manualmente
        
        const montoCordobasFisico = document.getElementById('MontoCordobasFisico');
        const montoDolaresFisico = document.getElementById('MontoDolaresFisico');
        const montoCordobasElectronico = document.getElementById('MontoCordobasElectronico');
        const montoDolaresElectronico = document.getElementById('MontoDolaresElectronico');
        
        // Función auxiliar para normalizar y comparar valores
        const parseNumero = (valor) => {
            if (!valor || !valor.trim()) return 0;
            const valorNormalizado = valor.trim().replace(',', '.');
            return parseFloat(valorNormalizado) || 0;
        };
        
        const montoTotalRedondeado = Math.round(montoTotal * 100) / 100;
        
        // Si el campo físico tiene el monto total completo y el electrónico también, limpiar ambos
        if (moneda === 'C$') {
            const montoFisico = parseNumero(montoCordobasFisico?.value);
            const montoElectronico = parseNumero(montoCordobasElectronico?.value);
            
            // Si ambos tienen el monto total (o muy cercano), limpiar ambos para que el usuario distribuya
            if (Math.abs(montoFisico - montoTotalRedondeado) < 0.01 && 
                Math.abs(montoElectronico - montoTotalRedondeado) < 0.01) {
                if (montoCordobasFisico) montoCordobasFisico.value = '';
                if (montoCordobasElectronico) montoCordobasElectronico.value = '';
            }
        } else if (moneda === '$') {
            const montoFisico = parseNumero(montoDolaresFisico?.value);
            const montoElectronico = parseNumero(montoDolaresElectronico?.value);
            const montoTotalEnDolares = montoTotal / tipoCambioValor;
            const montoTotalEnDolaresRedondeado = Math.round(montoTotalEnDolares * 100) / 100;
            
            // Si ambos tienen el monto total en dólares (o muy cercano), limpiar ambos
            if (Math.abs(montoFisico - montoTotalEnDolaresRedondeado) < 0.01 && 
                Math.abs(montoElectronico - montoTotalEnDolaresRedondeado) < 0.01) {
                if (montoDolaresFisico) montoDolaresFisico.value = '';
                if (montoDolaresElectronico) montoDolaresElectronico.value = '';
            }
        }
    },
    
    calcularMontoElectronicoAutomatico() {
        const tipoPago = document.querySelector('input[name="TipoPago"]:checked')?.value;
        if (tipoPago !== 'Mixto') return;
        
        const moneda = document.getElementById('Moneda')?.value;
        const montoInput = document.getElementById('Monto');
        if (!montoInput || !montoInput.value) return;
        
        const montoTotal = parseFloat(montoInput.value) || 0;
        const tipoCambioValor = PagoSystem.tipoCambio;
        
        // Función auxiliar para parsear números
        const parseNumero = (valor) => {
            if (!valor || !valor.trim()) return 0;
            const valorNormalizado = valor.trim().replace(',', '.');
            return parseFloat(valorNormalizado) || 0;
        };
        
        // Calcular monto físico actual
        let montoFisico = 0;
        if (moneda === 'C$') {
            montoFisico = parseNumero(document.getElementById('MontoCordobasFisico')?.value);
        } else if (moneda === '$') {
            const montoDolaresFisico = parseNumero(document.getElementById('MontoDolaresFisico')?.value);
            montoFisico = montoDolaresFisico * tipoCambioValor; // Convertir a córdobas
        }
        
        // Calcular monto electrónico automáticamente: Total - Físico
        const montoElectronico = Math.max(0, montoTotal - montoFisico);
        
        // Actualizar campo electrónico según la moneda
        if (moneda === 'C$') {
            const montoCordobasElectronico = document.getElementById('MontoCordobasElectronico');
            if (montoCordobasElectronico && montoElectronico > 0) {
                // Solo actualizar si el campo está vacío o si el usuario no lo ha editado manualmente
                // Para detectar si fue editado manualmente, verificamos si la suma actual no coincide
                const montoElectronicoActual = parseNumero(montoCordobasElectronico.value);
                const montoFisicoActual = parseNumero(document.getElementById('MontoCordobasFisico')?.value);
                const sumaActual = montoFisicoActual + montoElectronicoActual;
                
                // Si la suma actual no coincide con el total, significa que el usuario editó manualmente
                // En ese caso, solo actualizar si el campo electrónico está vacío o muy cerca del cálculo
                if (montoElectronicoActual === 0 || Math.abs(sumaActual - montoTotal) > 0.01) {
                    montoCordobasElectronico.value = montoElectronico.toFixed(2);
                }
            }
        } else if (moneda === '$') {
            const montoDolaresElectronico = document.getElementById('MontoDolaresElectronico');
            if (montoDolaresElectronico && montoElectronico > 0) {
                const montoElectronicoEnDolares = montoElectronico / tipoCambioValor;
                const montoElectronicoActual = parseNumero(montoDolaresElectronico.value);
                const montoFisicoActual = parseNumero(document.getElementById('MontoDolaresFisico')?.value);
                const sumaActual = (montoFisicoActual + montoElectronicoActual) * tipoCambioValor;
                
                if (montoElectronicoActual === 0 || Math.abs(sumaActual - montoTotal) > 0.01) {
                    montoDolaresElectronico.value = montoElectronicoEnDolares.toFixed(2);
                }
            }
        }
    },
    
    actualizarTipoCambio() {
        const moneda = document.getElementById('Moneda')?.value;
        const montoInput = document.getElementById('Monto');
        const tipoPago = document.querySelector('input[name="TipoPago"]:checked')?.value;
        
        if (!montoInput || !montoInput.value || parseFloat(montoInput.value) <= 0) return;
        
        const montoTotal = parseFloat(montoInput.value);
        const tipoCambioValor = PagoSystem.tipoCambio;
        const tipoPagoParaUsar = tipoPago || 'Fisico';
        
        // Cambiar TODOS los campos según la moneda seleccionada
        if (moneda === '$') {
            // Convertir todo a dólares
            const montoDolaresFisico = document.getElementById('MontoDolaresFisico');
            const montoCordobasFisico = document.getElementById('MontoCordobasFisico');
            const montoDolaresElectronico = document.getElementById('MontoDolaresElectronico');
            const montoCordobasElectronico = document.getElementById('MontoCordobasElectronico');
            
            // Físico
            if (montoDolaresFisico && (!montoDolaresFisico.value || parseFloat(montoDolaresFisico.value) === 0)) {
                montoDolaresFisico.value = (montoTotal / tipoCambioValor).toFixed(2);
            }
            if (montoCordobasFisico) {
                montoCordobasFisico.value = '';
            }
            
            // Electrónico
            if (montoDolaresElectronico && (!montoDolaresElectronico.value || parseFloat(montoDolaresElectronico.value) === 0)) {
                montoDolaresElectronico.value = (montoTotal / tipoCambioValor).toFixed(2);
            }
            if (montoCordobasElectronico) {
                montoCordobasElectronico.value = '';
            }
        } else if (moneda === 'C$') {
            // Convertir todo a córdobas
            const montoCordobasFisico = document.getElementById('MontoCordobasFisico');
            const montoDolaresFisico = document.getElementById('MontoDolaresFisico');
            const montoCordobasElectronico = document.getElementById('MontoCordobasElectronico');
            const montoDolaresElectronico = document.getElementById('MontoDolaresElectronico');
            
            // Físico
            if (montoCordobasFisico && (!montoCordobasFisico.value || parseFloat(montoCordobasFisico.value) === 0)) {
                montoCordobasFisico.value = montoTotal.toFixed(2);
            }
            if (montoDolaresFisico) {
                montoDolaresFisico.value = '';
            }
            
            // Electrónico
            if (montoCordobasElectronico && (!montoCordobasElectronico.value || parseFloat(montoCordobasElectronico.value) === 0)) {
                montoCordobasElectronico.value = montoTotal.toFixed(2);
            }
            if (montoDolaresElectronico) {
                montoDolaresElectronico.value = '';
            }
        }
        
        // Si es modo Mixto, recalcular el electrónico automáticamente
        if (tipoPago === 'Mixto') {
            this.calcularMontoElectronicoAutomatico();
        }
        
        PagoManager.actualizarEtiquetaMontoRecibido();
        
        if (!tipoPago && moneda) {
            const radioFisico = document.querySelector('input[name="TipoPago"][value="Fisico"]');
            if (radioFisico) {
                radioFisico.checked = true;
                this.mostrarCamposPago();
            }
        } else {
            PagoManager.calcularTotalPago();
        }
    }
};

// ============================================
// UTILIDADES
// ============================================
const Utilidades = {
    habilitarEdicionMonto() {
        const montoInput = document.getElementById('Monto');
        const btnEditar = document.getElementById('btnEditarMonto');
        
        if (montoInput.readOnly) {
            montoInput.readOnly = false;
            montoInput.focus();
            btnEditar.innerHTML = '<svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" /></svg>';
            btnEditar.title = 'Guardar (se llenará automáticamente)';
            document.getElementById('montoInfo').innerHTML = '<span class="text-amber-600 dark:text-amber-400 font-medium">⚠️ Edición manual habilitada. Usa punto (.) para decimales.</span>';
        } else {
            montoInput.readOnly = true;
            btnEditar.innerHTML = '<svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" /></svg>';
            btnEditar.title = 'Hacer editable';
        }
    },
    
    /**
     * Valida y formatea un input numérico en tiempo real
     * Permite solo números, punto y coma
     * Convierte coma a punto automáticamente
     */
    validarInputNumerico(input, event) {
        let valor = input.value;
        
        // Permitir teclas de control (backspace, delete, tab, etc.)
        if (event && (event.key === 'Backspace' || event.key === 'Delete' || event.key === 'Tab' || 
            event.key === 'ArrowLeft' || event.key === 'ArrowRight' || event.key === 'ArrowUp' || event.key === 'ArrowDown' ||
            event.ctrlKey || event.metaKey)) {
            return true;
        }
        
        // Si el campo está vacío, permitir borrar completamente
        if (valor === '' || valor === null || valor === undefined) {
            return true;
        }
        
        // Reemplazar coma por punto
        valor = valor.replace(',', '.');
        
        // Permitir solo números, un punto decimal, y espacios (que eliminaremos)
        valor = valor.replace(/[^\d.]/g, '');
        
        // Eliminar espacios
        valor = valor.replace(/\s/g, '');
        
        // Limitar a un solo punto decimal
        const partes = valor.split('.');
        if (partes.length > 2) {
            valor = partes[0] + '.' + partes.slice(1).join('');
        }
        
        // Limitar decimales a 2 dígitos
        if (partes.length === 2 && partes[1].length > 2) {
            valor = partes[0] + '.' + partes[1].substring(0, 2);
        }
        
        // Actualizar el valor del input
        input.value = valor;
        
        return false; // Prevenir el comportamiento por defecto si es necesario
    },
    
    /**
     * Formatea el valor cuando el usuario sale del campo (blur)
     * Convierte a formato estándar con punto y 2 decimales
     */
    formatearInputNumerico(input) {
        if (!input) return;
        
        let valor = input.value.trim();
        
        // Si está vacío, dejarlo vacío
        if (valor === '' || valor === null || valor === undefined) {
            input.value = '';
            return;
        }
        
        // Reemplazar coma por punto
        valor = valor.replace(',', '.');
        
        // Limpiar caracteres no numéricos excepto punto
        valor = valor.replace(/[^\d.]/g, '');
        
        // Convertir a número
        let numero = parseFloat(valor);
        
        // Si no es un número válido, limpiar el campo
        if (isNaN(numero)) {
            input.value = '';
            return;
        }
        
        // Formatear con 2 decimales máximo (pero no forzar si es entero)
        if (numero % 1 === 0) {
            input.value = numero.toString();
        } else {
            input.value = numero.toFixed(2);
        }
    },
    
    /**
     * Inicializa la validación para todos los inputs numéricos
     */
    inicializarValidacionInputsNumericos() {
        const inputsNumericos = [
            'MontoCordobasFisico',
            'MontoDolaresFisico',
            'MontoRecibidoFisico',
            'MontoCordobasElectronico',
            'MontoDolaresElectronico'
        ];
        
        inputsNumericos.forEach(id => {
            const input = document.getElementById(id);
            if (input) {
                // Validación en tiempo real mientras escribe
                input.addEventListener('input', (e) => {
                    Utilidades.validarInputNumerico(input, e);
                });
                
                // Formateo cuando sale del campo
                input.addEventListener('blur', () => {
                    Utilidades.formatearInputNumerico(input);
                    // Recalcular después de formatear
                    const tipoPago = document.querySelector('input[name="TipoPago"]:checked')?.value;
                    if (tipoPago === 'Mixto' && (id === 'MontoCordobasFisico' || id === 'MontoDolaresFisico')) {
                        CamposPagoManager.calcularMontoElectronicoAutomatico();
                    }
                    PagoManager.calcularTotalPago();
                    if (id.includes('Fisico')) {
                        PagoManager.calcularVueltoFisico();
                    }
                });
                
                // Prevenir pegado de valores inválidos
                input.addEventListener('paste', (e) => {
                    e.preventDefault();
                    const textoPegado = (e.clipboardData || window.clipboardData).getData('text');
                    // Limpiar y validar el texto pegado
                    let valor = textoPegado.replace(',', '.').replace(/[^\d.]/g, '');
                    const partes = valor.split('.');
                    if (partes.length > 2) {
                        valor = partes[0] + '.' + partes.slice(1).join('');
                    }
                    if (partes.length === 2 && partes[1].length > 2) {
                        valor = partes[0] + '.' + partes[1].substring(0, 2);
                    }
                    input.value = valor;
                    // Disparar evento input para recalcular
                    input.dispatchEvent(new Event('input'));
                });
            }
        });
    },
    
    validarMonto(input) {
        let valor = input.value.toString();
        if (valor.includes(',')) {
            valor = valor.replace(',', '.');
            input.value = valor;
        }
        
        valor = valor.replace(/[^\d.]/g, '');
        const partes = valor.split('.');
        if (partes.length > 2) {
            valor = partes[0] + '.' + partes.slice(1).join('');
        }
        
        if (partes.length === 2 && partes[1].length > 2) {
            valor = partes[0] + '.' + partes[1].substring(0, 2);
        }
        
        input.value = valor;
    },
    
    validarFormulario(e) {
        const clienteId = document.getElementById('clienteId').value;
        const montoInput = document.getElementById('Monto');
        const monto = parseFloat(montoInput.value) || 0;
        
        if (!clienteId) {
            e.preventDefault();
            const clienteSearch = document.getElementById('clienteSearch');
            clienteSearch.focus();
            clienteSearch.classList.add('border-red-500', 'ring-2', 'ring-red-500');
            setTimeout(() => {
                clienteSearch.classList.remove('border-red-500', 'ring-2', 'ring-red-500');
            }, 3000);
            Notify.warning('Debe seleccionar un cliente');
            return false;
        }
        
        const facturaId = document.getElementById('FacturaId')?.value;
        const facturaIds = document.getElementById('FacturaIds')?.value;
        
        if (!facturaId && !facturaIds) {
            e.preventDefault();
            Notify.warning('Debe seleccionar al menos una factura');
            return false;
        }
        
        const montoEsperado = parseFloat(montoInput.getAttribute('data-monto-esperado')) || 0;
        if (monto > montoEsperado * 2) {
            e.preventDefault();
            const mensaje = `⚠️ ERROR: El monto ingresado (C$ ${monto.toLocaleString('es-NI', {minimumFractionDigits: 2, maximumFractionDigits: 2})}) es demasiado alto.\n\n` +
                          `El monto esperado es: C$ ${montoEsperado.toLocaleString('es-NI', {minimumFractionDigits: 2, maximumFractionDigits: 2})}\n\n` +
                          `¿Estás seguro de que quieres continuar?`;
            
            // Usar showConfirm si está disponible, sino fallback a confirm
            const confirmado = typeof showConfirm !== 'undefined' 
                ? await showConfirm(mensaje, 'Monto Alto')
                : confirm(mensaje);
                
            if (!confirmado) {
                montoInput.value = montoEsperado.toFixed(2).replace(',', '.');
                montoInput.readOnly = true;
                return false;
            }
        }
        
        montoInput.value = monto.toFixed(2).replace(',', '.');
    }
};

// ============================================
// EXPORTAR FUNCIONES GLOBALES
// ============================================
window.cargarFacturas = (clienteId) => FacturaManager.cargarFacturas(clienteId);
window.mostrarCamposPago = () => CamposPagoManager.mostrarCamposPago();
window.actualizarTipoCambio = () => CamposPagoManager.actualizarTipoCambio();
window.calcularTotalPago = () => PagoManager.calcularTotalPago();
window.calcularVueltoFisico = () => PagoManager.calcularVueltoFisico();
window.actualizarEtiquetaMontoRecibido = () => PagoManager.actualizarEtiquetaMontoRecibido();
window.actualizarMonto = () => PagoManager.actualizarMonto();
window.habilitarEdicionMonto = () => Utilidades.habilitarEdicionMonto();
window.validarMonto = (input) => Utilidades.validarMonto(input);

// ============================================
// INICIALIZACIÓN
// ============================================
document.addEventListener('DOMContentLoaded', function() {
    // Inicializar sistema de pagos
    PagoSystem.init();
    
    // Validar formulario antes de enviar
    document.querySelector('#formPago')?.addEventListener('submit', Utilidades.validarFormulario);
});

