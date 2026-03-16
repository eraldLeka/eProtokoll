using eProtokoll.Models;
using Microsoft.Data.SqlClient;

namespace eProtokoll.Services.Mappers
{
    public static class ReaderHelper
    {
        public static string? GetString(SqlDataReader reader, string column)
        {
            int i = reader.GetOrdinal(column);
            return reader.IsDBNull(i) ? null : reader.GetString(i);
        }

        public static int? GetInt(SqlDataReader reader, string column)
        {
            int i = reader.GetOrdinal(column);
            return reader.IsDBNull(i) ? null : reader.GetInt32(i);
        }

        public static DateTime? GetDate(SqlDataReader reader, string column)
        {
            int i = reader.GetOrdinal(column);
            return reader.IsDBNull(i) ? null : reader.GetDateTime(i);
        }

        public static bool GetBool(SqlDataReader reader, string column)
        {
            int i = reader.GetOrdinal(column);
            return !reader.IsDBNull(i) && reader.GetBoolean(i);
        }
    }

    // ================= DOCUMENT BASE =================

    public static class DocumentMapper
    {
        private static void MapBase(SqlDataReader reader, Document document)
        {
            document.DocumentId = reader.GetInt32(reader.GetOrdinal("DocumentId"));
            document.DocumentNumber = reader.GetInt32(reader.GetOrdinal("DocumentNumber"));
            document.Year = reader.GetInt32(reader.GetOrdinal("Year"));
            document.DocumentType = (DocumentType)reader.GetInt32(reader.GetOrdinal("DocumentType"));
            document.Subject = reader.GetString(reader.GetOrdinal("Subject"));
            document.Content = ReaderHelper.GetString(reader, "Content");
            document.Classification = (Classification)reader.GetInt32(reader.GetOrdinal("Classification"));
            document.Priority = (Priority)reader.GetInt32(reader.GetOrdinal("Priority"));
            document.Notes = ReaderHelper.GetString(reader, "Notes");
            document.HasAttachments = ReaderHelper.GetBool(reader, "HasAttachments");
            document.CreatedBy = reader.GetInt32(reader.GetOrdinal("CreatedBy"));
            document.CreatedDate = reader.GetDateTime(reader.GetOrdinal("CreatedDate"));
        }

        public static Document MapToDocument(SqlDataReader reader)
        {
            var document = new Document();
            MapBase(reader, document);
            return document;
        }

        public static IncomingDocument MapToIncomingDocument(SqlDataReader reader)
        {
            var document = new IncomingDocument();
            MapBase(reader, document);

            document.InstitutionId = reader.GetInt32(reader.GetOrdinal("InstitutionId"));
            document.SenderName = reader.GetString(reader.GetOrdinal("SenderName"));
            document.ReceivedDate = reader.GetDateTime(reader.GetOrdinal("ReceivedDate"));
            document.ReceivedBy = ReaderHelper.GetInt(reader, "ReceivedBy");
            document.OriginalDocumentNumber = ReaderHelper.GetString(reader, "OriginalDocumentNumber");
            document.OriginalDocumentDate = ReaderHelper.GetDate(reader, "OriginalDocumentDate");
            document.ResponseDeadline = ReaderHelper.GetDate(reader, "ResponseDeadline");
            document.ResponseDate = ReaderHelper.GetDate(reader, "ResponseDate");
            document.ResponseDocumentId = ReaderHelper.GetInt(reader, "ResponseDocumentId");

            return document;
        }

        public static OutgoingDocument MapToOutgoingDocument(SqlDataReader reader)
        {
            var document = new OutgoingDocument();
            MapBase(reader, document);

            document.InstitutionId = reader.GetInt32(reader.GetOrdinal("InstitutionId"));
            document.RecipientName = reader.GetString(reader.GetOrdinal("RecipientName"));
            document.IsResponse = ReaderHelper.GetBool(reader, "IsResponse");
            document.OriginalIncomingDocumentId = ReaderHelper.GetInt(reader, "OriginalIncomingDocumentId");
            document.ArchiveLocation = ReaderHelper.GetString(reader, "ArchiveLocation");
            return document;
        }

        public static InternalDocument MapToInternalDocument(SqlDataReader reader)
        {
            var document = new InternalDocument();
            MapBase(reader, document);

            document.FromDepartment = ReaderHelper.GetString(reader, "FromDepartment");
            document.ToDepartment = ReaderHelper.GetString(reader, "ToDepartment");
            return document;
        }
    }

    // ================= ATTACHMENT =================

    public static class AttachmentMapper
    {
        public static DocumentAttachment Map(SqlDataReader reader)
        {
            return new DocumentAttachment
            {
                AttachmentId = reader.GetInt32(reader.GetOrdinal("AttachmentId")),
                DocumentId = reader.GetInt32(reader.GetOrdinal("DocumentId")),
                FileName = reader.GetString(reader.GetOrdinal("FileName")),
                OriginalFileName = reader.GetString(reader.GetOrdinal("OriginalFileName")),
                FilePath = reader.GetString(reader.GetOrdinal("FilePath")),
                FileSize = reader.GetInt64(reader.GetOrdinal("FileSize")),
                FileExtension = ReaderHelper.GetString(reader, "FileExtension"),
                ContentType = ReaderHelper.GetString(reader, "ContentType"),
                UploadedDate = reader.GetDateTime(reader.GetOrdinal("UploadedDate")),
                UploadedBy = reader.GetInt32(reader.GetOrdinal("UploadedBy")),
                Category = (FileCategory)reader.GetInt32(reader.GetOrdinal("Category")),
                Description = ReaderHelper.GetString(reader, "Description"),
            };
        }
    }
    // ================= TRACKING =================

    public static class TrackingMapper
    {
        public static DocumentTracking Map(SqlDataReader reader)
        {
            return new DocumentTracking
            {
                TrackingId = reader.GetInt32(reader.GetOrdinal("TrackingId")),
                DocumentId = reader.GetInt32(reader.GetOrdinal("DocumentId")),
                AssignedToUserId = reader.GetInt32(reader.GetOrdinal("AssignedToUserId")),
                AssignedByUserId = reader.GetInt32(reader.GetOrdinal("AssignedByUserId")),
                AssignedDate = reader.GetDateTime(reader.GetOrdinal("AssignedDate")),
                Priority = (Priority)reader.GetInt32(reader.GetOrdinal("Priority")),
                DueDate = ReaderHelper.GetDate(reader, "DueDate"),
                Notes = ReaderHelper.GetString(reader, "Notes"),
                CompletedDate = ReaderHelper.GetDate(reader, "CompletedDate"),
                CreatedDate = reader.GetDateTime(reader.GetOrdinal("CreatedDate"))
            };
        }
    }
    // ================= USER =================

    public static class UserMapper
    {
        public static Users Map(SqlDataReader reader)
        {
            return new Users
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                UserName = reader.GetString(reader.GetOrdinal("UserName")),
                Email = ReaderHelper.GetString(reader, "Email"),
                FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                LastName = reader.GetString(reader.GetOrdinal("LastName")),
                Position = ReaderHelper.GetString(reader, "Position"),
                Department = ReaderHelper.GetString(reader, "Department"),
                PhoneNumber = ReaderHelper.GetString(reader, "PhoneNumber"),
                IsActive = ReaderHelper.GetBool(reader, "IsActive"),
                CreatedDate = ReaderHelper.GetDate(reader, "CreatedDate") ?? DateTime.Now,
                ModifiedDate = ReaderHelper.GetDate(reader, "ModifiedDate")
            };
        }
    }

    // ================= INSTITUTION =================

    public static class InstitutionMapper
    {
        public static Institution Map(SqlDataReader reader)
        {
            return new Institution
            {
                InstitutionId = reader.GetInt32(reader.GetOrdinal("InstitutionId")),
                Name = reader.GetString(reader.GetOrdinal("Name")),
                ShortName = ReaderHelper.GetString(reader, "ShortName"),
                Type = (InstitutionType)reader.GetInt32(reader.GetOrdinal("Type")),
                Adress = ReaderHelper.GetString(reader, "Adress"),
                PostalCode = ReaderHelper.GetString(reader, "PostalCode"),
                Country = ReaderHelper.GetString(reader, "Country"),
                Phone = ReaderHelper.GetString(reader, "Phone"),
                Email = ReaderHelper.GetString(reader, "Email"),
                Website = ReaderHelper.GetString(reader, "Website"),
                ContactPerson = ReaderHelper.GetString(reader, "ContactPerson"),
                ContactPosition = ReaderHelper.GetString(reader, "ContactPosition"),
                ContactEmail = ReaderHelper.GetString(reader, "ContactEmail"),
                CreatedDate = reader.GetDateTime(reader.GetOrdinal("CreatedDate")),
                ModifiedDate = ReaderHelper.GetDate(reader, "ModifiedDate"),
                CreatedBy = ReaderHelper.GetString(reader, "CreatedBy"),
                ModifiedBy = ReaderHelper.GetString(reader, "ModifiedBy")
            };
        }
    }
}