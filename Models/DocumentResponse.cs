using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eProtokoll.Models
{
    /// <summary>
    /// Përgjigjet e dokumenteve - Response workflow për employee
    /// Ruan historinë e të gjitha përpjekjeve të ngarkimit
    /// </summary>
    public class DocumentResponse
    {
        [Key]
        public int ResponseId { get; set; }

        [Required(ErrorMessage = "Gjurmimi është i detyrueshëm")]
        [Display(Name = "Gjurmimi")]
        public int TrackingId { get; set; }

        [ForeignKey("TrackingId")]
        [Display(Name = "Gjurmimi i Dokumentit")]
        public virtual DocumentTracking? Tracking { get; set; }

        [Required(ErrorMessage = "Subjekti i përgjigjes është i detyrueshëm")]
        [StringLength(500)]
        [Display(Name = "Subjekti i Përgjigjes")]
        public string ResponseSubject { get; set; }

        [StringLength(2000)]
        [Display(Name = "Shënime")]
        [DataType(DataType.MultilineText)]
        public string? ResponseNotes { get; set; }

        [Required(ErrorMessage = "Dokumenti i skanuar është i detyrueshëm")]
        [StringLength(500)]
        [Display(Name = "Path i PDF-së")]
        public string ScannedPdfPath { get; set; }

        [Required]
        [StringLength(255)]
        [Display(Name = "Emri i File-it")]
        public string ScannedPdfName { get; set; }

        [Required]
        [Display(Name = "Madhësia e File-it (bytes)")]
        public long ScannedPdfSize { get; set; }

        [Display(Name = "Statusi i Përgjigjes")]
        public ResponseStatus Status { get; set; } = ResponseStatus.Draft;

        [Display(Name = "Data e Krijimit")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Display(Name = "Data e Dërgimit")]
        public DateTime? SubmittedDate { get; set; }

        [Display(Name = "Data e Aprovimit")]
        public DateTime? ApprovedDate { get; set; }

        [StringLength(450)]
        [Display(Name = "Aprovuar Nga")]
        public string? ApprovedByUserId { get; set; }

        [ForeignKey("ApprovedByUserId")]
        [Display(Name = "Aprovues")]
        public virtual ApplicationUser? ApprovedByUser { get; set; }

        [Display(Name = "Data e Refuzimit")]
        public DateTime? RejectedDate { get; set; }

        [StringLength(1000)]
        [Display(Name = "Arsyeja e Refuzimit")]
        [DataType(DataType.MultilineText)]
        public string? RejectionReason { get; set; }

        [Display(Name = "Dokumenti Dalës i Gjeneruar")]
        public int? OutgoingDocumentId { get; set; }

        [ForeignKey("OutgoingDocumentId")]
        [Display(Name = "Dokument Dalës")]
        public virtual Document? OutgoingDocument { get; set; }

        [Display(Name = "Është Aktiv")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Data e Modifikimit")]
        public DateTime? ModifiedDate { get; set; }

        [StringLength(450)]
        [Display(Name = "Modifikuar Nga")]
        public string? ModifiedBy { get; set; }

        [Display(Name = "Numri i Versionit")]
        public int VersionNumber { get; set; } = 1;
    }

    /// <summary>
    /// Statusi i përgjigjes së dokumentit
    /// </summary>
    public enum ResponseStatus
    {
        [Display(Name = "Draft")]
        Draft = 1,

        [Display(Name = "I Dërguar për Aprovim")]
        Submitted = 2,

        [Display(Name = "I Aprovuar")]
        Approved = 3,

        [Display(Name = "I Refuzuar")]
        Rejected = 4
    }
}