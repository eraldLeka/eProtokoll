using System.ComponentModel.DataAnnotations;

namespace eProtokoll.Models
{
    /// <summary>
    /// Institucionet e jashtme me të cilat komunikon organizata
    /// </summary>
    public class Institution
    {
        [Key]
        public int InstitutionId { get; set; }

        [Required(ErrorMessage = "Emri i institucionit është i detyrueshëm")]
        [StringLength(200)]
        [Display(Name = "Emri i Institucionit")]
        public string Name { get; set; }

        [StringLength(100)]
        [Display(Name = "Emri i Shkurtuar")]
        public string? ShortName { get; set; }

        [Required]
        [Display(Name = "Lloji i Institucionit")]
        public InstitutionType Type { get; set; }

        [StringLength(200)]
        [Display(Name = "Adresa")]
        public string? Adress { get; set; }

        [StringLength(20)]
        [Display(Name = "Kodi Postar")]
        public string? PostalCode { get; set; }

        [StringLength(50)]
        [Display(Name = "Shteti")]
        public string? Country { get; set; } = "Albania";

        [StringLength(20)]
        [Phone(ErrorMessage = "Numri i telefonit nuk është valid")]
        [Display(Name = "Telefoni")]
        public string? Phone { get; set; }

        [StringLength(100)]
        [EmailAddress(ErrorMessage = "Email-i nuk është valid")]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [StringLength(200)]
        [Url(ErrorMessage = "URL-ja nuk është valide")]
        [Display(Name = "Website")]
        public string? Website { get; set; }

        [StringLength(100)]
        [Display(Name = "Personi i Kontaktit")]
        public string? ContactPerson { get; set; }

        [StringLength(100)]
        [Display(Name = "Pozicioni i Kontaktit")]
        public string? ContactPosition { get; set; }

        [StringLength(100)]
        [EmailAddress]
        [Display(Name = "Email i Kontaktit")]
        public string? ContactEmail { get; set; }

        [Display(Name = "Aktiv")]
        public bool IsActive { get; set; } = true;

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
        [Display(Name = "Dokumentet Hyrëse")]
        public virtual ICollection<IncomingDocument>? IncomingDocuments { get; set; }

        [Display(Name = "Dokumentet Dalëse")]
        public virtual ICollection<OutgoingDocument>? OutgoingDocuments { get; set; }
    }

    public enum InstitutionType
    {
        [Display(Name = "Institucion Shtetëror")]
        Government = 1,

        [Display(Name = "Bashki/Komunë")]
        Municipality = 2,

        [Display(Name = "Ministri")]
        Ministry = 3,

        [Display(Name = "Kompani Private")]
        Private = 4,

        [Display(Name = "OJF/OJQ")]
        NGO = 5,

        [Display(Name = "Organizatë Ndërkombëtare")]
        International = 6,

        [Display(Name = "Institucion Arsimor")]
        Educational = 7,

        [Display(Name = "Institucion Shëndetësor")]
        Healthcare = 8,

        [Display(Name = "Media")]
        Media = 9,

        [Display(Name = "Tjetër")]
        Other = 10
    }
}