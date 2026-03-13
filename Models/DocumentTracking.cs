using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using eProtokoll.Models;

namespace eProtokoll.Models 
{
    public class DocumentTracking
    {
        [Key]
        public int TrackingId { get; set; }
        public int DocumentId { get; set; }
        public int AssignedToUserId { get; set; }
        public int AssignedByUserId { get; set; }
        public DateTime AssignedDate { get; set; } = DateTime.Now;
        public Priority Priority { get; set; } = Priority.Normal;
        public DateTime? DueDate { get; set; }
        public string? Notes { get; set; }
        public DateTime? CompletedDate { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [NotMapped] public Users? AssignedToUser { get; set; }
        [NotMapped] public Users? AssignedByUser { get; set; }
        [NotMapped] public string? DocumentProtocolNumber { get; set; }
        [NotMapped] public string? DocumentSubject { get; set; }
        [NotMapped] public string? DocumentDiscriminator { get; set; }
    }
}