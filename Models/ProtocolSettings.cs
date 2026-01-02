using System.ComponentModel.DataAnnotations;

namespace eProtokoll.Models
{
    /// <summary>
    /// Cilësimet e protokollimit të dokumenteve - Numri i protokollit dhe parametrat e tjerë
    /// </summary>
    public class ProtocolSettings
    {
        [Key]
        public int ProtocolSettingsId { get; set; }

        [Required(ErrorMessage = "Viti është i detyrueshëm")]
        [Display(Name = "Viti")]
        [Range(2000, 2100, ErrorMessage = "Viti duhet të jetë midis 2000 dhe 2100")]
        public int Year { get; set; } = DateTime.Now.Year;

        // === Cilësimet për Dokumentet Hyrëse ===
        [Required(ErrorMessage = "Numri i fillimit për dokumentet hyrëse është i detyrueshëm")]
        [Display(Name = "Hyrëse - Numri i Fillimit")]
        [Range(1, 999999, ErrorMessage = "Numri duhet të jetë midis 1 dhe 999999")]
        public int IncomingStartNumber { get; set; } = 1;

        [Display(Name = "Hyrëse - Numri Aktual")]
        public int IncomingCurrentNumber { get; set; } = 1;

        [Display(Name = "Hyrëse - Numri i Mbylljes")]
        public int? IncomingEndNumber { get; set; }

        [StringLength(20)]
        [Display(Name = "Hyrëse - Prefiksi")]
        public string? IncomingPrefix { get; set; } = "H";

        [StringLength(20)]
        [Display(Name = "Hyrëse - Sufiksi")]
        public string? IncomingSuffix { get; set; }

        // === Cilësimet për Dokumentet Dalëse ===
        [Required(ErrorMessage = "Numri i fillimit për dokumentet dalëse është i detyrueshëm")]
        [Display(Name = "Dalëse - Numri i Fillimit")]
        [Range(1, 999999, ErrorMessage = "Numri duhet të jetë midis 1 dhe 999999")]
        public int OutgoingStartNumber { get; set; } = 1;

        [Display(Name = "Dalëse - Numri Aktual")]
        public int OutgoingCurrentNumber { get; set; } = 1;

        [Display(Name = "Dalëse - Numri i Mbylljes")]
        public int? OutgoingEndNumber { get; set; }

        [StringLength(20)]
        [Display(Name = "Dalëse - Prefiksi")]
        public string? OutgoingPrefix { get; set; } = "D";

        [StringLength(20)]
        [Display(Name = "Dalëse - Sufiksi")]
        public string? OutgoingSuffix { get; set; }

        // === Cilësimet për Dokumentet e Brendshme ===
        [Required(ErrorMessage = "Numri i fillimit për dokumentet e brendshme është i detyrueshëm")]
        [Display(Name = "Brendshme - Numri i Fillimit")]
        [Range(1, 999999, ErrorMessage = "Numri duhet të jetë midis 1 dhe 999999")]
        public int InternalStartNumber { get; set; } = 1;

        [Display(Name = "Brendshme - Numri Aktual")]
        public int InternalCurrentNumber { get; set; } = 1;

        [Display(Name = "Brendshme - Numri i Mbylljes")]
        public int? InternalEndNumber { get; set; }

        [StringLength(20)]
        [Display(Name = "Brendshme - Prefiksi")]
        public string? InternalPrefix { get; set; } = "B";

        [StringLength(20)]
        [Display(Name = "Brendshme - Sufiksi")]
        public string? InternalSuffix { get; set; }

        // === Formati i Numrit të Protokollit ===
        [Required]
        [StringLength(50)]
        [Display(Name = "Formati i Numrit")]
        public string ProtocolNumberFormat { get; set; } = "{PREFIX}-{NUMBER}/{YEAR}";
        // Shembull: H-001/2024, D-001/2024, B-001/2024

        [Display(Name = "Gjatësia e Numrit (Zeros)")]
        [Range(1, 10, ErrorMessage = "Gjatësia duhet të jetë midis 1 dhe 10")]
        public int NumberPadding { get; set; } = 4;
        // Shembull: 4 = 0001, 5 = 00001

        // === Cilësime të Përgjithshme ===
        [Display(Name = "Rivendos Automatikisht Çdo Vit")]
        public bool AutoResetYearly { get; set; } = true;

        [Display(Name = "Lejon Modifikim Manualisht")]
        public bool AllowManualEdit { get; set; } = false;

        [Display(Name = "Shfaq Vitin në Numër")]
        public bool ShowYearInNumber { get; set; } = true;

        [Display(Name = "Ndaj me Vije (Slash)")]
        public bool UseSeparatorSlash { get; set; } = true;

        [StringLength(200)]
        [Display(Name = "Emri i Institucionit")]
        public string? InstitutionName { get; set; }

        [StringLength(100)]
        [Display(Name = "Kodi i Institucionit")]
        public string? InstitutionCode { get; set; }

        [StringLength(500)]
        [Display(Name = "Adresa e Institucionit")]
        public string? InstitutionAddress { get; set; }

        [StringLength(20)]
        [Display(Name = "Telefoni i Institucionit")]
        public string? InstitutionPhone { get; set; }

        [StringLength(100)]
        [EmailAddress]
        [Display(Name = "Email i Institucionit")]
        public string? InstitutionEmail { get; set; }

        [StringLength(200)]
        [Display(Name = "Website i Institucionit")]
        public string? InstitutionWebsite { get; set; }

        [Display(Name = "Data e Fillimit të Vitit Fiskal")]
        [DataType(DataType.Date)]
        public DateTime? FiscalYearStart { get; set; }

        [Display(Name = "Data e Mbylljes së Vitit Fiskal")]
        [DataType(DataType.Date)]
        public DateTime? FiscalYearEnd { get; set; }

        [Display(Name = "Është Aktiv")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Është i Mbyllur")]
        public bool IsClosed { get; set; } = false;

        [Display(Name = "Data e Mbylljes")]
        public DateTime? ClosedDate { get; set; }

        [StringLength(450)]
        [Display(Name = "Mbyllur nga")]
        public string? ClosedBy { get; set; }

        [StringLength(500)]
        [Display(Name = "Shënime")]
        [DataType(DataType.MultilineText)]
        public string? Notes { get; set; }

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
    }
}