document.addEventListener('DOMContentLoaded', function() {
    const clienteSelect = document.getElementById('clienteId');
    if (clienteSelect) {
        clienteSelect.addEventListener('change', function() {
            document.getElementById('formFiltros')?.submit();
        });
    }
});
