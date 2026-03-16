namespace eProtokoll.Models
{
    public class AuditLog
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = null!;
        public string Action { get; set; } = null!;
        public int? DocumentId { get; set; }
        public string? Description { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}