using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eProtokoll.Models
{
    /// <summary>
    /// Klasa bazë për të gjitha llojet e dokumenteve
    /// </summary>
    public class Document
    {
        [Key]
        public int DocumentId { get; set; }

        [Required(ErrorMessage = "Numri i protokollit është i detyrueshëm")]
        [StringLength(50)]
        [Display(Name = "Numri i Protokollit")]
        public string ProtocolNumber { get; set; }

        [Required(ErrorMessage = "Data e protokollimit është e detyrueshme")]
        [Display(Name = "Data e Protokollimit")]
        [DataType(DataType.Date)]
        public DateTime ProtocolDate { get; set; } = DateTime.Now.Date;

        [Display(Name = "Ora e Protokollimit")]
        [DataType(DataType.Time)]
        public TimeSpan ProtocolTime { get; set; } = DateTime.Now.TimeOfDay;

        [Required(ErrorMessage = "Lloji i dokumentit është i detyrueshëm")]
        [Display(Name = "Lloji i Dokumentit")]
        public DocumentType DocumentType { get; set; }

        [Required(ErrorMessage = "Subjekti është i detyrueshëm")]
        [StringLength(500, ErrorMessage = "Subjekti nuk mund të jetë më shumë se 500 karaktere")]
        [Display(Name = "Subjekti")]
        public string Subject { get; set; }

        [Display(Name = "Përmbajtja")]
        [DataType(DataType.MultilineText)]
        public string? Content { get; set; }

        [Required]
        [Display(Name = "Klasifikimi")]
        public int ClassificationId { get; set; }

        [ForeignKey("ClassificationId")]
        [Display(Name = "Klasifikimi")]
        public virtual Classification? Classification { get; set; }

        [Required]
        [Display(Name = "Statusi")]
        public DocumentStatus Status { get; set; } = DocumentStatus.Registered;

        [Display(Name = "Prioriteti")]
        public Priority Priority { get; set; } = Priority.Normal;

        [Display(Name = "Kërkon Përgjigje")]
        public bool RequiresResponse { get; set; } = false;

        [Display(Name = "Ka Afat")]
        public bool HasDeadline { get; set; } = false;

        [Display(Name = "Data e Afatit")]
        [DataType(DataType.Date)]
        public DateTime? DeadlineDate { get; set; }

        [StringLength(1000)]
        [Display(Name = "Shënime")]
        [DataType(DataType.MultilineText)]
        public string? Notes { get; set; }

        [Display(Name = "Ka Attachment")]
        public bool HasAttachments { get; set; } = false;

        [Display(Name = "Është Arkivuar")]
        public bool IsArchived { get; set; } = false;

        [Display(Name = "Data e Arkivimit")]
        [DataType(DataType.Date)]
        public DateTime? ArchivedDate { get; set; }

        [Display(Name = "Arkivuar nga")]
        public string? ArchivedBy { get; set; }

        [Display(Name = "Data e Krijimit")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Display(Name = "Data e Modifikimit")]
        public DateTime? ModifiedDate { get; set; }

        [BindNever]
        [StringLength(450)]
        [Display(Name = "Krijuar nga")]
        public string? CreatedBy { get; set; }

        [ForeignKey("CreatedBy")]
        public virtual ApplicationUser? Creator { get; set; }

        [StringLength(450)]
        [Display(Name = "Modifikuar nga")]
        public string? ModifiedBy { get; set; }

        // Navigation Properties
        [Display(Name = "Shtojcat")]
        public virtual ICollection<DocumentAttachment>? Attachments { get; set; }

        [Display(Name = "Gjurmimi")]
        public virtual ICollection<DocumentTracking>? Trackings { get; set; }

        [Display(Name = "Afatet")]
        public virtual ICollection<Deadline>? Deadlines { get; set; }
    }
}