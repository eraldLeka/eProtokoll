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

        [Required]
        [StringLength(64)]
        public string FileHash { get; set; } = string.Empty;

        [StringLength(100)]
        public string? ContentType { get; set; }

        [StringLength(50)]
        public string? FileExtension { get; set; }

        [Required]
        public long FileSize { get; set; }

        [NotMapped]
        public decimal FileSizeMB => Math.Round((decimal)FileSize / 1024 / 1024, 2);

        public FileCategory Category { get; set; } = FileCategory.PDF;

        [StringLength(500)]
        public string? Description { get; set; }

        public int DisplayOrder { get; set; } = 0;

        public bool IsPrimaryDocument { get; set; } = false;

        public DateTime UploadedDate { get; set; } = DateTime.Now;

        [Required]
        public int UploadedBy { get; set; }

        [ForeignKey("UploadedBy")]
        public virtual Users? Uploader { get; set; }
    }

    public enum FileCategory
    {
        [Display(Name = "PDF")]
        PDF = 1
    }
}