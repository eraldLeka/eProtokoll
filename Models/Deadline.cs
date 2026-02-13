using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eProtokoll.Models
{
    public class Deadline
    {
        [Key]
        public int DeadlineId { get; set; }

        [Required]
        public int DocumentId { get; set; }

        [ForeignKey("DocumentId")]
        public virtual Document? Document { get; set; }

        [Required(ErrorMessage = "Titulli është i detyrueshëm")]
        [StringLength(200)]
        [Display(Name = "Titulli")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Data e afatit është e detyrueshme")]
        [Display(Name = "Data e Afatit")]
        [DataType(DataType.Date)]
        public DateTime DueDate { get; set; }

        [Required]
        [Display(Name = "Prioriteti")]
        public Priority Priority { get; set; } = Priority.Normal;

        // Përdoruesi përgjegjës
        [Required]
        public int ResponsibleUserId { get; set; }

        [ForeignKey("ResponsibleUserId")]
        public virtual Users? ResponsibleUser { get; set; }

        [Display(Name = "Është Përfunduar")]
        public bool IsCompleted { get; set; } = false;

        [Display(Name = "Data e Përfundimit")]
        public DateTime? CompletedDate { get; set; }

        public int? CompletedBy { get; set; }

        [ForeignKey("CompletedBy")]
        public virtual Users? CompletedByUser { get; set; }

        [StringLength(1000)]
        [Display(Name = "Shënime")]
        [DataType(DataType.MultilineText)]
        public string? Notes { get; set; }

        [Display(Name = "Data e Krijimit")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Required]
        public int CreatedBy { get; set; }

        [ForeignKey("CreatedBy")]
        public virtual Users? Creator { get; set; }
    }
}