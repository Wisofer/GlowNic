using GlowNic.Data;
using GlowNic.Models.Entities;
using GlowNic.Services.Interfaces;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GlowNic.Services.Implementations;

/// <summary>
/// Servicio para enviar notificaciones push usando Firebase Cloud Messaging
/// </summary>
public class PushNotificationService : IPushNotificationService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PushNotificationService> _logger;

    public PushNotificationService(
        ApplicationDbContext context,
        ILogger<PushNotificationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SendPushNotificationAsync(
        Template? template, 
        List<Device> devices, 
        IDictionary<string, string>? extraData = null, 
        bool dataOnly = false)
    {
        if (template == null || devices == null || !devices.Any())
        {
            return;
        }

        // Filtrar dispositivos con tokens válidos
        var validDevices = devices
            .Where(d => !string.IsNullOrWhiteSpace(d.FcmToken))
            .ToList();

        if (!validDevices.Any())
        {
            return;
        }

        // Validar URL de imagen si existe
        if (!string.IsNullOrWhiteSpace(template.ImageUrl))
        {
            if (!Uri.TryCreate(template.ImageUrl, UriKind.Absolute, out var imageUri) ||
                (imageUri.Scheme != Uri.UriSchemeHttp && imageUri.Scheme != Uri.UriSchemeHttps))
            {
                template.ImageUrl = null; // Ignorar imagen inválida
            }
        }

        try
        {
            // Verificar que Firebase esté inicializado
            if (FirebaseApp.DefaultInstance == null)
            {
                throw new InvalidOperationException("Firebase no está inicializado. Verifica la configuración en Program.cs");
            }

            if (FirebaseMessaging.DefaultInstance == null)
            {
                throw new InvalidOperationException("FirebaseMessaging no está disponible. Verifica la configuración.");
            }
            
            // Construir notificación
            var notification = new Notification
            {
                Title = template.Title,
                Body = template.Body,
                ImageUrl = template.ImageUrl
            };

            // Construir diccionario de datos
            var data = new Dictionary<string, string>();

            // Agregar datos adicionales
            if (extraData != null)
            {
                foreach (var item in extraData)
                {
                    data[item.Key] = item.Value;
                }
            }

            // Agregar datos de la plantilla
            data["title"] = template.Title;
            data["body"] = template.Body;
            data["type"] = "announcement"; // Tipo por defecto
            if (!string.IsNullOrWhiteSpace(template.ImageUrl))
            {
                data["imageUrl"] = template.ImageUrl;
            }
            if (template.Id > 0)
            {
                data["templateId"] = template.Id.ToString();
            }

            // Construir mensaje base
            var message = new Message
            {
                Notification = dataOnly ? null : notification,
                Data = data ?? new Dictionary<string, string>()
            };

            // Configurar Android
            message.Android = new AndroidConfig
            {
                Priority = Priority.High,
                Notification = dataOnly ? null : new AndroidNotification
                {
                    Title = template.Title,
                    Body = template.Body,
                    ImageUrl = template.ImageUrl,
                    Sound = "default",
                    ChannelId = "default"
                }
            };

            // Configurar iOS (APNS)
            message.Apns = new ApnsConfig
            {
                Headers = new Dictionary<string, string>
                {
                    ["apns-priority"] = "10"
                },
                Aps = new Aps
                {
                    Alert = dataOnly ? null : new ApsAlert
                    {
                        Title = template.Title,
                        Body = template.Body
                    },
                    Sound = dataOnly ? null : "default",
                    Badge = dataOnly ? (int?)null : 1,
                    ContentAvailable = dataOnly
                }
            };

            // Configurar Web
            message.Webpush = new WebpushConfig
            {
                Notification = dataOnly ? null : new WebpushNotification
                {
                    Title = template.Title,
                    Body = template.Body,
                    Icon = template.ImageUrl,
                    Image = template.ImageUrl
                }
            };

            // Dividir tokens en lotes de 500 (límite de FCM)
            const int batchSize = 500;
            var batches = validDevices
                .Select((device, index) => new { device, index })
                .GroupBy(x => x.index / batchSize)
                .Select(g => g.Select(x => x.device).ToList())
                .ToList();

            var totalSent = 0;
            var totalFailed = 0;

            foreach (var batch in batches)
            {
                var tokens = batch.Select(d => d.FcmToken).Where(t => !string.IsNullOrWhiteSpace(t)).ToList();
                
                if (!tokens.Any())
                {
                    continue;
                }
                
                try
                {
                    // Verificar nuevamente que Firebase esté disponible
                    if (FirebaseApp.DefaultInstance == null)
                    {
                        throw new InvalidOperationException("FirebaseApp no está inicializado");
                    }
                    
                    if (FirebaseMessaging.DefaultInstance == null)
                    {
                        throw new InvalidOperationException("FirebaseMessaging no está disponible");
                    }

                    // Validar que message.Data no sea null
                    var messageData = message.Data ?? new Dictionary<string, string>();
                    
                    // Enviar a múltiples dispositivos
                    var multicastMessage = new MulticastMessage
                    {
                        Tokens = tokens,
                        Notification = dataOnly ? null : notification,
                        Data = messageData,
                        Android = message.Android,
                        Apns = message.Apns,
                        Webpush = message.Webpush
                    };

                    // Obtener instancia de Firebase
                    var firebaseMessaging = FirebaseMessaging.DefaultInstance;
                    if (firebaseMessaging == null)
                    {
                        throw new InvalidOperationException("FirebaseMessaging.DefaultInstance es null");
                    }
                    
                    var response = await firebaseMessaging.SendEachForMulticastAsync(multicastMessage);
                    
                    totalSent += response.SuccessCount;
                    totalFailed += response.FailureCount;

                    // Registrar logs de éxito
                    for (int i = 0; i < batch.Count && i < response.Responses.Count; i++)
                    {
                        var device = batch[i];
                        var fcmResponse = response.Responses[i];

                        var log = new NotificationLog
                        {
                            Status = fcmResponse.IsSuccess ? "sent" : "failed",
                            Payload = System.Text.Json.JsonSerializer.Serialize(new
                            {
                                title = template.Title,
                                body = template.Body,
                                imageUrl = template.ImageUrl,
                                extraData = extraData
                            }),
                            SentAt = DateTime.UtcNow,
                            DeviceId = device.Id,
                            TemplateId = template.Id,
                            UserId = device.UserId
                        };

                        _context.NotificationLogs.Add(log);
                    }

                    // Eliminar tokens inválidos
                    if (response?.Responses != null)
                    {
                        for (int i = 0; i < batch.Count && i < response.Responses.Count; i++)
                        {
                            var fcmResponse = response.Responses[i];
                            if (!fcmResponse.IsSuccess && 
                                (fcmResponse.Exception?.MessagingErrorCode == MessagingErrorCode.InvalidArgument ||
                                 fcmResponse.Exception?.MessagingErrorCode == MessagingErrorCode.Unregistered))
                            {
                                var device = batch[i];
                                _context.Devices.Remove(device);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    totalFailed += batch.Count;

                    // Registrar logs de error
                    foreach (var device in batch)
                    {
                        var log = new NotificationLog
                        {
                            Status = "failed",
                            Payload = $"Error: {ex.Message}",
                            SentAt = DateTime.UtcNow,
                            DeviceId = device.Id,
                            TemplateId = template.Id,
                            UserId = device.UserId
                        };
                        _context.NotificationLogs.Add(log);
                    }
                }
            }

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar notificaciones push");
            throw;
        }
    }
}
