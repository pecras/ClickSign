namespace SignInClick.DTOS
{
    public class DocumentModel
    {
        public int Id { get; set; }
        public string Key { get; set; } // O ID do documento no Clicksign
        public string Name { get; set; } // Nome do arquivo/documento
                                         // Adicione outros campos relevantes, como data de criação, etc.
    }
}
