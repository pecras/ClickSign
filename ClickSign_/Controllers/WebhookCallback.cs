using Amazon.S3;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text;
using System.Security.Cryptography;
using SignInClick.Services;

namespace SignInClick.Controllers
{
    [ApiController]
    [Route("/api/documents")]
    public class WebhookController : ControllerBase
    {
        private readonly ILogger<WebhookController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;
        private readonly AwsS3Handler _s3Handler; // Injetando o serviço AwsS3Handler

        public WebhookController(ILogger<WebhookController> logger, IWebHostEnvironment environment, IConfiguration configuration, AwsS3Handler s3Handler)
        {
            _logger = logger;
            _configuration = configuration;
            _environment = environment;
            _s3Handler = s3Handler;
        }

        [HttpPost]
        public IActionResult ReceiveWebhook([FromBody] JsonElement webhookPayload)
        {
            // Obtém o segredo HMAC do appsettings.json
            var HMACSHA256Secret = _configuration["Clicksign:HMACSHA256Secret"];

            // Logando o payload recebido
            var payloadJson = webhookPayload.GetRawText();

            // Verifica se o cabeçalho 'Content-Hmac' está presente
            if (!Request.Headers.ContainsKey("Content-Hmac"))
            {
                _logger.LogWarning("Header 'Content-Hmac' ausente.");
                return Unauthorized(new { message = "Header 'Content-Hmac' ausente" });
            }

            // Pega o valor do cabeçalho 'Content-Hmac'
            var receivedHmac = Request.Headers["Content-Hmac"].ToString();

            // Calcula o HMAC com base no corpo da requisição e no segredo compartilhado
            var computedHmac = ComputeHmacSha256(payloadJson, HMACSHA256Secret);
            _logger.LogInformation("HMAC calculado: {ComputedHmac}", computedHmac);

            // Verifica se o HMAC recebido corresponde ao HMAC calculado
            if (computedHmac != receivedHmac)
            {
                _logger.LogWarning("HMAC inválido. Acesso não autorizado.");
                return Unauthorized(new { message = "HMAC inválido" });
            }

            try
            {
                _logger.LogInformation("Recebendo webhook de documento da Clicksign");

                // Extrai o evento principal
                var mainEvent = webhookPayload.GetProperty("event");
                var eventName = mainEvent.GetProperty("name").GetString();
                _logger.LogInformation("Evento recebido: {EventName}", eventName);

                // Extrai o documento
                var document = webhookPayload.GetProperty("document");
                var documentKey = document.GetProperty("key").GetString();
                _logger.LogInformation("Documento recebido com chave: {DocumentKey}", documentKey);

                // Processa o evento baseado no nome
                switch (eventName)
                {
                    case "add_signer":
                        ProcessAddSigner(mainEvent, document);
                        break;
                    case "sign":
                        ProcessSignEvent(mainEvent, document);
                        break;
                    case "upload":
                        ProcessUploadEvent(mainEvent, document);
                        break;
                    case "document_closed":
                        EventAutoClose(mainEvent, document);
                        break;
                    case "cancel":
                        EventAutoClose(mainEvent, document);
                        break;
                    case "custom":
                        EventCustom(mainEvent, document);
                        break;
                    default:
                        _logger.LogWarning($"Evento desconhecido: {eventName}");
                        break;
                }

                return Ok(new { message = "Webhook processado com sucesso" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erro ao processar o webhook: {ex.Message}");
                return StatusCode(500, new { message = "Erro ao processar o webhook", error = ex.Message });
            }
        }

        // Método para calcular o HMAC SHA256
        private string ComputeHmacSha256(string message, string secret)
        {
            var encoding = new UTF8Encoding();
            byte[] keyByte = encoding.GetBytes(secret);
            byte[] messageBytes = encoding.GetBytes(message);

            using (var hmacsha256 = new HMACSHA256(keyByte))
            {
                byte[] hashMessage = hmacsha256.ComputeHash(messageBytes);
                return $"sha256={BitConverter.ToString(hashMessage).Replace("-", "").ToLower()}";
            }
        }

        // Processa o evento "add_signer"
        private void ProcessAddSigner(JsonElement eventData, JsonElement document)
        {
            var signer = eventData.GetProperty("data").GetProperty("signers")[0];
            var signerEmail = signer.GetProperty("email").GetString();
            var signerName = eventData.GetProperty("data").GetProperty("user").GetProperty("name").GetString();
            var documentKey = document.GetProperty("key").GetString();

            _logger.LogInformation($"Adicionando signatário: {signerName} ({signerEmail}) no documento {documentKey}");
            // Implementar a lógica para processar o signatário
        }

        // Processa o evento "sign"
        private void ProcessSignEvent(JsonElement eventData, JsonElement document)
        {
            var documentKey = document.GetProperty("key").GetString();
            _logger.LogInformation($"Documento {documentKey} foi assinado.");
            // Implementar a lógica de assinatura
        }

        // Processa o evento "upload"
        private void ProcessUploadEvent(JsonElement eventData, JsonElement document)
        {
            var documentKey = document.GetProperty("key").GetString();
            _logger.LogInformation($"Documento {documentKey} foi carregado.");
            // Implementar a lógica de upload
        }


        private void EventCustom(JsonElement eventData, JsonElement document)
        {
            var documentKey = document.GetProperty("key").GetString();
            _logger.LogInformation($"Documento {documentKey} foi carregado.");
            // Implementar a lógica de upload
        }

        // Processa o evento "auto_close"
        private async void EventAutoClose(JsonElement eventData, JsonElement document)
        {
            try
            {
                var documentKey = document.GetProperty("key").GetString();
                _logger.LogInformation($"Documento {documentKey} foi fechado automaticamente.");

                // Extrair o nome do documento
                var documentName = document.GetProperty("name").GetString();

                // Extrair o conteúdo base64 do documento
                var documentContentBase64 = document.GetProperty("content").GetString(); // Verifique se 'content' é o campo correto

                // Converter o conteúdo Base64 para bytes
                byte[] fileBytes = Convert.FromBase64String(documentContentBase64);
                using (var stream = new MemoryStream(fileBytes))
                {
                    // Fazer upload para o S3 usando AwsS3Handler
                    var bucketName = _configuration["AWS:S3Bucket"];
                    await _s3Handler.Upload(stream, bucketName, documentName);
                }

                // Checar se houve falha no upload
                if (_s3Handler.IsValid)
                {
                    _logger.LogInformation($"Documento {documentName} foi enviado com sucesso para o S3.");
                }
                else
                {
                    foreach (var notification in _s3Handler.Notifications)
                    {
                        _logger.LogError($"Erro no upload para S3: {notification.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erro ao processar o evento 'auto_close': {ex.Message}");
            }
        }
    }
}
