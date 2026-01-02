using System.ComponentModel.DataAnnotations;

namespace eProtokoll.Models
{
    /// Institucionet e jashtme me të cilat komunikon organizata

    public class Institution
    {
        [Key]
        public int InstitutionId { get; set; }

        [Required(ErrorMessage = "Emri i institucionit është i detyrueshëm")]
        [StringLength(200)]
        [Display(Name = "Emri i institucionit")]
        public string Name { get; set; }

        [StringLength(100)]
        [Display(Name = "Emri i Shkurtuar")]
        public string ShortName { get; set; }

        [Required]
        [Display(Name ="Lloji i instutuicionit")]
        public InstitutionType Type { get; set; }

        [StringLength(200)]
        [Display(Name ="Adresa")]
        public string? Adress { get; set; }

        [StringLength(50)]
        [Display(Name = "Qyteti")]
        public string? City { get; set; }

        [StringLength(20)]
        [Display(Name ="Kodi Postar")]
        public string? PostalCode { get; set; }

        [StringLength(50)]
        [Display(Name = "Shteti")]
        public string? Country { get; set; } = "Albania";

        [StringLength(20)]
        [Phone(ErrorMessage = "Numri i telefonit nuk është valid")]
        [Display(Name ="Telefoni")]
        public string? Phone { get; set; }

        [StringLength(20)]
        [Display(Name ="Faksi")]
        public string? Fax { get; set; }

        [StringLength(100)]
        [EmailAddress(ErrorMessage = "Email-i nuk eshte valid")]
        [Display(Name ="Email")]
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

        [StringLength(20)]
        [Display(Name = "Telefoni i Kontaktit")]
        public string? ContactPhone { get; set; }


        [StringLength(100)]
        [EmailAddress]
        [Display(Name = "Email i Kontaktit")]
        public string? ContactEmail { get; set; }

        [StringLength(20)]
        [Display(Name = "NIPT/Kodi Fiskal")]
        public string? TaxCode { get; set; }

        [Display(Name = "Aktiv")]
        public bool IsActive { get; set; } = true;

        [StringLength(500)]
        [Display(Name = "Shënime")]
        public string? Notes { get; set; }

        [Display(Name = "Data e Krijimit")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Display(Name = "Data e Modifikimit")]
        public DateTime? ModifiedDate { get; set; }

        [StringLength(36)]
        [Display(Name = "Krijuar nga")]
        public string? CreatedBy { get; set; }

        [StringLength(36)]
        [Display(Name = "Modifikuar nga")]
        public string? ModifiedBy { get; set; }


        ///Navigation Properties
        [Display(Name = "Dokumentet Hyrese")]
        public virtual ICollection<IncomingDocument>? IncomingDocuments { get; set; }

        [Display(Name ="Dokumentet Dalese")]
        public virtual ICollection<OutgoingDocument>? OutgoingDocuments { get; set; }


        ///Llojet e institucioneve
        ///
        public enum InstitutionType
        {
            [Display(Name="Institucion Shteteror")]
            Government = 1,

            [Display(Name ="Bashki/Komune")]
            Municipality = 2,

            [Display(Name ="Ministri")]
            Ministry = 3,

            [Display (Name ="Kompani private")]
            Private = 4,

            [Display(Name ="OJF/OJQ")]
            NGO = 5,

            [Display(Name ="Organizate Nderkombetare")]
            International = 6,

            [Display(Name= "Instuticion Arsimor" )]
            Educational = 7,

            [Display(Name ="Instuticion Shendetesor")]
            Healthcare = 8,

            [Display(Name = "Media")]
            Media = 9,

            [Display(Name = "Tjeter")]
            Other = 10
        }
            
        




    }
}
