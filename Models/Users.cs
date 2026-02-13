using System.ComponentModel.DataAnnotations;

namespace eProtokoll.Models
{
    public class Users
    {
        public int Id { get; set; }

        //login fields
        [Required]
        public string UserName { get; set; }
       
        [Required]
        public string PasswordHash { get; set; } = null!;

        [Required]
        [StringLength(50)]
        [Display(Name = "Emri")]
        public string FirstName { get; set; } = null!;

        [Required]
        [StringLength(50)]
        [Display(Name = "Mbiemri")]
        public string LastName { get; set; } = null!;
        public string FullName => $"{FirstName} {LastName}";
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        public int? PhoneNumber { get; set; }

        [StringLength(100)]
        [Display(Name = "Pozicioni")]
        public string? Position { get; set; }

        [StringLength(100)]
        [Display(Name = "Departmenti")]
        public string? Department { get; set; }


        [Required]
        [Display(Name = "Roli")]
        public UserRole Role { get; set; }


        [Display(Name = "Aktiv")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Data e krijimit")]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [Display(Name = "Data e modifikimit")]
        public DateTime? ModifiedDate { get; set; }

        [StringLength(36)]
        [Display(Name = "Modifikuar nga")]
        public string? ModifiedBy { get; set; }


        public virtual ICollection<Document> CreatedDocuments { get; set; } = new List<Document>();
        public virtual ICollection<DocumentTracking> AssignedDocuments { get; set; } = new List<DocumentTracking>();
        public virtual ICollection<Deadline> Deadlines { get; set; } = new List<Deadline>();


        public enum UserRole
        {
            [Display(Name = "Administrator")]
            Administrator = 1,

            [Display(Name = "Menaxher")]
            Manager = 2,

            [Display(Name = "Punonjës")]
            Employee = 3
        }
    }
}
