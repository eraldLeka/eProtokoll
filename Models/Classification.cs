using System.ComponentModel.DataAnnotations;

namespace eProtokoll.Models
{
    /// <summary>
    /// Klasifikimi i dokumenteve sipas nivelit të aksesit
    /// </summary>
    public class Classification
    {
        [Key]
        public int ClassificationId { get; set; }

        [Required(ErrorMessage = "Emri i klasifikimit është i detyrueshëm")]
        [StringLength(100, ErrorMessage = "Emri nuk mund të jetë më shumë se 100 karaktere")]
        [Display(Name = "Emri i Klasifikimit")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Niveli i aksesit është i detyrueshëm")]
        [Display(Name = "Niveli i Aksesit")]
        public AccessLevel Level { get; set; }

        [StringLength(500)]
        [Display(Name = "Përshkrimi")]
        public string? Description { get; set; }

        [Required]
        [Display(Name = "Koha e Ruajtjes (vjet)")]
        [Range(1, 99, ErrorMessage = "Koha e ruajtjes duhet të jetë midis 1 dhe 99 vjet")]
        public int RetentionYears { get; set; } = 5;

        [StringLength(7)]
        [Display(Name = "Ngjyra e Etiketës")]
        [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "Ngjyra duhet të jetë në format HEX (p.sh. #FF0000)")]
        public string? ColorCode { get; set; }

        [Display(Name = "Renditja")]
        public int SortOrder { get; set; }

        [Display(Name = "Aktiv")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Është Parazgjedhje")]
        public bool IsDefault { get; set; } = false;

        [Display(Name = "Data e Krijimit")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Display(Name = "Data e Modifikimit")]
        public DateTime? ModifiedDate { get; set; }

        [StringLength(450)]
        [Display(Name = "Krijuar nga")]
        public string? CreatedBy { get; set; }

        [StringLength(450)]
        [Display(Name = "Modifikuar nga")]
        public string? ModifiedBy { get; set; }

        // Navigation Properties
        [Display(Name = "Dokumentet")]
        public virtual ICollection<Document>? Documents { get; set; }
    }

    /// <summary>
    /// Nivelet e aksesit për dokumentet
    /// </summary>
    public enum AccessLevel
    {
        [Display(Name = "Publik", Description = "Të gjithë mund ta shohin")]
        Public = 0,

        [Display(Name = "I Kufizuar", Description = "Vetëm punonjësit e përzgjedhur")]
        Restricted = 1,

        [Display(Name = "Sekret", Description = "Vetëm menaxherët dhe administratorët")]
        Secret = 2
    }
}