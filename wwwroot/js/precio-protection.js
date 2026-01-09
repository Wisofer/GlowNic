/**
 * Sistema de Protección para Inputs Numéricos
 * Previene que los navegadores redondeen o modifiquen valores
 * Cubre: Precios, Montos, Cantidades, Stock, etc.
 */

document.addEventListener('DOMContentLoaded', function() {
    let inputsProtegidos = 0;
    
    // =========================================
    // 1. PROTEGER INPUTS DE PRECIO (decimales)
    // =========================================
    const precioInputs = document.querySelectorAll('input[type="number"][step="0.01"]');
    
    precioInputs.forEach(input => {
        protegerInputPrecio(input);
        inputsProtegidos++;
    });
    
    // =========================================
    // 2. PROTEGER INPUTS POR NOMBRE/ID
    // =========================================
    const selectoresPrecio = [
        'input[name*="precio" i]',
        'input[name*="monto" i]',
        'input[name*="price" i]',
        'input[name*="amount" i]',
        'input[id*="precio" i]',
        'input[id*="monto" i]',
        'input[id*="price" i]',
        'input[id*="amount" i]',
        'input[name="Monto"]',
        'input[id="Monto"]'
    ];
    
    selectoresPrecio.forEach(selector => {
        document.querySelectorAll(selector).forEach(input => {
            if (input.type === 'number' && !input.hasAttribute('data-protegido')) {
                protegerInputPrecio(input);
                inputsProtegidos++;
            }
        });
    });
    
    // =========================================
    // 3. PROTEGER INPUTS DE CANTIDAD/STOCK (enteros)
    // =========================================
    const selectoresCantidad = [
        'input[name*="cantidad" i]',
        'input[name*="stock" i]',
        'input[name*="quantity" i]',
        'input[id*="cantidad" i]',
        'input[id*="stock" i]',
        'input[name="Stock"]',
        'input[name="StockMinimo"]',
        'input[name="Kilometraje"]',
        'input[name="Ano"]'
    ];
    
    selectoresCantidad.forEach(selector => {
        document.querySelectorAll(selector).forEach(input => {
            if (input.type === 'number' && !input.hasAttribute('data-protegido')) {
                protegerInputCantidad(input);
                inputsProtegidos++;
            }
        });
    });
    
    // =========================================
    // 4. PROTEGER TODOS LOS OTROS type="number"
    // =========================================
    const todosNumbers = document.querySelectorAll('input[type="number"]');
    todosNumbers.forEach(input => {
        if (!input.hasAttribute('data-protegido')) {
            // Por defecto, proteger como precio si no se especificó
            protegerInputGenerico(input);
            inputsProtegidos++;
        }
    });
    
});

/**
 * Proteger input de PRECIO (permite decimales)
 */
function protegerInputPrecio(input) {
    input.setAttribute('data-protegido', 'true');
    input.setAttribute('step', 'any');
    
    // Guardar valor original
    input.addEventListener('focus', function() {
        this.dataset.valorOriginal = this.value;
    });
    
    // Validar y formatear al salir
    input.addEventListener('blur', function() {
        let valor = this.value.trim();
        
        if (valor === '') return;
        
        // Reemplazar coma por punto (normalizar separador decimal)
        valor = valor.replace(',', '.');
        
        // Eliminar cualquier separador de miles (puntos que no sean el decimal)
        // Si hay múltiples puntos, solo el último es el decimal
        const partes = valor.split('.');
        if (partes.length > 2) {
            // Hay múltiples puntos, unir todo excepto el último y usar el último como decimal
            valor = partes.slice(0, -1).join('') + '.' + partes[partes.length - 1];
        }
        
        const numero = parseFloat(valor);
        
        if (isNaN(numero) || numero < 0) {
            this.value = this.dataset.valorOriginal || '';
            if (typeof Notify !== 'undefined') {
                Notify.warning('Por favor ingrese un precio válido');
            }
            return;
        }
        
        // Formatear a 2 decimales (siempre con punto como separador decimal)
        this.value = numero.toFixed(2);
    });
    
    // Asegurar formato correcto antes de enviar el formulario
    const form = input.closest('form');
    if (form) {
        form.addEventListener('submit', function(e) {
            let valor = input.value.trim();
            
            if (valor !== '') {
                // Normalizar: reemplazar coma por punto
                valor = valor.replace(',', '.');
                
                // Eliminar separadores de miles si existen
                const partes = valor.split('.');
                if (partes.length > 2) {
                    valor = partes.slice(0, -1).join('') + '.' + partes[partes.length - 1];
                }
                
                const numero = parseFloat(valor);
                if (!isNaN(numero) && numero >= 0) {
                    // Asegurar que el valor enviado siempre tenga punto como separador decimal
                    input.value = numero.toFixed(2);
                }
            }
        });
    }
    
    // Prevenir caracteres inválidos
    input.addEventListener('keydown', function(e) {
        // Permitir: backspace, delete, tab, escape, enter
        if ([46, 8, 9, 27, 13].indexOf(e.keyCode) !== -1 ||
            // Permitir: Ctrl+A, Ctrl+C, Ctrl+V, Ctrl+X
            (e.keyCode === 65 && e.ctrlKey === true) ||
            (e.keyCode === 67 && e.ctrlKey === true) ||
            (e.keyCode === 86 && e.ctrlKey === true) ||
            (e.keyCode === 88 && e.ctrlKey === true) ||
            // Permitir: home, end, left, right
            (e.keyCode >= 35 && e.keyCode <= 39)) {
            return;
        }
        
        // Permitir: números, punto decimal y coma
        if ((e.shiftKey || (e.keyCode < 48 || e.keyCode > 57)) && 
            (e.keyCode < 96 || e.keyCode > 105) && 
            e.keyCode !== 190 && e.keyCode !== 110 && e.keyCode !== 188) {
            e.preventDefault();
        }
    });
    
    // Prevenir cambios con rueda del mouse
    input.addEventListener('wheel', function(e) {
        e.preventDefault();
        this.blur();
    }, { passive: false });
}

/**
 * Proteger input de CANTIDAD (solo enteros)
 */
function protegerInputCantidad(input) {
    input.setAttribute('data-protegido', 'true');
    input.setAttribute('step', '1');
    
    // Guardar valor original
    input.addEventListener('focus', function() {
        this.dataset.valorOriginal = this.value;
    });
    
    // Validar al salir (solo enteros)
    input.addEventListener('blur', function() {
        let valor = this.value.trim();
        
        if (valor === '') return;
        
        const numero = parseInt(valor);
        const min = parseInt(this.getAttribute('min')) || 0;
        const max = parseInt(this.getAttribute('max'));
        
        if (isNaN(numero) || numero < min || (max && numero > max)) {
            this.value = this.dataset.valorOriginal || min || '';
            if (typeof Notify !== 'undefined') {
                Notify.warning('Por favor ingrese una cantidad válida');
            }
            return;
        }
        
        // Asegurar que sea entero
        this.value = Math.floor(numero);
    });
    
    // Prevenir decimales
    input.addEventListener('keydown', function(e) {
        // Permitir: backspace, delete, tab, escape, enter
        if ([46, 8, 9, 27, 13].indexOf(e.keyCode) !== -1 ||
            // Permitir: Ctrl+A, Ctrl+C, Ctrl+V, Ctrl+X
            (e.keyCode === 65 && e.ctrlKey === true) ||
            (e.keyCode === 67 && e.ctrlKey === true) ||
            (e.keyCode === 86 && e.ctrlKey === true) ||
            (e.keyCode === 88 && e.ctrlKey === true) ||
            // Permitir: home, end, left, right
            (e.keyCode >= 35 && e.keyCode <= 39)) {
            return;
        }
        
        // Solo números (NO punto ni coma)
        if ((e.shiftKey || (e.keyCode < 48 || e.keyCode > 57)) && 
            (e.keyCode < 96 || e.keyCode > 105)) {
            e.preventDefault();
        }
    });
    
    // Prevenir cambios con rueda del mouse
    input.addEventListener('wheel', function(e) {
        e.preventDefault();
        this.blur();
    }, { passive: false });
}

/**
 * Proteger input GENÉRICO (detecta si es decimal o entero)
 */
function protegerInputGenerico(input) {
    input.setAttribute('data-protegido', 'true');
    
    // Detectar si debe ser decimal o entero por el step
    const step = input.getAttribute('step');
    const esDecimal = step && step !== '1' && step !== '';
    
    if (esDecimal) {
        input.setAttribute('step', 'any');
        protegerInputPrecio(input);
    } else {
        protegerInputCantidad(input);
    }
}

/**
 * Función auxiliar para validar precio antes de enviar formulario
 */
function validarPrecio(valor) {
    if (valor === '' || valor === null || valor === undefined) {
        return true;
    }
    
    const numero = parseFloat(String(valor).replace(',', '.'));
    return !isNaN(numero) && numero >= 0;
}

/**
 * Función auxiliar para formatear precio
 */
function formatearPrecio(valor) {
    if (valor === '' || valor === null || valor === undefined) {
        return '';
    }
    
    const numero = parseFloat(String(valor).replace(',', '.'));
    if (isNaN(numero)) {
        return '';
    }
    
    return numero.toFixed(2);
}

/**
 * Función auxiliar para validar cantidad
 */
function validarCantidad(valor, min = 0, max = null) {
    if (valor === '' || valor === null || valor === undefined) {
        return true;
    }
    
    const numero = parseInt(valor);
    if (isNaN(numero) || numero < min) {
        return false;
    }
    
    if (max !== null && numero > max) {
        return false;
    }
    
    return true;
}

