using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eProtokoll.Models
{
    /// <summary>
    /// Gjurmimi i dokumenteve - Delegimi i dokumenteve tek punonjësit
    /// </summary>
    public class DocumentTracking
    {
        [Key]
        public int TrackingId { get; set; }

        [Required(ErrorMessage = "Dokumenti është i detyrueshëm")]
        [Display(Name = "Dokumenti")]
        public int DocumentId { get; set; }

        [ForeignKey("DocumentId")]
        [Display(Name = "Dokumenti")]
        public virtual Document? Document { get; set; }

        [Required(ErrorMessage = "Përdoruesi i caktuar është i detyrueshëm")]
        [StringLength(450)]
        [Display(Name = "Caktuar Për")]
        public string AssignedToUserId { get; set; }

        [ForeignKey("AssignedToUserId")]
        [Display(Name = "Përdoruesi")]
        public virtual ApplicationUser? AssignedToUser { get; set; }

        [Required(ErrorMessage = "Përdoruesi që cakton është i detyrueshëm")]
        [StringLength(450)]
        [Display(Name = "Caktuar Nga")]
        public string AssignedByUserId { get; set; }

        [ForeignKey("AssignedByUserId")]
        [Display(Name = "Caktuar Nga")]
        public virtual ApplicationUser? AssignedByUser { get; set; }

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