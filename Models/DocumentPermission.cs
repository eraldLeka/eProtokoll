using Microsoft.EntityFrameworkCore.Migrations;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eProtokoll.Models
{
    public class DocumentPermission
    {
        [Key]
        public int Id { get; set; }

        public int DocumentId { get; set; }
        [ForeignKey("DocumentId")]
        public virtual Document Document { get; set; }

        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual Users User { get; set; }
    }
}