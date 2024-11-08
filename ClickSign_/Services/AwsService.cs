using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Flunt.Notifications;


namespace SignInClick.Services;

public class AwsS3Handler : Notifiable<Notification>
{
    private readonly AmazonS3Client _client;
    public AwsS3Handler()
    {
        string accessKey = "";
        string secretKey = "";
        RegionEndpoint regiao = RegionEndpoint.USEast1;

        _client = new AmazonS3Client(accessKey, secretKey, regiao);
    }
    public async Task Upload(Stream stream, string bucket, string arquivo)
    {
        try
        {
            TransferUtility transferUtility = new(_client);
            await transferUtility.UploadAsync(stream, bucket, arquivo);
        }
        catch (AmazonS3Exception amazonS3Exception)
        {
            AddNotification("AmazonS3", $"Falha no Upload : Amazon S3 Exception: {amazonS3Exception.Message}");
        }
        catch (Exception ex)
        {
            AddNotification("AmazonS3", $"Falha no Upload : Error occurred: {ex.Message}");
        }
    }
    public async Task<string?> UploadImage(IFormFile? formulario, string bucket)
    {
        if (formulario != null)
        {
            var extension = Path.GetExtension(formulario.FileName);
            var extensoesValidas = new[] { ".jpg", ".jpeg", ".png", ".webp" };

            if (!extensoesValidas.Contains(extension.ToLower()))
                AddNotification("Upload de Imagem", "Tipo de Arquivo não suportado! tipos de imagens aceitas: jpg, jpeg, png, webp");

            const long maxFileSize = 1 * 1024 * 1024;

            if (formulario.Length > maxFileSize)
                AddNotification("Upload de Imagem", "O arquivo excede o tamanho máximo permitido de 1mb.");

            if (IsValid)
            {
                string arquivo = Guid.NewGuid() + extension;

                using (var stream = formulario.OpenReadStream())
                    await Upload(stream, bucket, arquivo);
                return arquivo;
            }
        }
        return null;
    }

    public async Task Deletar(string bucket, string arquivo)
    {
        try
        {
            var request = new DeleteObjectRequest
            {
                BucketName = bucket,
                Key = arquivo
            };

            await _client.DeleteObjectAsync(request);
        }
        catch (AmazonS3Exception amazonS3Exception)
        {
            AddNotification("AmazonS3", $"Amazon S3 Exception: {amazonS3Exception.Message}");
        }
        catch (Exception ex)
        {
            AddNotification("AmazonS3", $"Error occurred: {ex.Message}");
        }
    }
    public string Get(string bucket, string arquivo)
    {
        return $"https://{bucket}.s3.us-east-1.amazonaws.com/{arquivo}";
    }
}
