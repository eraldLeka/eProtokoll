using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eProtokoll.Models
{
    public class Document
    {
        public int DocumentId { get; set; }

        [BindNever]
        [Display(Name = "Numri i Dokumentit")]
        public int DocumentNumber { get; set; }

        [BindNever]
        [Display(Name = "Viti")]
        public int Year { get; set; } = DateTime.Now.Year;

        [NotMapped]
        [Display(Name = "Numri i Protokollit")]
        public string ProtocolNumber => $"{DocumentNumber}/{Year}";

        [Required]
        [Display(Name = "Lloji i Dokumentit")]
        public DocumentType DocumentType { get; set; }

        [Required(ErrorMessage = "Subjekti është i detyrueshëm")]
        [StringLength(500, ErrorMessage = "Subjekti nuk mund të jetë më shumë se 500 karaktere")]
        [Display(Name = "Subjekti")]
        public string Subject { get; set; } = null!;


        [Required]
        [Display(Name = "Klasifikimi")]
        public Classification Classification { get; set; }


        [Display(Name = "Prioriteti")]
        public Priority Priority { get; set; } = Priority.Normal;

        [Display(Name = "Ka Attachment")]
        public bool HasAttachments { get; set; } = false;

        [Display(Name = "Data e Krijimit")]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [BindNever]
        [Display(Name = "Krijuar nga")]
        public int CreatedBy { get; set; }

        [ForeignKey("CreatedBy")]
        public virtual Users? Creator { get; set; }

        [Display(Name = "Kerkon pergjigje")]
        public bool RequiresResponse { get; set; } = false;

        public virtual ICollection<DocumentAttachment> Attachments { get; set; } = new List<DocumentAttachment>();

        public virtual ICollection<DocumentTracking> Trackings { get; set; } = new List<DocumentTracking>();
    }
}