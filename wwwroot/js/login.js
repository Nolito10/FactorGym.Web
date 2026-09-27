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

    // Funcionalidad para mostrar / ocultar contraseña dinámicamente al escribir
    const btnTogglePassword = document.getElementById('btnTogglePassword');
    const inputPassword = document.getElementById('Password');
    const toggleIcon = document.getElementById('togglePasswordIcon');
    const lockIcon = document.getElementById('lockPasswordIcon');

    if (btnTogglePassword && inputPassword && toggleIcon) {
        // Muestra el ojo solo cuando hay caracteres ingresados, y regresa al candado si está vacío
        function updateToggleVisibility() {
            if (inputPassword.value.length > 0) {
                if (lockIcon) lockIcon.classList.add('d-none');
                btnTogglePassword.classList.remove('d-none');
            } else {
                if (lockIcon) lockIcon.classList.remove('d-none');
                btnTogglePassword.classList.add('d-none');

                // Si el usuario borra todo, restablecer el campo a tipo contraseña
                inputPassword.setAttribute('type', 'password');
                toggleIcon.classList.remove('bi-eye-slash');
                toggleIcon.classList.add('bi-eye');
                btnTogglePassword.setAttribute('title', 'Mostrar contraseña');
                btnTogglePassword.setAttribute('aria-label', 'Mostrar contraseña');
            }
        }

        // Escuchar eventos mientras el usuario tipea o modifica el campo
        inputPassword.addEventListener('input', updateToggleVisibility);
        inputPassword.addEventListener('change', updateToggleVisibility);

        // Verificar estado inicial por si el navegador recuerda o autocompleta la contraseña
        updateToggleVisibility();
        setTimeout(updateToggleVisibility, 150);

        // Alternar entre ver y ocultar contraseña al presionar el ojo
        btnTogglePassword.addEventListener('click', function (e) {
            e.preventDefault();

            const isPassword = inputPassword.getAttribute('type') === 'password';

            if (isPassword) {
                inputPassword.setAttribute('type', 'text');
                toggleIcon.classList.remove('bi-eye');
                toggleIcon.classList.add('bi-eye-slash');
                btnTogglePassword.setAttribute('title', 'Ocultar contraseña');
                btnTogglePassword.setAttribute('aria-label', 'Ocultar contraseña');
            } else {
                inputPassword.setAttribute('type', 'password');
                toggleIcon.classList.remove('bi-eye-slash');
                toggleIcon.classList.add('bi-eye');
                btnTogglePassword.setAttribute('title', 'Mostrar contraseña');
                btnTogglePassword.setAttribute('aria-label', 'Mostrar contraseña');
            }

            // Mantener el foco en el campo y conservar el cursor al final del texto
            inputPassword.focus();
            const valLength = inputPassword.value.length;
            if (inputPassword.setSelectionRange) {
                inputPassword.setSelectionRange(valLength, valLength);
            }
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