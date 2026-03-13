using eProtokoll.Models;
using Microsoft.EntityFrameworkCore;

namespace eProtokoll.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // DbSet për të gjitha modelet
        public DbSet<Institution> Institutions { get; set; }
        public DbSet<ProtocolSettings> ProtocolSettings { get; set; }
        public DbSet<Document> Documents { get; set; }
        public DbSet<IncomingDocument> IncomingDocuments { get; set; }
        public DbSet<OutgoingDocument> OutgoingDocuments { get; set; }
        public DbSet<InternalDocument> InternalDocuments { get; set; }
        public DbSet<DocumentTracking> DocumentTrackings { get; set; }
        public DbSet<DocumentAttachment> DocumentAttachments { get; set; }
        public DbSet<DocumentPermission> DocumentPermissions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ==================== USERS ====================
            modelBuilder.Entity<Users>(entity =>
            {
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.UserName).IsUnique();

                entity.HasMany(u => u.CreatedDocuments)
                    .WithOne(d => d.Creator)
                    .HasForeignKey(d => d.CreatedBy)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(u => u.AssignedDocuments)
                    .WithOne()
                    .HasForeignKey(t => t.AssignedToUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ==================== INSTITUTION ====================
            modelBuilder.Entity<Institution>(entity =>
            {
                entity.HasIndex(e => e.Name);
                entity.HasIndex(e => e.TaxCode).IsUnique();

                entity.Property(e => e.Name).IsRequired();
                entity.Property(e => e.Type).IsRequired();
            });

            // ==================== PROTOCOL SETTINGS ====================
            modelBuilder.Entity<ProtocolSettings>(entity =>
            {
                entity.HasIndex(e => e.Year).IsUnique();

                entity.Property(e => e.Year).IsRequired();
                entity.Property(e => e.IncomingCurrentNumber).IsRequired();
                entity.Property(e => e.OutgoingCurrentNumber).IsRequired();
                entity.Property(e => e.InternalCurrentNumber).IsRequired();
            });

            // ==================== DOCUMENT (Base Class) ====================
            modelBuilder.Entity<Document>(entity =>
            {
                entity.HasIndex(e => e.ProtocolNumber).IsUnique();
                entity.HasIndex(e => e.ProtocolDate);
                entity.HasIndex(e => e.DocumentType);

                entity.Property(e => e.ProtocolNumber).IsRequired();
                entity.Property(e => e.Subject).IsRequired().HasMaxLength(500);

                entity.Ignore(e => e.Discriminator);

                entity.HasMany(d => d.Attachments)
                    .WithOne(a => a.Document)
                    .HasForeignKey(a => a.DocumentId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany<DocumentTracking>()
                    .WithOne()
                    .HasForeignKey(t => t.DocumentId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ==================== INCOMING DOCUMENT ====================
            modelBuilder.Entity<IncomingDocument>(entity =>
            {
                entity.HasIndex(e => e.InstitutionId);
                entity.HasIndex(e => e.ReceivedDate);

                entity.HasOne(d => d.Institution)
                    .WithMany(i => i.IncomingDocuments)
                    .HasForeignKey(d => d.InstitutionId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.ResponseDocument)
                    .WithOne(o => o.OriginalIncomingDocument)
                    .HasForeignKey<IncomingDocument>(i => i.ResponseDocumentId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ==================== OUTGOING DOCUMENT ====================
            modelBuilder.Entity<OutgoingDocument>(entity =>
            {
                entity.HasIndex(e => e.InstitutionId);

                entity.HasOne(d => d.Institution)
                    .WithMany(i => i.OutgoingDocuments)
                    .HasForeignKey(d => d.InstitutionId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ==================== INTERNAL DOCUMENT ====================
            modelBuilder.Entity<InternalDocument>(entity =>
            {
                entity.HasIndex(e => e.FromDepartment);
                entity.HasIndex(e => e.ToDepartment);
            });

            // ==================== DOCUMENT TRACKING ====================
            modelBuilder.Entity<DocumentTracking>(entity =>
            {
                entity.HasIndex(e => e.DocumentId);
                entity.HasIndex(e => e.AssignedToUserId);
                entity.HasIndex(e => e.DueDate);

                entity.HasOne<Users>()
                    .WithMany()
                    .HasForeignKey(t => t.AssignedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Ignore(t => t.AssignedToUser);
                entity.Ignore(t => t.AssignedByUser);
                entity.Ignore(t => t.DocumentProtocolNumber);
                entity.Ignore(t => t.DocumentSubject);
                entity.Ignore(t => t.DocumentDiscriminator);
            });

            // ==================== DOCUMENT ATTACHMENT ====================
            modelBuilder.Entity<DocumentAttachment>(entity =>
            {
                entity.HasIndex(e => e.DocumentId);
                entity.HasIndex(e => e.UploadedDate);

                entity.Property(e => e.FileName).IsRequired().HasMaxLength(255);
                entity.Property(e => e.FilePath).IsRequired().HasMaxLength(500);
                entity.Property(e => e.FileSize).IsRequired();

                entity.HasOne(a => a.Uploader)
                    .WithMany()
                    .HasForeignKey(a => a.UploadedBy)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ==================== DOCUMENT PERMISSION ====================
            modelBuilder.Entity<DocumentPermission>(entity =>
            {
                entity.HasOne(dp => dp.Document)
                    .WithMany()
                    .HasForeignKey(dp => dp.DocumentId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(dp => dp.User)
                    .WithMany()
                    .HasForeignKey(dp => dp.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ==================== DISCRIMINATOR ====================
            modelBuilder.Entity<Document>()
                .HasDiscriminator<string>("Discriminator")
                .HasValue<Document>("Document")
                .HasValue<IncomingDocument>("IncomingDocument")
                .HasValue<OutgoingDocument>("OutgoingDocument")
                .HasValue<InternalDocument>("InternalDocument");

            // ==================== SEED DATA ====================
            modelBuilder.Entity<ProtocolSettings>().HasData(
                new ProtocolSettings
                {
                    ProtocolSettingsId = 1,
                    Year = DateTime.Now.Year,
                    IncomingStartNumber = 1,
                    IncomingCurrentNumber = 1,
                    IncomingPrefix = "H",
                    OutgoingStartNumber = 1,
                    OutgoingCurrentNumber = 1,
                    OutgoingPrefix = "D",
                    InternalStartNumber = 1,
                    InternalCurrentNumber = 1,
                    InternalPrefix = "B",
                    ProtocolNumberFormat = "{PREFIX}-{NUMBER}/{YEAR}",
                    NumberPadding = 4,
                    AutoResetYearly = true,
                    AllowManualEdit = false,
                    ShowYearInNumber = true,
                    UseSeparatorSlash = true,
                    IsActive = true,
                    IsClosed = false,
                    CreatedDate = DateTime.Now
                }
            );
        }
    }
}