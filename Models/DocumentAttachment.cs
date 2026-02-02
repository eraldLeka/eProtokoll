using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eProtokoll.Models
{
    public class DocumentAttachment
    {
        [Key]
        public int AttachmentId { get; set; }

        [Required]
        public int DocumentId { get; set; }

        [ForeignKey("DocumentId")]
        public virtual Document? Document { get; set; }

        [Required]
        [StringLength(255)]
        public string FileName { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string OriginalFileName { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string FilePath { get; set; } = string.Empty;

        [StringLength(100)]
        public string? ContentType { get; set; }

        [StringLength(50)]
        public string? FileExtension { get; set; }

        [Required]
        public long FileSize { get; set; }

        [NotMapped]
        public decimal FileSizeMB => Math.Round((decimal)FileSize / 1024 / 1024, 2);

        public FileCategory Category { get; set; } = FileCategory.Document;

        [StringLength(500)]
        public string? Description { get; set; }

        public int DisplayOrder { get; set; } = 0;

        public bool IsPrimaryDocument { get; set; } = false;

        public DateTime UploadedDate { get; set; } = DateTime.Now;

        [Required]
        [StringLength(450)]
        public string UploadedBy { get; set; } = string.Empty;

        [ForeignKey("UploadedBy")]
        public virtual ApplicationUser? Uploader { get; set; }
    }

    public enum FileCategory
    {
        [Display(Name = "Dokument")]
        Document = 1,

        [Display(Name = "PDF")]
        PDF = 2,

        [Display(Name = "Imazh")]
        Image = 3,

        [Display(Name = "Word")]
        Word = 4,

        [Display(Name = "Excel")]
        Excel = 5,

        [Display(Name = "Tjetër")]
        Other = 99
    }
}