using System.Net.Http.Headers;
using System.Text;
using SignInClick.DTOS;
using System.Text.Json;


namespace SignInClick.Services
{
    public class ProcessClickSign
    {
        private readonly HttpClient _httpClient;
        private readonly string _accessToken;

         private readonly string _clickSignUrl;


        public ProcessClickSign(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _accessToken = configuration["Clicksign:AccessToken"] ?? string.Empty;
            _clickSignUrl = configuration["Clicksign:URl"] ?? string.Empty;
        }

        public async Task<string> CreateEnvelopeAsync( string name)
  {
    var url = $"{_clickSignUrl}/api/v3/envelopes";

    var requestBody = new
    {
        data = new
        {
            type = "envelopes",
            attributes = new
            {
                name = name,
                // Outros atributos podem ser adicionados aqui
            }
        }
    };

    var json = JsonSerializer.Serialize(requestBody);
    var content = new StringContent(json, Encoding.UTF8)
    {
        Headers = { ContentType = new MediaTypeHeaderValue("application/vnd.api+json") }
    };

    var requestMessage = new HttpRequestMessage(HttpMethod.Post, url)
    {
        Content = content
    };

    requestMessage.Headers.Add("Authorization", _accessToken);
    requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.api+json"));

    var response = await _httpClient.SendAsync(requestMessage);
    var result = await response.Content.ReadAsStringAsync();
   
    if (response.IsSuccessStatusCode)
    {
     using (JsonDocument doc = JsonDocument.Parse(result))
      {
        var root = doc.RootElement;
        if (root.TryGetProperty("data", out var dataElement) && 
            dataElement.TryGetProperty("id", out var idElement))
        {
            return idElement.GetString() ?? string.Empty; // Retorna o ID como string
        }
    }

          return "ID do envelope não encontrado."; 
        
    }
    else
    {
        return $"Erro ao criar o envelope: {result}";
    }
}





        public async Task<string> UpdateEnvelopeStatus(string envelopeId)
        {
            var url = $"{_clickSignUrl}/api/v3/envelopes/{envelopeId}";

            // Montar o corpo da requisição
            var requestBody = new
            {
                data = new
                {
                    id = envelopeId,
                    type = "envelopes",
                    attributes = new
                    {
                        status = "running", // Ativando o envelope
                      
                    }
                }
            };


            var json = System.Text.Json.JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/vnd.api+json");
            content.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.api+json");

            // Criar a requisição HTTP
            var requestMessage = new HttpRequestMessage(HttpMethod.Patch, url)
            {
                Content = content
            };
            requestMessage.Headers.Add("Authorization", _accessToken);
            requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.api+json"));

            // Enviar a requisição
            var response = await _httpClient.SendAsync(requestMessage);

            // Verificar se a resposta foi bem-sucedida
            var result = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Envelope ativo");
            return response.IsSuccessStatusCode ? result : $"Erro ao ativar o envelope: {result}";
        }



        public async Task<string> ProcessDocument(string envelopeId, IFormFile file )
        {
            if (file == null || file.Length == 0)
            {
                return "Nenhum arquivo foi enviado.";
            }

            var url = $"{_clickSignUrl}/api/v3/envelopes/{envelopeId}/documents";

            // Ler e converter o arquivo para Base64
            string contentBase64;
            using (var memoryStream = new MemoryStream())
            {
                await file.CopyToAsync(memoryStream);
                contentBase64 = Convert.ToBase64String(memoryStream.ToArray());
            }

            // Montar o corpo da requisição
            var requestBody = new
            {
                data = new
                {
                    type = "documents",
                    attributes = new
                    {
                        filename = file.FileName,
                        content_base64 = $"data:application/pdf;base64,{contentBase64}",
                    }
                }
            };

            var json = System.Text.Json.JsonSerializer.Serialize(requestBody);
           var content = new StringContent(json, Encoding.UTF8);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.api+json");

            

            // Criar a requisição HTTP
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = content
            };
            requestMessage.Headers.Add("Authorization", _accessToken);
            requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.api+json"));

            // Enviar a requisição
            var response = await _httpClient.SendAsync(requestMessage);

            // Verificar se a resposta foi bem-sucedida
            var result = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                using (JsonDocument doc = JsonDocument.Parse(result))
                {
                    var root = doc.RootElement;
                    if (root.TryGetProperty("data", out var dataElement) &&
                        dataElement.TryGetProperty("id", out var idElement))
                    {
                        return idElement.GetString() ?? string.Empty; // Retorna o ID como string
                    }
                }

                return "ID do envelope não encontrado.";

            }
            else
            {
                return $"Erro ao criar o envelope: {result}";
            }
        }

        public async Task<string> CreateSigner(string envelopeId, string signer, string emailSigner)
        {
            var url = $"{_clickSignUrl}/api/v3/envelopes/{envelopeId}/signers";

            // Montar o corpo da requisição
            var requestBody = new
            {
                data = new
                {
                    type = "signers",
                    attributes = new
                    {
                        
                      name = signer,
                      email = emailSigner 
                       
                    }
                }
            };

            var json = System.Text.Json.JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/vnd.api+json");

             content.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.api+json");

            // Criar a requisição HTTP
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = content
            };
            requestMessage.Headers.Add("Authorization", _accessToken);
            requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.api+json"));

            // Enviar a requisição
            var response = await _httpClient.SendAsync(requestMessage);

            // Verificar se a resposta foi bem-sucedida
            var result = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"{result}"); if (response.IsSuccessStatusCode)
            {
                using (JsonDocument doc = JsonDocument.Parse(result))
                {
                    var root = doc.RootElement;
                    if (root.TryGetProperty("data", out var dataElement) &&
                        dataElement.TryGetProperty("id", out var idElement))
                    {
                        return idElement.GetString() ?? string.Empty; // Retorna o ID como string
                    }
                }

                return "ID do envelope não encontrado.";

            }
            else
            {
                return $"Erro ao criar o envelope: {result}";
            }
        }

      
        public async Task<string> AgreeRequisito(string envelopeId, string documentId, string signerId, string role)
        {
            var url = $"{_clickSignUrl}/api/v3/envelopes/{envelopeId}/requirements";

            // Montar o corpo da requisição
            var requestBody = new
            {
                data = new
                {
                    type = "requirements",
                    attributes = new
                    {
                        action = "agree",
                        role = role
                    },
                    relationships = new
                    {
                        document = new
                        {
                            data = new
                            {
                                type = "documents",
                                id = documentId
                            }
                        },
                        signer = new
                        {
                            data = new
                            {
                                type = "signers",
                                id = signerId
                            }
                        }
                    }
                }
            };

            var json = System.Text.Json.JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/vnd.api+json");
            content.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.api+json");

            // Criar a requisição HTTP
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = content
            };
            requestMessage.Headers.Add("Authorization", _accessToken);
            requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.api+json"));

            // Enviar a requisição
            var response = await _httpClient.SendAsync(requestMessage);

            // Verificar se a resposta foi bem-sucedida
            var result = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Requisito Adicionado");
            return response.IsSuccessStatusCode ? result : $"Erro ao criar o requisito: {result}";
        }

        // Método para criar ambos os requisitos
        public async Task<string> AgreeBothRequisitos(string envelopeId, string documentId, string signer1Id, string signer2Id)
        {
            // Criar requisito para o contractee
            var result1 = await AgreeRequisito(envelopeId, documentId, signer1Id, "contractee");

            // Verificar se o primeiro foi bem-sucedido antes de criar o segundo
            if (!result1.StartsWith("Erro"))
            {
                // Criar requisito para o contractor
                var result2 = await AgreeRequisito(envelopeId, documentId, signer2Id, "contractor");
                return result2; // Retorna o resultado do segundo requisito
            }

            return result1; // Retorna erro do primeiro requisito se houve falha
        }





        public async Task<string> CreateRequisito(string envelopeId, string documentId, string signerId)
        {
            var url = $"{_clickSignUrl}/api/v3/envelopes/{envelopeId}/requirements";

            // Montar o corpo da requisição
            var requestBody = new
            {
                data = new
                {
                    type = "requirements",
                    attributes = new
                    {
                        action = "provide_evidence",
                        auth = "email",

                    },
                    relationships = new
                    {
                        document = new
                        {
                            data = new
                            {
                                type = "documents",
                                id = documentId
                            }
                        },
                        signer = new
                        {
                            data = new
                            {
                                type = "signers",
                                id = signerId
                            }
                        }
                    }
                }
            };

            var json = System.Text.Json.JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/vnd.api+json");
            content.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.api+json");

            // Criar a requisição HTTP
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = content
            };
            requestMessage.Headers.Add("Authorization", _accessToken);
            requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.api+json"));

            // Enviar a requisição
            var response = await _httpClient.SendAsync(requestMessage);

            // Verificar se a resposta foi bem-sucedida
            var result = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Requisito criado");
            return response.IsSuccessStatusCode ? result : $"Erro ao criar o requisito: {result}";
        }




        // Método para criar ambos os requisitos
        public async Task<string> CreateBothRequisitos(string envelopeId, string documentId, string signer1Id, string signer2Id)
        {
            // Criar requisito para o contractee
            var result1 = await CreateRequisito(envelopeId, documentId, signer1Id);

            // Verificar se o primeiro foi bem-sucedido antes de criar o segundo
            if (!result1.StartsWith("Erro"))
            {
                // Criar requisito para o contractor
                var result2 = await CreateRequisito(envelopeId, documentId, signer2Id);
                return result2; // Retorna o resultado do segundo requisito
            }

            return result1; // Retorna erro do primeiro requisito se houve falha
        }



        public async Task<string> CreateEnvelopeNotification(string envelopeId)
        {
            var url = $"{_clickSignUrl}/api/v3/envelopes/{envelopeId}/notifications";

            // Montar o corpo da requisição
            var requestBody = new
            {
                data = new
                {
                    type = "notifications",
                    attributes = new
                    {
                        message = "O se doumento foi ciado com Sucesso" // Mensagem que você deseja enviar
                    }
                }
            };

            var json = System.Text.Json.JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/vnd.api+json");
            content.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.api+json");

            // Criar a requisição HTTP
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = content
            };
            requestMessage.Headers.Add("Authorization", _accessToken);
            requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.api+json"));

            // Enviar a requisição
            var response = await _httpClient.SendAsync(requestMessage);

            // Verificar se a resposta foi bem-sucedida
            var result = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Notificação do envelope criada");
            return response.IsSuccessStatusCode ? result : $"Erro ao criar a notificação do envelope: {result}";
        }


        public async Task<string> CreateNotification(string envelopeId, string signerId)
        {
            var url = $"{_clickSignUrl}/api/v3/envelopes/{envelopeId}/signers/{signerId}/notifications";

            // Montar o corpo da requisição
            var requestBody = new
            {
                data = new
                {
                    type = "notifications",
                    attributes = new
                    {
                        message = " ao clicar no Token a sua assinatura estará Válida"
                    }
                }
            };

            var json = System.Text.Json.JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/vnd.api+json");
            content.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.api+json");

            // Criar a requisição HTTP
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = content
            };
            requestMessage.Headers.Add("Authorization", _accessToken);
            requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.api+json"));

            // Enviar a requisição
            var response = await _httpClient.SendAsync(requestMessage);

            // Verificar se a resposta foi bem-sucedida
            var result = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Notificação de assinatura cirada");
            return response.IsSuccessStatusCode ? result : $"Erro ao criar a notificação: {result}";
        }



    }


}



       


    






    public class CreateEnvelopeData
    {
        public string Name { get; set; } = string.Empty;
    }





   public class EnvelopeResponse
    {
        public EnvelopeData Data { get; set; } = new EnvelopeData();
    }

    public class EnvelopeData
    {
        public string Id { get; set; } = string.Empty;
    }



public class CreateProcessSigner
    {
       public string Name { get; set; }
      
        public string Documentation { get; set; }
      
        
    }

    public class CommunicateEventsDTO
    {
        public string DocumentSigned { get; set; }
        public string SignatureRequest { get; set; }
        public string SignatureReminder { get; set; }
    }

