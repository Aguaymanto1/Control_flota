using System;
using System.ComponentModel.DataAnnotations;

namespace Control_flota.Models.Operaciones
{
    public class Inspeccion
    {
        public int Id { get; set; }

        public string Placa { get; set; } = string.Empty;

        public string Estado { get; set; } = string.Empty;

        public string? Observaciones { get; set; }

        public DateTime Fecha { get; set; } = DateTime.Now;
    }
}