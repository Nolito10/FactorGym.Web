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
});