using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eProtokoll.Models
{
    /// <summary>
    /// Dokumentet e brendshme - Shkresa që qarkullojnë brenda institucionit
    /// </summary>
    public class InternalDocument : Document
    {
        [StringLength(100)]
        [Display(Name = "Departamenti Dërgues")]
        public string? FromDepartment { get; set; }

        [StringLength(100)]
        [Display(Name = "Departamenti Marrës")]
        public string? ToDepartment { get; set; }

        [StringLength(450)]
        [Display(Name = "Nga (Përdoruesi)")]
        public string? FromUserId { get; set; }

        [ForeignKey("FromUserId")]
        public virtual ApplicationUser? FromUser { get; set; }

        [StringLength(450)]
        [Display(Name = "Për (Përdoruesi)")]
        public string? ToUserId { get; set; }

        [ForeignKey("ToUserId")]
        public virtual ApplicationUser? ToUser { get; set; }

        // === PËRGJIGJA ===

        [Display(Name = "Data e Afatit për Përgjigje")]
        [DataType(DataType.Date)]
        public DateTime? ResponseDeadline { get; set; }

        [Display(Name = "Është Përgjigjur")]
        public bool IsResponded { get; set; } = false;

        [Display(Name = "Data e Përgjigjes")]
        [DataType(DataType.Date)]
        public DateTime? ResponseDate { get; set; }

        [Display(Name = "Dokumenti i Përgjigjes")]
        public int? ResponseDocumentId { get; set; }

        [ForeignKey("ResponseDocumentId")]
        public virtual InternalDocument? ResponseDocument { get; set; }

        public InternalDocument()
        {
            DocumentType = DocumentType.Internal;
        }
    }
} 