using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Control_flota.Models.Operaciones;
using Microsoft.AspNetCore.Identity;

namespace Control_flota.Models.Login
{
    public class Usuario:IdentityUser
    {
        // Solo agregamos Estado para poder bloquear usuarios
        public bool Estado { get; set; } = true;
        
        // Relación con Conductor (Opcional)
        public int? ConductorId { get; set; }
        public Conductor? Conductor { get; set; }
    }
}