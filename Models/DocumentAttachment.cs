using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eProtokoll.Models
{
    /// <summary>
    /// Shtojcat e dokumenteve - Skedarët e bashkangjitur me dokumentet
    /// </summary>
    public class DocumentAttachment
    {
        [Key]
        public int AttachmentId { get; set; }

        [Required(ErrorMessage = "Dokumenti është i detyrueshëm")]
        [Display(Name = "Dokumenti")]
        public int DocumentId { get; set; }

        [ForeignKey("DocumentId")]
        [Display(Name = "Dokumenti")]
        public virtual Document? Document { get; set; }

        [Required(ErrorMessage = "Emri i skedarit është i detyrueshëm")]
        [StringLength(255)]
        [Display(Name = "Emri i Skedarit")]
        public string FileName { get; set; }

        [Required]
        [StringLength(500)]
        [Display(Name = "Emri Origjinal i Skedarit")]
        public string OriginalFileName { get; set; }

        [Required]
        [StringLength(500)]
        [Display(Name = "Shtegu i Skedarit")]
        public string FilePath { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Lloji i Skedarit (MIME)")]
        public string ContentType { get; set; }

        [StringLength(50)]
        [Display(Name = "Ekstensioni")]
        public string? FileExtension { get; set; }

        [Required]
        [Display(Name = "Madhësia (bytes)")]
        public long FileSize { get; set; }

        [Display(Name = "Madhësia (MB)")]
        [NotMapped]
        public decimal FileSizeMB => Math.Round((decimal)FileSize / 1024 / 1024, 2);

        [Display(Name = "Kategoria e Skedarit")]
        public FileCategory Category { get; set; }

        [StringLength(500)]
        [Display(Name = "Përshkrimi")]
        [DataType(DataType.MultilineText)]
        public string? Description { get; set; }

        [Display(Name = "Numri Rendor")]
        public int DisplayOrder { get; set; } = 0;

        [Display(Name = "Është Dokument Kryesor")]
        public bool IsPrimaryDocument { get; set; } = false;

        [Display(Name = "Është i Skanuar")]
        public bool IsScanned { get; set; } = false;

        [Display(Name = "Është i Enkriptuar")]
        public bool IsEncrypted { get; set; } = false;

        [StringLength(200)]
        [Display(Name = "Hash i Skedarit (SHA256)")]
        public string? FileHash { get; set; }

        [Display(Name = "Është i Kompresuar")]
        public bool IsCompressed { get; set; } = false;

        [Display(Name = "Versioni")]
        public int Version { get; set; } = 1;

        [Display(Name = "Shtojca Paraprake")]
        public int? PreviousVersionId { get; set; }

        [ForeignKey("PreviousVersionId")]
        public virtual DocumentAttachment? PreviousVersion { get; set; }

        [Display(Name = "Ka Thumbnail")]
        public bool HasThumbnail { get; set; } = false;

        [StringLength(500)]
        [Display(Name = "Shtegu i Thumbnail")]
        public string? ThumbnailPath { get; set; }

        [Display(Name = "Numri i Faqeve")]
        public int? PageCount { get; set; }

        [Display(Name = "Gjerësia (pixels)")]
        public int? Width { get; set; }

        [Display(Name = "Lartësia (pixels)")]
        public int? Height { get; set; }

        [Display(Name = "Kohëzgjatja (sekonda)")]
        public int? Duration { get; set; }

        [Display(Name = "Ka OCR")]
        public bool HasOCR { get; set; } = false;

        [Display(Name = "Teksti i OCR")]
        [DataType(DataType.MultilineText)]
        public string? OCRText { get; set; }

        [Display(Name = "Data e OCR")]
        public DateTime? OCRProcessedDate { get; set; }

        [Display(Name = "Është i Shtypur")]
        public bool IsPrinted { get; set; } = false;

        [Display(Name = "Numri i Shtypjeve")]
        public int PrintCount { get; set; } = 0;

        [Display(Name = "Data e Shtypjes së Fundit")]
        public DateTime? LastPrintedDate { get; set; }

        [Display(Name = "Është Shkarkuar")]
        public bool IsDownloaded { get; set; } = false;

        [Display(Name = "Numri i Shkarkimeve")]
        public int DownloadCount { get; set; } = 0;

        [Display(Name = "Data e Shkarkimit të Fundit")]
        public DateTime? LastDownloadedDate { get; set; }

        [Display(Name = "Numri i Shikimeve")]
        public int ViewCount { get; set; } = 0;

        [Display(Name = "Data e Shikimit të Fundit")]
        public DateTime? LastViewedDate { get; set; }

        [Display(Name = "Është i Fshirë")]
        public bool IsDeleted { get; set; } = false;

        [Display(Name = "Data e Fshirjes")]
        public DateTime? DeletedDate { get; set; }

        [StringLength(450)]
        [Display(Name = "Fshirë nga")]
        public string? DeletedBy { get; set; }

        [Display(Name = "Është i Arkivuar")]
        public bool IsArchived { get; set; } = false;

        [Display(Name = "Data e Arkivimit")]
        public DateTime? ArchivedDate { get; set; }

        [Display(Name = "Është Virus-Free")]
        public bool IsVirusScanned { get; set; } = false;

        [Display(Name = "Është i Sigurt")]
        public bool IsVirusFree { get; set; } = false;

        [Display(Name = "Data e Skanimit Antivirus")]
        public DateTime? VirusScanDate { get; set; }

        [StringLength(200)]
        [Display(Name = "Motori Antivirus")]
        public string? AntivirusEngine { get; set; }

        [Display(Name = "Është Publik")]
        public bool IsPublic { get; set; } = false;

        [Display(Name = "Kërkon Autorizim")]
        public bool RequiresAuthorization { get; set; } = true;

        [Display(Name = "Lejon Shkarkim")]
        public bool AllowDownload { get; set; } = true;

        [Display(Name = "Lejon Shtypje")]
        public bool AllowPrint { get; set; } = true;

        [Display(Name = "Data e Skadimit")]
        [DataType(DataType.Date)]
        public DateTime? ExpirationDate { get; set; }

        [StringLength(500)]
        [Display(Name = "Shënime")]
        [DataType(DataType.MultilineText)]
        public string? Notes { get; set; }

        [StringLength(500)]
        [Display(Name = "Metadata (JSON)")]
        public string? Metadata { get; set; }

        [Display(Name = "Data e Ngarkimit")]
        public DateTime UploadedDate { get; set; } = DateTime.Now;

        [Required]
        [StringLength(450)]
        [Display(Name = "Ngarkuar nga")]
        public string UploadedBy { get; set; }

        [ForeignKey("UploadedBy")]
        public virtual ApplicationUser? Uploader { get; set; }

        [Display(Name = "Data e Modifikimit")]
        public DateTime? ModifiedDate { get; set; }

        [StringLength(450)]
        [Display(Name = "Modifikuar nga")]
        public string? ModifiedBy { get; set; }

        // Navigation Properties
        public virtual ICollection<DocumentAttachment>? Versions { get; set; }
    }

    /// <summary>
    /// Kategoritë e skedarëve
    /// </summary>
    public enum FileCategory
    {
        [Display(Name = "Dokument")]
        Document = 1,

        [Display(Name = "Imazh")]
        Image = 2,

        [Display(Name = "PDF")]
        PDF = 3,

        [Display(Name = "Word")]
        Word = 4,

        [Display(Name = "Excel")]
        Excel = 5,

        [Display(Name = "PowerPoint")]
        PowerPoint = 6,

        [Display(Name = "Video")]
        Video = 7,

        [Display(Name = "Audio")]
        Audio = 8,

        [Display(Name = "Arkiv (ZIP/RAR)")]
        Archive = 9,

        [Display(Name = "Email")]
        Email = 10,

        [Display(Name = "Tjetër")]
        Other = 11
    }
}