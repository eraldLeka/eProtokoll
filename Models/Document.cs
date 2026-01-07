using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Xml.Linq;

namespace eProtokoll.Models
{
    /// Klasa bazë për të gjitha llojet e dokumenteve

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

        [StringLength(100)]
        [Display(Name = "Numri i Referencës")]
        public string? ReferenceNumber { get; set; }

        [Display(Name = "Data e Referencës")]
        [DataType(DataType.Date)]
        public DateTime? ReferenceDate { get; set; }

        [Required]
        [Display(Name = "Klasifikimi")]
        public int ClassificationId { get; set; }

        [ForeignKey("ClassificationId")]
        [Display(Name = "Klasifikimi")]
        public virtual Classification? Classification { get; set; }

        [Required]
        [Display(Name = "Statusi")]
        public DocumentStatus Status { get; set; } = DocumentStatus.Draft;

        [Display(Name = "Prioriteti")]
        public eProtokoll.Models.Priority Priority { get; set; } = eProtokoll.Models.Priority.Normal;

        [Display(Name = "Ka Afat")]
        public bool HasDeadline { get; set; } = false;

        [Display(Name = "Data e Afatit")]
        [DataType(DataType.Date)]
        public DateTime? DeadlineDate { get; set; }


        [StringLength(1000)]
        [Display(Name = "Shënime")]
        [DataType(DataType.MultilineText)]
        public string? Notes { get; set; }

        [Display(Name = "Numri i Faqeve")]
        public int? PageCount { get; set; }

        [StringLength(100)]
        [Display(Name = "Gjuha e Dokumentit")]
        public string? Language { get; set; } = "Shqip";

        [Display(Name = "Është i Skanuar")]
        public bool IsScanned { get; set; } = false;

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
        [Required]
        [StringLength(450)]
        [Display(Name = "Krijuar nga")]
        public string CreatedBy { get; set; }

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


    /// Llojet e dokumenteve

    public enum DocumentType
    {
        [Display(Name = "Dokument Hyrës")]
        Incoming = 1,

        [Display(Name = "Dokument Dalës")]
        Outgoing = 2,

        [Display(Name = "Dokument i Brendshëm")]
        Internal = 3
    }


    /// Statuset e dokumenteve

    public enum DocumentStatus
    {
        [Display(Name = "Draft")]
        Draft = 1,

        [Display(Name = "I Protokolluar")]
        Registered = 2,

        [Display(Name = "Në Proces")]
        InProgress = 3,

        [Display(Name = "Në Pritje")]
        Pending = 4,

        [Display(Name = "I Përfunduar")]
        Completed = 5,

        [Display(Name = "I Anulluar")]
        Cancelled = 6,

        [Display(Name = "I Arkivuar")]
        Archived = 7
    }


    /// Prioriteti i dokumenteve

    public enum Priority
    {
        [Display(Name = "I Ulët")]
        Low = 1,

        [Display(Name = "Normal")]
        Normal = 2,

        [Display(Name = "I Lartë")]
        High = 3,

        [Display(Name = "Urgjent")]
        Urgent = 4
    }
}