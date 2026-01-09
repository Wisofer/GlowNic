using GlowNic.Models.DTOs.Responses;
using GlowNic.Services.Interfaces;

namespace GlowNic.Services.Implementations;

/// <summary>
/// Servicio para ayuda y soporte
/// </summary>
public class HelpSupportService : IHelpSupportService
{
    public Task<HelpSupportDto> GetHelpSupportAsync()
    {
        var helpSupport = new HelpSupportDto
        {
            Contact = new ContactInfoDto
            {
                Email = "info@cowib.es",
                Phones = new List<string> { "+505 8139569", "+505 82310100" },
                Website = "https://www.cowib.es"
            },
            Faqs = new List<FaqDto>
            {
                new FaqDto
                {
                    Id = 1,
                    Question = "¿Cómo agendo una cita?",
                    Answer = "Puedes agendar una cita escaneando el código QR del salón o visitando su perfil público. Selecciona el servicio, fecha y hora disponible, completa tus datos y confirma la cita.",
                    Order = 1
                },
                new FaqDto
                {
                    Id = 2,
                    Question = "¿Puedo cancelar o modificar una cita?",
                    Answer = "Sí, puedes cancelar o modificar una cita desde la aplicación. Si necesitas ayuda, contacta directamente con el salón.",
                    Order = 2
                },
                new FaqDto
                {
                    Id = 3,
                    Question = "¿Cómo veo mis estadísticas?",
                    Answer = "En la sección de Estadísticas Rápidas puedes ver tus citas del mes, ingresos, clientes atendidos y promedio por cliente. También puedes exportar reportes detallados.",
                    Order = 3
                },
                new FaqDto
                {
                    Id = 4,
                    Question = "¿Cómo configuro mis horarios de trabajo?",
                    Answer = "Ve a la sección 'Horarios de Trabajo' en la aplicación. Puedes activar/desactivar días y configurar las horas de inicio y fin para cada día de la semana.",
                    Order = 4
                },
                new FaqDto
                {
                    Id = 5,
                    Question = "¿Necesito conexión a internet para usar la aplicación?",
                    Answer = "Sí, necesitas conexión a internet para sincronizar tus datos, agendar citas y acceder a todas las funcionalidades de la aplicación.",
                    Order = 5
                }
            }
        };

        return Task.FromResult(helpSupport);
    }
}

