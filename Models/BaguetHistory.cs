namespace SmartBaguet.API.Models
{
    public class BaguetHistory
    {
        public int Id { get; set; }


        public string CodeBaguet { get; set; } = string.Empty;

        public string CodePlant { get; set; } = string.Empty;

        public DateTime DateEntree { get; set; }

        public DateTime? DateSortie { get; set; }
    }
}
