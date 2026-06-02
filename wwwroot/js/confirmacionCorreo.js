// wwwroot/js/confirmacionCorreo.js

$(document).ready(function() {
    $('#btnEnviarCorreo').click(function(e) {
        e.preventDefault();
        
        var $btn = $(this);
        var url = $btn.data('url');
        var id = $btn.data('id');
        var token = $('input[name="__RequestVerificationToken"]').val();
        
        // Confirmación antes de enviar
        if (!confirm('¿Enviar constancia al correo del cliente?')) {
            return;
        }
        
        $btn.prop('disabled', true);
        $btn.html('<i class="fa-solid fa-spinner fa-spin me-2"></i> Enviando...');
        
        $.ajax({
            url: url,
            type: 'POST',
            data: {
                id: id,
                __RequestVerificationToken: token
            },
            success: function(response) {
                mostrarMensaje(response.message, response.success);
            },
            error: function() {
                mostrarMensaje('Error al enviar el correo. Intente nuevamente.', false);
            },
            complete: function() {
                $btn.prop('disabled', false);
                $btn.html('<i class="fa-regular fa-envelope me-2"></i> Enviar Correo');
            }
        });
    });
});

function mostrarMensaje(mensaje, esExito) {
    var toast = $('<div>')
        .addClass('toast-mensaje')
        .addClass(esExito ? 'toast-exito' : 'toast-error')
        .html(`<i class="fa-solid ${esExito ? 'fa-check-circle' : 'fa-exclamation-circle'}"></i> ${mensaje}`);
    
    $('body').append(toast);
    
    setTimeout(function() {
        toast.fadeOut(500, function() {
            $(this).remove();
        });
    }, 3000);
}