using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eProtokoll.Models
{
    public class DocumentTracking
    {
        [Key]
        public int TrackingId { get; set; }

        [Required(ErrorMessage = "Dokumenti është i detyrueshëm")]
        [Display(Name = "Dokumenti")]
        public int DocumentId { get; set; }

        [ForeignKey("DocumentId")]
        public virtual Document? Document { get; set; }

        [Required(ErrorMessage = "Përdoruesi i caktuar është i detyrueshëm")]
        [Display(Name = "Caktuar Për")]
        public int AssignedToUserId { get; set; }

        [ForeignKey("AssignedToUserId")]
        public virtual Users? AssignedToUser { get; set; }

        [Required(ErrorMessage = "Përdoruesi që cakton është i detyrueshëm")]
        [Display(Name = "Caktuar Nga")]
        public int AssignedByUserId { get; set; }

        [ForeignKey("AssignedByUserId")]
        public virtual Users? AssignedByUser { get; set; }

        [Required]
        [Display(Name = "Data e Caktimit")]
        public DateTime AssignedDate { get; set; } = DateTime.Now;

        [Display(Name = "Prioriteti")]
        public Priority Priority { get; set; } = Priority.Normal;

        [Display(Name = "Data e Afatit")]
        [DataType(DataType.Date)]
        public DateTime? DueDate { get; set; }

        [StringLength(1000)]
        [Display(Name = "Shënime")]
        [DataType(DataType.MultilineText)]
        public string? Notes { get; set; }

        [Display(Name = "Data e Përfundimit")]
        public DateTime? CompletedDate { get; set; }

        [Display(Name = "Është Aktiv")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Data e Krijimit")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}