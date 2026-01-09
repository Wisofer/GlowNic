/**
 * Layout Notifications
 * Convertir TempData a notificaciones con sonidos
 */

document.addEventListener('DOMContentLoaded', function() {
    // Detectar notificaciones de TempData (toasts de DaisyUI)
    const successMsg = document.getElementById('successMessage');
    const errorMsg = document.getElementById('errorMessage');
    const warningMsg = document.getElementById('warningMessage');
    
    if (successMsg) {
        const message = successMsg.querySelector('span')?.textContent || successMsg.textContent;
        if (message) {
            Notify.success(message);
            successMsg.style.display = 'none';
        }
    }
    
    if (errorMsg) {
        const message = errorMsg.querySelector('span')?.textContent || errorMsg.textContent;
        if (message) {
            Notify.error(message);
            errorMsg.style.display = 'none';
        }
    }
    
    if (warningMsg) {
        const message = warningMsg.querySelector('span')?.textContent || warningMsg.textContent;
        if (message) {
            Notify.warning(message);
            warningMsg.style.display = 'none';
        }
    }
});

