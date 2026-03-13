using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace eProtokoll.Models
{
    public class Document
    {
        public int DocumentId { get; set; }

        [BindNever]
        [Required]
        [StringLength(50)]
        [Display(Name = "Numri i Protokollit")]
        public string ProtocolNumber { get; set; } = null!;

        [BindNever]
        [Display(Name = "Data e Protokollimit")]
        [DataType(DataType.Date)]
        public DateTime ProtocolDate { get; set; } = DateTime.Now.Date;

        [BindNever]
        [Display(Name = "Ora e Protokollimit")]
        [DataType(DataType.Time)]
        public TimeSpan ProtocolTime { get; set; } = new TimeSpan(
            DateTime.Now.Hour,
            DateTime.Now.Minute,
            DateTime.Now.Second
        );

        [Required]
        [Display(Name = "Lloji i Dokumentit")]
        public DocumentType DocumentType { get; set; }

        [Required(ErrorMessage = "Subjekti është i detyrueshëm")]
        [StringLength(500, ErrorMessage = "Subjekti nuk mund të jetë më shumë se 500 karaktere")]
        [Display(Name = "Subjekti")]
        public string Subject { get; set; } = null!;

        [Display(Name = "Përmbajtja")]
        [DataType(DataType.MultilineText)]
        public string? Content { get; set; }

        [Required]
        [Display(Name = "Klasifikimi")]
        public Classification Classification { get; set; }

        [Display(Name = "Statusi")]
        public DocumentStatus Status { get; set; } = DocumentStatus.Registered;

        [Display(Name = "Prioriteti")]
        public Priority Priority { get; set; } = Priority.Normal;

        [StringLength(1000)]
        [Display(Name = "Shënime")]
        [DataType(DataType.MultilineText)]
        public string? Notes { get; set; }

        [Display(Name = "Ka Attachment")]
        public bool HasAttachments { get; set; } = false;

        [Display(Name = "Data e Krijimit")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [BindNever]
        [Display(Name = "Krijuar nga")]
        public int? CreatedBy { get; set; }

        [ForeignKey("CreatedBy")]
        public virtual Users? Creator { get; set; }

        public virtual ICollection<DocumentAttachment>? Attachments { get; set; }
        public virtual ICollection<DocumentTracking>? Trackings { get; set; }
        [NotMapped]
        public string? Discriminator { get; set; }
    }
}