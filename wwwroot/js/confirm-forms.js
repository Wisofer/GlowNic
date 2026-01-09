/**
 * Sistema para reemplazar confirm() en forms con onsubmit
 * Intercepta todos los forms con confirmación y usa el sistema bonito
 */

document.addEventListener('DOMContentLoaded', function() {
    // Buscar todos los forms con onsubmit que usan confirm()
    const forms = document.querySelectorAll('form[onsubmit*="confirm"]');
    
    forms.forEach(form => {
        const originalOnsubmit = form.getAttribute('onsubmit');
        
        // Extraer el mensaje del confirm
        const confirmMatch = originalOnsubmit.match(/confirm\(['"](.+?)['"]\)/);
        if (!confirmMatch) return;
        
        const mensaje = confirmMatch[1];
        
        // Remover el onsubmit original
        form.removeAttribute('onsubmit');
        
        // Agregar event listener con confirmación bonita
        form.addEventListener('submit', async function(e) {
            e.preventDefault();
            
            // Mostrar confirmación bonita
            const confirmado = await showConfirm(mensaje, 'Confirmar Acción');
            
            if (confirmado) {
                // Si confirma, remover este listener y enviar el form
                form.removeEventListener('submit', arguments.callee);
                form.submit();
            }
        });
    });
    
});

