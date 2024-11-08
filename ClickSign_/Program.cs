using SignInClick.Services;

namespace SignInClick
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Adicionar o serviço de CORS
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAllOrigins", policy =>
                {
                    policy.AllowAnyOrigin()  // Permite qualquer origem
                          .AllowAnyHeader()  // Permite qualquer cabeçalho
                          .AllowAnyMethod(); // Permite qualquer método HTTP (GET, POST, etc.)
                });
            });

            // Add services to the container.
            builder.Services.AddControllers();

            // Configuração do Swagger/OpenAPI
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // Configuração do ClicksignService com HttpClient
            builder.Services.AddHttpClient<ClicksignService>(client =>
            {
                client.BaseAddress = new Uri("https://app.clicksign.com/v3");
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            });

            // Registro do ProcessClickSign no contêiner de injeção de dependência
            builder.Services.AddScoped<ProcessClickSign>();
            builder.Services.AddTransient<AwsS3Handler>();


            var app = builder.Build();

            // Configuração do pipeline de requisições HTTP
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            // Aplicar a política de CORS
            app.UseCors("AllowAllOrigins");

            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();

            app.Run();
        }
    }
}
