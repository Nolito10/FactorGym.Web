document.addEventListener('DOMContentLoaded', () => {
    const loginForm = document.getElementById('loginForm');
    const btnSubmit = document.getElementById('btnSubmit');

    if (loginForm) {
        loginForm.addEventListener('submit', function (e) {
            const user = document.getElementById('Username').value.trim();
            const pass = document.getElementById('Password').value.trim();

            // 1. Validación de campos vacíos
            if (!user || !pass) {
                e.preventDefault(); // Detenemos el envío al servidor
                showAlert('¡Atención!', 'Por favor, ingresa tu Nombre de Usuario y Contraseña para continuar.');
                return;
            }

            // 2. Si todo está correcto, mostramos la animación de carga en el botón
            btnSubmit.disabled = true;
            btnSubmit.innerHTML = '<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span>';
        });
    }
});

// Funciones globales para manejar el Modal de Alerta
function showAlert(title, message) {
    const alertOverlay = document.getElementById('customAlert');
    document.getElementById('alertTitle').innerText = title;
    document.getElementById('alertMessage').innerText = message;

    // Mostramos el modal
    alertOverlay.classList.add('show');
}

function closeAlert() {
    const alertOverlay = document.getElementById('customAlert');
    alertOverlay.classList.remove('show');
}