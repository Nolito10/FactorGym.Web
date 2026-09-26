document.addEventListener("DOMContentLoaded", function () {
    const sidebarCollapseBtn = document.getElementById('sidebarCollapse');
    const sidebar = document.getElementById('sidebar');
    const content = document.getElementById('content');

    if (sidebarCollapseBtn) {
        sidebarCollapseBtn.addEventListener('click', function () {
            sidebar.classList.toggle('active');

            if (window.innerWidth <= 768) {
                content.classList.toggle('active');
            }
        });
    }

    // Gestión dinámica del bordeado activo en todo el menú
    const menuItems = document.querySelectorAll('#sidebar .menu-item');
    const submenus = document.querySelectorAll('#sidebar .collapse');
    const subMenuItems = document.querySelectorAll('#sidebar .sub-menu-item');

    menuItems.forEach(item => {
        item.addEventListener('click', function () {
            menuItems.forEach(m => m.classList.remove('active'));
            this.classList.add('active');
        });
    });

    // Acordeón automático: al abrir un menú, cerrar los demás y activar el bordeado en el elemento actual
    submenus.forEach(menu => {
        menu.addEventListener('show.bs.collapse', function () {
            submenus.forEach(other => {
                if (other !== menu && other.classList.contains('show')) {
                    const bsCollapse = bootstrap.Collapse.getInstance(other) || new bootstrap.Collapse(other, { toggle: false });
                    bsCollapse.hide();
                }
            });

            const parentToggle = document.querySelector(`[href="#${menu.id}"]`);
            if (parentToggle) {
                menuItems.forEach(m => m.classList.remove('active'));
                parentToggle.classList.add('active');
            }
        });
    });

    // Ocultar número del botón principal al abrir Equipamiento y mostrarlo al cerrar
    const equiposSubmenu = document.getElementById('equiposSubmenu');
    const badgeParent = document.querySelector('.badge-alertas-parent');
    if (equiposSubmenu && badgeParent) {
        equiposSubmenu.addEventListener('show.bs.collapse', function () {
            badgeParent.classList.add('d-none');
        });
        equiposSubmenu.addEventListener('hide.bs.collapse', function () {
            const menuItem = equiposSubmenu.previousElementSibling;
            if (!menuItem || !menuItem.classList.contains('active')) {
                badgeParent.classList.remove('d-none');
            }
        });
    }

    // Gestión de interacción en submenús
    subMenuItems.forEach(link => {
        link.addEventListener('click', function () {
            // Remover active-sub de todos los items para evitar selección múltiple
            subMenuItems.forEach(item => item.classList.remove('active-sub'));
            this.classList.add('active-sub');
            this.blur(); // Remueve el foco nativo para que no retenga el estado hover/focus
        });
    });

    // Validar que solo haya un elemento active-sub (limpieza de inconsistencias)
    const activeSubItems = document.querySelectorAll('#sidebar .sub-menu-item.active-sub');
    const currentUrl = (window.location.pathname + window.location.search).toLowerCase();
    const currentPath = window.location.pathname.toLowerCase();

    if (activeSubItems.length > 1) {
        // Si hay más de un sub-item activo (ej. index vs index?filtro=alertas o subruta),
        // dar prioridad a coincidencia exacta de URL completa, luego exacta de path
        let exactMatch = Array.from(activeSubItems).find(i => (i.getAttribute('href') || '').toLowerCase() === currentUrl);
        if (!exactMatch) {
            exactMatch = Array.from(activeSubItems).find(i => (i.getAttribute('href') || '').toLowerCase().split('?')[0] === currentPath);
        }
        activeSubItems.forEach(i => {
            if (i !== exactMatch) {
                i.classList.remove('active-sub');
            }
        });
    } else if (activeSubItems.length === 0) {
        // Si ningún item vino marcado como active-sub desde Razor, buscar coincidencia exacta
        let matched = Array.from(subMenuItems).find(i => (i.getAttribute('href') || '').toLowerCase() === currentUrl);
        if (!matched) {
            matched = Array.from(subMenuItems).find(i => (i.getAttribute('href') || '').toLowerCase().split('?')[0] === currentPath);
        }
        if (matched) {
            matched.classList.add('active-sub');
            const parentCollapse = matched.closest('.collapse');
            if (parentCollapse) {
                if (!parentCollapse.classList.contains('show')) {
                    const bsCollapse = bootstrap.Collapse.getInstance(parentCollapse) || new bootstrap.Collapse(parentCollapse, { toggle: false });
                    bsCollapse.show();
                }
                const parentToggle = document.querySelector(`[href="#${parentCollapse.id}"]`);
                if (parentToggle) {
                    menuItems.forEach(m => m.classList.remove('active'));
                    parentToggle.classList.add('active');
                }
            }
        }
    }
});