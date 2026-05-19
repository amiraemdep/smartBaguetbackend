namespace SmartBaguet.API.Models
{
    public class Baguet
    {
        public int Id { get; set; }

        public string CodeBaguet { get; set; } = string.Empty;

        public string Status { get; set; } = "VIDE";

        public int? CurrentPlantId { get; set; }

        public Plant? CurrentPlant { get; set; }
    }
}
