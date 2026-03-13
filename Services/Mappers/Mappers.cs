using eProtokoll.Models;
using Microsoft.Data.SqlClient;
namespace eProtokoll.Services.Mappers
{
    // ==================== DOCUMENT MAPPERS ====================
    public static class DocumentMapper
    {
        public static Document MapToDocument(SqlDataReader reader)
        {
            return new Document
            {
                DocumentId = reader.GetInt32(reader.GetOrdinal("DocumentId")),
                ProtocolNumber = reader.GetString(reader.GetOrdinal("ProtocolNumber")),
                ProtocolDate = reader.GetDateTime(reader.GetOrdinal("ProtocolDate")),
                ProtocolTime = reader.GetTimeSpan(reader.GetOrdinal("ProtocolTime")),
                DocumentType = (DocumentType)reader.GetInt32(reader.GetOrdinal("DocumentType")),
                Subject = reader.GetString(reader.GetOrdinal("Subject")),
                Content = reader.IsDBNull(reader.GetOrdinal("Content")) ? null : reader.GetString(reader.GetOrdinal("Content")),
                Classification = (Classification)reader.GetInt32(reader.GetOrdinal("Classification")),
                Status = (DocumentStatus)reader.GetInt32(reader.GetOrdinal("Status")),
                Priority = (Priority)reader.GetInt32(reader.GetOrdinal("Priority")),
                Notes = reader.IsDBNull(reader.GetOrdinal("Notes")) ? null : reader.GetString(reader.GetOrdinal("Notes")),
                HasAttachments = !reader.IsDBNull(reader.GetOrdinal("HasAttachments"))
                    && reader.GetBoolean(reader.GetOrdinal("HasAttachments")),
                CreatedBy = reader.GetInt32(reader.GetOrdinal("CreatedBy")),
                CreatedDate = reader.GetDateTime(reader.GetOrdinal("CreatedDate")),
            };
        }
        public static IncomingDocument MapToIncomingDocument(SqlDataReader reader)
        {
            return new IncomingDocument
            {
                DocumentId = reader.GetInt32(reader.GetOrdinal("DocumentId")),
                ProtocolNumber = reader.GetString(reader.GetOrdinal("ProtocolNumber")),
                ProtocolDate = reader.GetDateTime(reader.GetOrdinal("ProtocolDate")),
                ProtocolTime = reader.GetTimeSpan(reader.GetOrdinal("ProtocolTime")),
                DocumentType = (DocumentType)reader.GetInt32(reader.GetOrdinal("DocumentType")),
                Subject = reader.GetString(reader.GetOrdinal("Subject")),
                Content = reader.IsDBNull(reader.GetOrdinal("Content")) ? null : reader.GetString(reader.GetOrdinal("Content")),
                Classification = (Classification)reader.GetInt32(reader.GetOrdinal("Classification")),
                Status = (DocumentStatus)reader.GetInt32(reader.GetOrdinal("Status")),
                Priority = (Priority)reader.GetInt32(reader.GetOrdinal("Priority")),
                Notes = reader.IsDBNull(reader.GetOrdinal("Notes")) ? null : reader.GetString(reader.GetOrdinal("Notes")),
                HasAttachments = !reader.IsDBNull(reader.GetOrdinal("HasAttachments"))
                    && reader.GetBoolean(reader.GetOrdinal("HasAttachments")),
                CreatedBy = reader.GetInt32(reader.GetOrdinal("CreatedBy")),
                CreatedDate = reader.GetDateTime(reader.GetOrdinal("CreatedDate")),
                InstitutionId = reader.GetInt32(reader.GetOrdinal("InstitutionId")),
                SenderName = reader.GetString(reader.GetOrdinal("SenderName")),
                ReceivedDate = reader.GetDateTime(reader.GetOrdinal("ReceivedDate")),
                ReceivedBy = reader.IsDBNull(reader.GetOrdinal("ReceivedBy")) ? null : (int?)reader.GetInt32(reader.GetOrdinal("ReceivedBy")),
                OriginalDocumentNumber = reader.IsDBNull(reader.GetOrdinal("OriginalDocumentNumber")) ? null : reader.GetString(reader.GetOrdinal("OriginalDocumentNumber")),
                OriginalDocumentDate = reader.IsDBNull(reader.GetOrdinal("OriginalDocumentDate")) ? null : reader.GetDateTime(reader.GetOrdinal("OriginalDocumentDate")),
                ResponseDeadline = reader.IsDBNull(reader.GetOrdinal("ResponseDeadline")) ? null : reader.GetDateTime(reader.GetOrdinal("ResponseDeadline")),

                IsResponded = !reader.IsDBNull(reader.GetOrdinal("IsResponded"))
                    && reader.GetBoolean(reader.GetOrdinal("IsResponded")),
                ResponseDate = reader.IsDBNull(reader.GetOrdinal("ResponseDate")) ? null : reader.GetDateTime(reader.GetOrdinal("ResponseDate")),
                ResponseDocumentId = reader.IsDBNull(reader.GetOrdinal("ResponseDocumentId")) ? null : reader.GetInt32(reader.GetOrdinal("ResponseDocumentId"))
            };
        }

        public static OutgoingDocument MapToOutgoingDocument(SqlDataReader reader)
        {
            return new OutgoingDocument
            {
                // Base Document properties
                DocumentId = reader.GetInt32(reader.GetOrdinal("DocumentId")),
                ProtocolNumber = reader.GetString(reader.GetOrdinal("ProtocolNumber")),
                ProtocolDate = reader.GetDateTime(reader.GetOrdinal("ProtocolDate")),
                ProtocolTime = reader.GetTimeSpan(reader.GetOrdinal("ProtocolTime")),
                DocumentType = (DocumentType)reader.GetInt32(reader.GetOrdinal("DocumentType")),
                Subject = reader.GetString(reader.GetOrdinal("Subject")),
                Content = reader.IsDBNull(reader.GetOrdinal("Content")) ? null : reader.GetString(reader.GetOrdinal("Content")),
                Classification = (Classification)reader.GetInt32(reader.GetOrdinal("Classification")),
                Status = (DocumentStatus)reader.GetInt32(reader.GetOrdinal("Status")),
                Priority = (Priority)reader.GetInt32(reader.GetOrdinal("Priority")),
                Notes = reader.IsDBNull(reader.GetOrdinal("Notes")) ? null : reader.GetString(reader.GetOrdinal("Notes")),
                HasAttachments = !reader.IsDBNull(reader.GetOrdinal("HasAttachments"))
                    && reader.GetBoolean(reader.GetOrdinal("HasAttachments")),
                CreatedBy = reader.GetInt32(reader.GetOrdinal("CreatedBy")),
                CreatedDate = reader.GetDateTime(reader.GetOrdinal("CreatedDate")),
                InstitutionId = reader.GetInt32(reader.GetOrdinal("InstitutionId")),
                RecipientName = reader.GetString(reader.GetOrdinal("RecipientName")),
                IsResponse = !reader.IsDBNull(reader.GetOrdinal("IsResponse"))
                    && reader.GetBoolean(reader.GetOrdinal("IsResponse")),
                OriginalIncomingDocumentId = reader.IsDBNull(reader.GetOrdinal("OriginalIncomingDocumentId")) ? null : reader.GetInt32(reader.GetOrdinal("OriginalIncomingDocumentId")),
                ArchiveLocation = reader.IsDBNull(reader.GetOrdinal("ArchiveLocation")) ? null : reader.GetString(reader.GetOrdinal("ArchiveLocation"))
            };
        }

        public static InternalDocument MapToInternalDocument(SqlDataReader reader)
        {
            return new InternalDocument
            {
                // Base Document properties
                DocumentId = reader.GetInt32(reader.GetOrdinal("DocumentId")),
                ProtocolNumber = reader.GetString(reader.GetOrdinal("ProtocolNumber")),
                ProtocolDate = reader.GetDateTime(reader.GetOrdinal("ProtocolDate")),
                ProtocolTime = reader.GetTimeSpan(reader.GetOrdinal("ProtocolTime")),
                DocumentType = (DocumentType)reader.GetInt32(reader.GetOrdinal("DocumentType")),
                Subject = reader.GetString(reader.GetOrdinal("Subject")),
                Content = reader.IsDBNull(reader.GetOrdinal("Content")) ? null : reader.GetString(reader.GetOrdinal("Content")),
                Classification = (Classification)reader.GetInt32(reader.GetOrdinal("Classification")),
                Status = (DocumentStatus)reader.GetInt32(reader.GetOrdinal("Status")),
                Priority = (Priority)reader.GetInt32(reader.GetOrdinal("Priority")),
                Notes = reader.IsDBNull(reader.GetOrdinal("Notes")) ? null : reader.GetString(reader.GetOrdinal("Notes")),
                HasAttachments = reader.IsDBNull(reader.GetOrdinal("HasAttachments")) ? false : reader.GetBoolean(reader.GetOrdinal("HasAttachments")),
                CreatedBy = reader.GetInt32(reader.GetOrdinal("CreatedBy")),
                CreatedDate = reader.GetDateTime(reader.GetOrdinal("CreatedDate")),
                FromDepartment = reader.IsDBNull(reader.GetOrdinal("FromDepartment")) ? null : reader.GetString(reader.GetOrdinal("FromDepartment")),
                ToDepartment = reader.IsDBNull(reader.GetOrdinal("ToDepartment")) ? null : reader.GetString(reader.GetOrdinal("ToDepartment")),
            };
        }
    }

    // ==================== ATTACHMENT MAPPER ====================

    public static class AttachmentMapper
    {
        public static DocumentAttachment MapToDocumentAttachment(SqlDataReader reader)
        {
            return new DocumentAttachment
            {
                AttachmentId = reader.GetInt32(reader.GetOrdinal("AttachmentId")),
                DocumentId = reader.GetInt32(reader.GetOrdinal("DocumentId")),
                FileName = reader.GetString(reader.GetOrdinal("FileName")),
                OriginalFileName = reader.GetString(reader.GetOrdinal("OriginalFileName")),
                FilePath = reader.GetString(reader.GetOrdinal("FilePath")),
                FileSize = reader.GetInt64(reader.GetOrdinal("FileSize")),
                FileExtension = reader.IsDBNull(reader.GetOrdinal("FileExtension")) ? null : reader.GetString(reader.GetOrdinal("FileExtension")),
                ContentType = reader.IsDBNull(reader.GetOrdinal("ContentType")) ? null : reader.GetString(reader.GetOrdinal("ContentType")),
                UploadedDate = reader.GetDateTime(reader.GetOrdinal("UploadedDate")),
                UploadedBy = reader.GetInt32(reader.GetOrdinal("UploadedBy")),
                Category = (FileCategory)reader.GetInt32(reader.GetOrdinal("Category")),
                Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
                DisplayOrder = reader.GetInt32(reader.GetOrdinal("DisplayOrder")),
                IsPrimaryDocument = reader.GetBoolean(reader.GetOrdinal("IsPrimaryDocument"))
            };
        }
    }

    public static class TrackingMapper
    {
        public static DocumentTracking MapToDocumentTracking(SqlDataReader reader)
        {
            return new DocumentTracking
            {
                TrackingId = reader.GetInt32(reader.GetOrdinal("TrackingId")),
                DocumentId = reader.GetInt32(reader.GetOrdinal("DocumentId")),
                AssignedToUserId = reader.GetInt32(reader.GetOrdinal("AssignedToUserId")),
                AssignedByUserId = reader.GetInt32(reader.GetOrdinal("AssignedByUserId")),
                AssignedDate = reader.GetDateTime(reader.GetOrdinal("AssignedDate")),
                Priority = (Priority)reader.GetInt32(reader.GetOrdinal("Priority")),
                DueDate = reader.IsDBNull(reader.GetOrdinal("DueDate")) ? null : reader.GetDateTime(reader.GetOrdinal("DueDate")),
                Notes = reader.IsDBNull(reader.GetOrdinal("Notes")) ? null : reader.GetString(reader.GetOrdinal("Notes")),
                CompletedDate = reader.IsDBNull(reader.GetOrdinal("CompletedDate")) ? null : reader.GetDateTime(reader.GetOrdinal("CompletedDate")),
                IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                CreatedDate = reader.GetDateTime(reader.GetOrdinal("CreatedDate"))
            };
        }
    }

    // ==================== USER MAPPER ====================
    public static class UserMapper
    {
        public static Users MapToApplicationUser(SqlDataReader reader)
        {
            return new Users
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                UserName = reader.GetString(reader.GetOrdinal("UserName")),
                Email = reader.IsDBNull(reader.GetOrdinal("Email")) ? null : reader.GetString(reader.GetOrdinal("Email")),
                FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                LastName = reader.GetString(reader.GetOrdinal("LastName")),
                Position = reader.IsDBNull(reader.GetOrdinal("Position")) ? null : reader.GetString(reader.GetOrdinal("Position")),
                Department = reader.IsDBNull(reader.GetOrdinal("Department")) ? null : reader.GetString(reader.GetOrdinal("Department")),
                PhoneNumber = reader.IsDBNull(reader.GetOrdinal("PhoneNumber")) ? null : reader.GetString(reader.GetOrdinal("PhoneNumber")),
                IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                CreatedDate = reader.GetDateTime(reader.GetOrdinal("CreatedDate")),
                ModifiedDate = reader.IsDBNull(reader.GetOrdinal("ModifiedDate")) ? null : reader.GetDateTime(reader.GetOrdinal("ModifiedDate"))
            };
        }
    }
    // ==================== PROTOCOL SETTINGS MAPPER ====================

    public static class ProtocolSettingsMapper
    {
        public static ProtocolSettings MapToProtocolSettings(SqlDataReader reader)
        {
            return new ProtocolSettings
            {
                ProtocolSettingsId = reader.GetInt32(reader.GetOrdinal("ProtocolSettingsId")),
                Year = reader.GetInt32(reader.GetOrdinal("Year")),
                IncomingStartNumber = reader.GetInt32(reader.GetOrdinal("IncomingStartNumber")),
                IncomingCurrentNumber = reader.GetInt32(reader.GetOrdinal("IncomingCurrentNumber")),
                IncomingEndNumber = reader.IsDBNull(reader.GetOrdinal("IncomingEndNumber")) ? null : reader.GetInt32(reader.GetOrdinal("IncomingEndNumber")),
                IncomingPrefix = reader.IsDBNull(reader.GetOrdinal("IncomingPrefix")) ? null : reader.GetString(reader.GetOrdinal("IncomingPrefix")),
                IncomingSuffix = reader.IsDBNull(reader.GetOrdinal("IncomingSuffix")) ? null : reader.GetString(reader.GetOrdinal("IncomingSuffix")),
                OutgoingStartNumber = reader.GetInt32(reader.GetOrdinal("OutgoingStartNumber")),
                OutgoingCurrentNumber = reader.GetInt32(reader.GetOrdinal("OutgoingCurrentNumber")),
                OutgoingEndNumber = reader.IsDBNull(reader.GetOrdinal("OutgoingEndNumber")) ? null : reader.GetInt32(reader.GetOrdinal("OutgoingEndNumber")),
                OutgoingPrefix = reader.IsDBNull(reader.GetOrdinal("OutgoingPrefix")) ? null : reader.GetString(reader.GetOrdinal("OutgoingPrefix")),
                OutgoingSuffix = reader.IsDBNull(reader.GetOrdinal("OutgoingSuffix")) ? null : reader.GetString(reader.GetOrdinal("OutgoingSuffix")),
                InternalStartNumber = reader.GetInt32(reader.GetOrdinal("InternalStartNumber")),
                InternalCurrentNumber = reader.GetInt32(reader.GetOrdinal("InternalCurrentNumber")),
                InternalEndNumber = reader.IsDBNull(reader.GetOrdinal("InternalEndNumber")) ? null : reader.GetInt32(reader.GetOrdinal("InternalEndNumber")),
                InternalPrefix = reader.IsDBNull(reader.GetOrdinal("InternalPrefix")) ? null : reader.GetString(reader.GetOrdinal("InternalPrefix")),
                InternalSuffix = reader.IsDBNull(reader.GetOrdinal("InternalSuffix")) ? null : reader.GetString(reader.GetOrdinal("InternalSuffix")),
                ProtocolNumberFormat = reader.GetString(reader.GetOrdinal("ProtocolNumberFormat")),
                NumberPadding = reader.GetInt32(reader.GetOrdinal("NumberPadding")),
                AutoResetYearly = reader.GetBoolean(reader.GetOrdinal("AutoResetYearly")),
                AllowManualEdit = reader.GetBoolean(reader.GetOrdinal("AllowManualEdit")),
                ShowYearInNumber = reader.GetBoolean(reader.GetOrdinal("ShowYearInNumber")),
                UseSeparatorSlash = reader.GetBoolean(reader.GetOrdinal("UseSeparatorSlash")),
                InstitutionName = reader.IsDBNull(reader.GetOrdinal("InstitutionName")) ? null : reader.GetString(reader.GetOrdinal("InstitutionName")),
                InstitutionCode = reader.IsDBNull(reader.GetOrdinal("InstitutionCode")) ? null : reader.GetString(reader.GetOrdinal("InstitutionCode")),
                InstitutionAddress = reader.IsDBNull(reader.GetOrdinal("InstitutionAddress")) ? null : reader.GetString(reader.GetOrdinal("InstitutionAddress")),
                InstitutionPhone = reader.IsDBNull(reader.GetOrdinal("InstitutionPhone")) ? null : reader.GetString(reader.GetOrdinal("InstitutionPhone")),
                InstitutionEmail = reader.IsDBNull(reader.GetOrdinal("InstitutionEmail")) ? null : reader.GetString(reader.GetOrdinal("InstitutionEmail")),
                InstitutionWebsite = reader.IsDBNull(reader.GetOrdinal("InstitutionWebsite")) ? null : reader.GetString(reader.GetOrdinal("InstitutionWebsite")),
                FiscalYearStart = reader.IsDBNull(reader.GetOrdinal("FiscalYearStart")) ? null : reader.GetDateTime(reader.GetOrdinal("FiscalYearStart")),
                FiscalYearEnd = reader.IsDBNull(reader.GetOrdinal("FiscalYearEnd")) ? null : reader.GetDateTime(reader.GetOrdinal("FiscalYearEnd")),
                IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                IsClosed = reader.GetBoolean(reader.GetOrdinal("IsClosed")),
                ClosedDate = reader.IsDBNull(reader.GetOrdinal("ClosedDate")) ? null : reader.GetDateTime(reader.GetOrdinal("ClosedDate")),
                ClosedBy = reader.IsDBNull(reader.GetOrdinal("ClosedBy")) ? null : reader.GetString(reader.GetOrdinal("ClosedBy")),
                Notes = reader.IsDBNull(reader.GetOrdinal("Notes")) ? null : reader.GetString(reader.GetOrdinal("Notes")),
                CreatedDate = reader.GetDateTime(reader.GetOrdinal("CreatedDate")),
                ModifiedDate = reader.IsDBNull(reader.GetOrdinal("ModifiedDate")) ? null : reader.GetDateTime(reader.GetOrdinal("ModifiedDate")),
                CreatedBy = reader.IsDBNull(reader.GetOrdinal("CreatedBy")) ? null : reader.GetString(reader.GetOrdinal("CreatedBy")),
                ModifiedBy = reader.IsDBNull(reader.GetOrdinal("ModifiedBy")) ? null : reader.GetString(reader.GetOrdinal("ModifiedBy"))
            };
        }
    }

    // ==================== INSTITUTION MAPPER ====================
    public static class InstitutionMapper
    {
        public static Institution MapToInstitution(SqlDataReader reader)
        {
            return new Institution
            {
                InstitutionId = reader.GetInt32(reader.GetOrdinal("InstitutionId")),
                Name = reader.GetString(reader.GetOrdinal("Name")),
                ShortName = reader.IsDBNull(reader.GetOrdinal("ShortName")) ? null : reader.GetString(reader.GetOrdinal("ShortName")),
                Type = (InstitutionType)reader.GetInt32(reader.GetOrdinal("Type")),
                Adress = reader.IsDBNull(reader.GetOrdinal("Adress")) ? null : reader.GetString(reader.GetOrdinal("Adress")),
                City = reader.IsDBNull(reader.GetOrdinal("City")) ? null : reader.GetString(reader.GetOrdinal("City")),
                PostalCode = reader.IsDBNull(reader.GetOrdinal("PostalCode")) ? null : reader.GetString(reader.GetOrdinal("PostalCode")),
                Country = reader.IsDBNull(reader.GetOrdinal("Country")) ? null : reader.GetString(reader.GetOrdinal("Country")),
                Phone = reader.IsDBNull(reader.GetOrdinal("Phone")) ? null : reader.GetString(reader.GetOrdinal("Phone")),
                Fax = reader.IsDBNull(reader.GetOrdinal("Fax")) ? null : reader.GetString(reader.GetOrdinal("Fax")),
                Email = reader.IsDBNull(reader.GetOrdinal("Email")) ? null : reader.GetString(reader.GetOrdinal("Email")),
                Website = reader.IsDBNull(reader.GetOrdinal("Website")) ? null : reader.GetString(reader.GetOrdinal("Website")),
                ContactPerson = reader.IsDBNull(reader.GetOrdinal("ContactPerson")) ? null : reader.GetString(reader.GetOrdinal("ContactPerson")),
                ContactPosition = reader.IsDBNull(reader.GetOrdinal("ContactPosition")) ? null : reader.GetString(reader.GetOrdinal("ContactPosition")),
                ContactPhone = reader.IsDBNull(reader.GetOrdinal("ContactPhone")) ? null : reader.GetString(reader.GetOrdinal("ContactPhone")),
                ContactEmail = reader.IsDBNull(reader.GetOrdinal("ContactEmail")) ? null : reader.GetString(reader.GetOrdinal("ContactEmail")),
                TaxCode = reader.IsDBNull(reader.GetOrdinal("TaxCode")) ? null : reader.GetString(reader.GetOrdinal("TaxCode")),
                IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                Notes = reader.IsDBNull(reader.GetOrdinal("Notes")) ? null : reader.GetString(reader.GetOrdinal("Notes")),
                CreatedDate = reader.GetDateTime(reader.GetOrdinal("CreatedDate")),
                ModifiedDate = reader.IsDBNull(reader.GetOrdinal("ModifiedDate")) ? null : reader.GetDateTime(reader.GetOrdinal("ModifiedDate")),
                CreatedBy = reader.IsDBNull(reader.GetOrdinal("CreatedBy")) ? null : reader.GetString(reader.GetOrdinal("CreatedBy")),
                ModifiedBy = reader.IsDBNull(reader.GetOrdinal("ModifiedBy")) ? null : reader.GetString(reader.GetOrdinal("ModifiedBy"))
            };
        }
    }

}