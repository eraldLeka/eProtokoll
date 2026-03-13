using eProtokoll.Models;
using eProtokoll.Services.Mappers;
using Microsoft.Data.SqlClient;

namespace eProtokoll.Repositories.Document
{
    public class DocumentRepository : IDocumentRepository
    {
        private readonly string _connectionString;

        public DocumentRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        // ==================== INCOMING ====================

        public async Task<(List<IncomingDocument> Documents, int TotalCount)> GetIncomingAsync(
            int page, int pageSize, int? createdBy = null, int? accessUserId = null)
        {
            var documents = new List<IncomingDocument>();
            int totalCount = 0;

            string whereClause;

            if (accessUserId.HasValue)
            {
                whereClause = @"WHERE d.Discriminator = 'IncomingDocument'
                    AND (
                        d.Classification = 1
                        OR d.CreatedBy = @AccessUserId
                        OR (d.Classification = 2 AND EXISTS (
                            SELECT 1 FROM DocumentPermissions dp
                            WHERE dp.DocumentId = d.DocumentId AND dp.UserId = @AccessUserId
                        ))
                    )";
            }
            else if (createdBy.HasValue)
            {
                whereClause = "WHERE d.Discriminator = 'IncomingDocument' AND d.CreatedBy = @CreatedBy";
            }
            else
            {
                whereClause = "WHERE d.Discriminator = 'IncomingDocument'";
            }

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using (var cmd = new SqlCommand(
                $"SELECT COUNT(*) FROM Documents d {whereClause}", connection))
            {
                if (accessUserId.HasValue)
                    cmd.Parameters.AddWithValue("@AccessUserId", accessUserId.Value);
                else if (createdBy.HasValue)
                    cmd.Parameters.AddWithValue("@CreatedBy", createdBy.Value);
                totalCount = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            var query = $@"
                SELECT d.*,
                    i.Name as InstitutionName, i.ShortName as InstitutionShortName,
                    u.UserName as CreatorUserName,
                    u.FirstName as CreatorFirstName,
                    u.LastName as CreatorLastName
                FROM Documents d
                LEFT JOIN Institutions i ON d.InstitutionId = i.InstitutionId
                LEFT JOIN Users u ON d.CreatedBy = u.Id
                {whereClause}
                ORDER BY d.CreatedDate DESC
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            using (var cmd = new SqlCommand(query, connection))
            {
                if (accessUserId.HasValue)
                    cmd.Parameters.AddWithValue("@AccessUserId", accessUserId.Value);
                else if (createdBy.HasValue)
                    cmd.Parameters.AddWithValue("@CreatedBy", createdBy.Value);
                cmd.Parameters.AddWithValue("@Offset", (page - 1) * pageSize);
                cmd.Parameters.AddWithValue("@PageSize", pageSize);

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var document = DocumentMapper.MapToIncomingDocument(reader);

                    if (!reader.IsDBNull(reader.GetOrdinal("InstitutionName")))
                    {
                        document.Institution = new Institution
                        {
                            InstitutionId = document.InstitutionId,
                            Name = reader.GetString(reader.GetOrdinal("InstitutionName")),
                            ShortName = reader.IsDBNull(reader.GetOrdinal("InstitutionShortName"))
                                ? null : reader.GetString(reader.GetOrdinal("InstitutionShortName"))
                        };
                    }

                    if (!reader.IsDBNull(reader.GetOrdinal("CreatorUserName")))
                    {
                        document.Creator = new Users
                        {
                            UserName = reader.GetString(reader.GetOrdinal("CreatorUserName")),
                            FirstName = reader.GetString(reader.GetOrdinal("CreatorFirstName")),
                            LastName = reader.GetString(reader.GetOrdinal("CreatorLastName"))
                        };
                    }

                    documents.Add(document);
                }
            }

            return (documents, totalCount);
        }

        public async Task<IncomingDocument?> GetIncomingByIdAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
                SELECT d.*,
                    i.Name as InstitutionName, i.ShortName as InstitutionShortName,
                    i.Adress as InstitutionAdress,
                    u.UserName as CreatorUserName,
                    u.FirstName as CreatorFirstName,
                    u.LastName as CreatorLastName
                FROM Documents d
                LEFT JOIN Institutions i ON d.InstitutionId = i.InstitutionId
                LEFT JOIN Users u ON d.CreatedBy = u.Id
                WHERE d.DocumentId = @DocumentId AND d.Discriminator = 'IncomingDocument'";

            using var cmd = new SqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@DocumentId", id);

            using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null;

            var document = DocumentMapper.MapToIncomingDocument(reader);

            if (!reader.IsDBNull(reader.GetOrdinal("InstitutionName")))
            {
                document.Institution = new Institution
                {
                    InstitutionId = document.InstitutionId,
                    Name = reader.GetString(reader.GetOrdinal("InstitutionName")),
                    ShortName = reader.IsDBNull(reader.GetOrdinal("InstitutionShortName"))
                        ? null : reader.GetString(reader.GetOrdinal("InstitutionShortName")),
                    Adress = reader.IsDBNull(reader.GetOrdinal("InstitutionAdress"))
                        ? null : reader.GetString(reader.GetOrdinal("InstitutionAdress"))
                };
            }

            if (!reader.IsDBNull(reader.GetOrdinal("CreatorUserName")))
            {
                document.Creator = new Users
                {
                    UserName = reader.GetString(reader.GetOrdinal("CreatorUserName")),
                    FirstName = reader.GetString(reader.GetOrdinal("CreatorFirstName")),
                    LastName = reader.GetString(reader.GetOrdinal("CreatorLastName"))
                };
            }

            return document;
        }

        public async Task<int> InsertIncomingAsync(IncomingDocument model)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                var query = @"
                    INSERT INTO Documents (
                        ProtocolNumber, ProtocolDate, ProtocolTime, DocumentType, Subject, Content,
                        Classification, Status, Priority, Notes, HasAttachments, CreatedDate, CreatedBy,
                        InstitutionId, SenderName, ReceivedDate, OriginalDocumentNumber, OriginalDocumentDate,
                        Discriminator
                    ) OUTPUT INSERTED.DocumentId VALUES (
                        @ProtocolNumber, @ProtocolDate, @ProtocolTime, @DocumentType, @Subject, @Content,
                        @Classification, @Status, @Priority, @Notes, @HasAttachments, @CreatedDate, @CreatedBy,
                        @InstitutionId, @SenderName, @ReceivedDate, @OriginalDocumentNumber, @OriginalDocumentDate,
                        'IncomingDocument'
                    )";

                using var cmd = new SqlCommand(query, connection, transaction);
                cmd.Parameters.AddWithValue("@ProtocolNumber", model.ProtocolNumber);
                cmd.Parameters.AddWithValue("@ProtocolDate", model.ProtocolDate);
                cmd.Parameters.AddWithValue("@ProtocolTime", model.ProtocolTime);
                cmd.Parameters.AddWithValue("@DocumentType", (int)DocumentType.Incoming);
                cmd.Parameters.AddWithValue("@Subject", model.Subject);
                cmd.Parameters.AddWithValue("@Content", (object?)model.Content ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Classification", (int)model.Classification);
                cmd.Parameters.AddWithValue("@Status", (int)model.Status);
                cmd.Parameters.AddWithValue("@Priority", (int)model.Priority);
                cmd.Parameters.AddWithValue("@Notes", (object?)model.Notes ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@HasAttachments", false);
                cmd.Parameters.AddWithValue("@CreatedDate", model.CreatedDate);
                cmd.Parameters.AddWithValue("@CreatedBy", model.CreatedBy);
                cmd.Parameters.AddWithValue("@InstitutionId", model.InstitutionId);
                cmd.Parameters.AddWithValue("@SenderName", model.SenderName);
                cmd.Parameters.AddWithValue("@ReceivedDate", model.ReceivedDate);
                cmd.Parameters.AddWithValue("@OriginalDocumentNumber",
                    (object?)model.OriginalDocumentNumber ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@OriginalDocumentDate",
                    (object?)model.OriginalDocumentDate ?? DBNull.Value);

                var documentId = (int)await cmd.ExecuteScalarAsync();
                transaction.Commit();
                return documentId;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        // ==================== OUTGOING ====================

        public async Task<(List<OutgoingDocument> Documents, int TotalCount)> GetOutgoingAsync(
            int page, int pageSize, int? createdBy = null, int? accessUserId = null)
        {
            var documents = new List<OutgoingDocument>();
            int totalCount = 0;

            string whereClause;

            if (accessUserId.HasValue)
            {
                whereClause = @"WHERE d.Discriminator = 'OutgoingDocument'
                    AND (
                        d.Classification = 1
                        OR d.CreatedBy = @AccessUserId
                        OR (d.Classification = 2 AND EXISTS (
                            SELECT 1 FROM DocumentPermissions dp
                            WHERE dp.DocumentId = d.DocumentId AND dp.UserId = @AccessUserId
                        ))
                    )";
            }
            else if (createdBy.HasValue)
            {
                whereClause = "WHERE d.Discriminator = 'OutgoingDocument' AND d.CreatedBy = @CreatedBy";
            }
            else
            {
                whereClause = "WHERE d.Discriminator = 'OutgoingDocument'";
            }

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using (var cmd = new SqlCommand(
                $"SELECT COUNT(*) FROM Documents d {whereClause}", connection))
            {
                if (accessUserId.HasValue)
                    cmd.Parameters.AddWithValue("@AccessUserId", accessUserId.Value);
                else if (createdBy.HasValue)
                    cmd.Parameters.AddWithValue("@CreatedBy", createdBy.Value);
                totalCount = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            var query = $@"
                SELECT d.*,
                    i.Name as InstitutionName, i.ShortName as InstitutionShortName,
                    u.UserName as CreatorUserName,
                    u.FirstName as CreatorFirstName,
                    u.LastName as CreatorLastName
                FROM Documents d
                LEFT JOIN Institutions i ON d.InstitutionId = i.InstitutionId
                LEFT JOIN Users u ON d.CreatedBy = u.Id
                {whereClause}
                ORDER BY d.CreatedDate DESC
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            using (var cmd = new SqlCommand(query, connection))
            {
                if (accessUserId.HasValue)
                    cmd.Parameters.AddWithValue("@AccessUserId", accessUserId.Value);
                else if (createdBy.HasValue)
                    cmd.Parameters.AddWithValue("@CreatedBy", createdBy.Value);
                cmd.Parameters.AddWithValue("@Offset", (page - 1) * pageSize);
                cmd.Parameters.AddWithValue("@PageSize", pageSize);

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var document = DocumentMapper.MapToOutgoingDocument(reader);

                    if (!reader.IsDBNull(reader.GetOrdinal("InstitutionName")))
                    {
                        document.Institution = new Institution
                        {
                            InstitutionId = document.InstitutionId,
                            Name = reader.GetString(reader.GetOrdinal("InstitutionName")),
                            ShortName = reader.IsDBNull(reader.GetOrdinal("InstitutionShortName"))
                                ? null : reader.GetString(reader.GetOrdinal("InstitutionShortName"))
                        };
                    }

                    if (!reader.IsDBNull(reader.GetOrdinal("CreatorUserName")))
                    {
                        document.Creator = new Users
                        {
                            UserName = reader.GetString(reader.GetOrdinal("CreatorUserName")),
                            FirstName = reader.GetString(reader.GetOrdinal("CreatorFirstName")),
                            LastName = reader.GetString(reader.GetOrdinal("CreatorLastName"))
                        };
                    }

                    documents.Add(document);
                }
            }

            return (documents, totalCount);
        }

        public async Task<OutgoingDocument?> GetOutgoingByIdAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
                SELECT d.*,
                    i.Name as InstitutionName, i.ShortName as InstitutionShortName,
                    i.Adress as InstitutionAdress,
                    u.UserName as CreatorUserName,
                    u.FirstName as CreatorFirstName,
                    u.LastName as CreatorLastName
                FROM Documents d
                LEFT JOIN Institutions i ON d.InstitutionId = i.InstitutionId
                LEFT JOIN Users u ON d.CreatedBy = u.Id
                WHERE d.DocumentId = @DocumentId AND d.Discriminator = 'OutgoingDocument'";

            using var cmd = new SqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@DocumentId", id);

            using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null;

            var document = DocumentMapper.MapToOutgoingDocument(reader);

            if (!reader.IsDBNull(reader.GetOrdinal("InstitutionName")))
            {
                document.Institution = new Institution
                {
                    InstitutionId = document.InstitutionId,
                    Name = reader.GetString(reader.GetOrdinal("InstitutionName")),
                    ShortName = reader.IsDBNull(reader.GetOrdinal("InstitutionShortName"))
                        ? null : reader.GetString(reader.GetOrdinal("InstitutionShortName")),
                    Adress = reader.IsDBNull(reader.GetOrdinal("InstitutionAdress"))
                        ? null : reader.GetString(reader.GetOrdinal("InstitutionAdress"))
                };
            }

            if (!reader.IsDBNull(reader.GetOrdinal("CreatorUserName")))
            {
                document.Creator = new Users
                {
                    UserName = reader.GetString(reader.GetOrdinal("CreatorUserName")),
                    FirstName = reader.GetString(reader.GetOrdinal("CreatorFirstName")),
                    LastName = reader.GetString(reader.GetOrdinal("CreatorLastName"))
                };
            }

            return document;
        }

        public async Task<int> InsertOutgoingAsync(OutgoingDocument model)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                var query = @"
                    INSERT INTO Documents (
                        ProtocolNumber, ProtocolDate, ProtocolTime, DocumentType, Subject, Content,
                        Classification, Status, Priority, Notes, HasAttachments,
                        CreatedDate, CreatedBy, InstitutionId, RecipientName, IsResponse,
                        OriginalIncomingDocumentId, ArchiveLocation, Discriminator
                    ) OUTPUT INSERTED.DocumentId VALUES (
                        @ProtocolNumber, @ProtocolDate, @ProtocolTime, @DocumentType, @Subject, @Content,
                        @Classification, @Status, @Priority, @Notes, @HasAttachments,
                        @CreatedDate, @CreatedBy, @InstitutionId, @RecipientName, @IsResponse,
                        @OriginalIncomingDocumentId, @ArchiveLocation, 'OutgoingDocument'
                    )";

                using var cmd = new SqlCommand(query, connection, transaction);
                cmd.Parameters.AddWithValue("@ProtocolNumber", model.ProtocolNumber);
                cmd.Parameters.AddWithValue("@ProtocolDate", model.ProtocolDate);
                cmd.Parameters.AddWithValue("@ProtocolTime", model.ProtocolTime);
                cmd.Parameters.AddWithValue("@DocumentType", (int)DocumentType.Outgoing);
                cmd.Parameters.AddWithValue("@Subject", model.Subject);
                cmd.Parameters.AddWithValue("@Content", (object?)model.Content ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Classification", (int)model.Classification);
                cmd.Parameters.AddWithValue("@Status", (int)model.Status);
                cmd.Parameters.AddWithValue("@Priority", (int)model.Priority);
                cmd.Parameters.AddWithValue("@Notes", (object?)model.Notes ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@HasAttachments", false);
                cmd.Parameters.AddWithValue("@CreatedDate", model.CreatedDate);
                cmd.Parameters.AddWithValue("@CreatedBy", model.CreatedBy);
                cmd.Parameters.AddWithValue("@InstitutionId", model.InstitutionId);
                cmd.Parameters.AddWithValue("@RecipientName", model.RecipientName);
                cmd.Parameters.AddWithValue("@IsResponse", model.IsResponse);
                cmd.Parameters.AddWithValue("@OriginalIncomingDocumentId",
                    (object?)model.OriginalIncomingDocumentId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ArchiveLocation",
                    (object?)model.ArchiveLocation ?? DBNull.Value);

                var documentId = (int)await cmd.ExecuteScalarAsync();
                transaction.Commit();
                return documentId;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        // ==================== INTERNAL ====================

        public async Task<(List<InternalDocument> Documents, int TotalCount)> GetInternalAsync(
            int page, int pageSize, int? createdBy = null, int? accessUserId = null)
        {
            var documents = new List<InternalDocument>();
            int totalCount = 0;

            string whereClause;

            if (accessUserId.HasValue)
            {
                whereClause = @"WHERE d.Discriminator = 'InternalDocument'
                    AND (
                        d.Classification = 1
                        OR d.CreatedBy = @AccessUserId
                        OR (d.Classification = 2 AND EXISTS (
                            SELECT 1 FROM DocumentPermissions dp
                            WHERE dp.DocumentId = d.DocumentId AND dp.UserId = @AccessUserId
                        ))
                    )";
            }
            else if (createdBy.HasValue)
            {
                whereClause = "WHERE d.Discriminator = 'InternalDocument' AND d.CreatedBy = @CreatedBy";
            }
            else
            {
                whereClause = "WHERE d.Discriminator = 'InternalDocument'";
            }

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using (var cmd = new SqlCommand(
                $"SELECT COUNT(*) FROM Documents d {whereClause}", connection))
            {
                if (accessUserId.HasValue)
                    cmd.Parameters.AddWithValue("@AccessUserId", accessUserId.Value);
                else if (createdBy.HasValue)
                    cmd.Parameters.AddWithValue("@CreatedBy", createdBy.Value);
                totalCount = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            var query = $@"
                SELECT d.*,
                    u.UserName as CreatorUserName,
                    u.FirstName as CreatorFirstName,
                    u.LastName as CreatorLastName
                FROM Documents d
                LEFT JOIN Users u ON d.CreatedBy = u.Id
                {whereClause}
                ORDER BY d.CreatedDate DESC
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            using (var cmd = new SqlCommand(query, connection))
            {
                if (accessUserId.HasValue)
                    cmd.Parameters.AddWithValue("@AccessUserId", accessUserId.Value);
                else if (createdBy.HasValue)
                    cmd.Parameters.AddWithValue("@CreatedBy", createdBy.Value);
                cmd.Parameters.AddWithValue("@Offset", (page - 1) * pageSize);
                cmd.Parameters.AddWithValue("@PageSize", pageSize);

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var document = DocumentMapper.MapToInternalDocument(reader);

                    if (!reader.IsDBNull(reader.GetOrdinal("CreatorUserName")))
                    {
                        document.Creator = new Users
                        {
                            UserName = reader.GetString(reader.GetOrdinal("CreatorUserName")),
                            FirstName = reader.GetString(reader.GetOrdinal("CreatorFirstName")),
                            LastName = reader.GetString(reader.GetOrdinal("CreatorLastName"))
                        };
                    }

                    documents.Add(document);
                }
            }

            return (documents, totalCount);
        }

        public async Task<InternalDocument?> GetInternalByIdAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
                SELECT d.*,
                    u.UserName as CreatorUserName,
                    u.FirstName as CreatorFirstName,
                    u.LastName as CreatorLastName
                FROM Documents d
                LEFT JOIN Users u ON d.CreatedBy = u.Id
                WHERE d.DocumentId = @DocumentId AND d.Discriminator = 'InternalDocument'";

            using var cmd = new SqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@DocumentId", id);

            using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null;

            var document = DocumentMapper.MapToInternalDocument(reader);

            if (!reader.IsDBNull(reader.GetOrdinal("CreatorUserName")))
            {
                document.Creator = new Users
                {
                    UserName = reader.GetString(reader.GetOrdinal("CreatorUserName")),
                    FirstName = reader.GetString(reader.GetOrdinal("CreatorFirstName")),
                    LastName = reader.GetString(reader.GetOrdinal("CreatorLastName"))
                };
            }

            return document;
        }

        public async Task<int> InsertInternalAsync(InternalDocument model)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                var query = @"
                    INSERT INTO Documents (
                        ProtocolNumber, ProtocolDate, ProtocolTime, DocumentType, Subject, Content,
                        Classification, Status, Priority, Notes, HasAttachments,
                        CreatedDate, CreatedBy, FromDepartment, ToDepartment, Discriminator
                    ) OUTPUT INSERTED.DocumentId VALUES (
                        @ProtocolNumber, @ProtocolDate, @ProtocolTime, @DocumentType, @Subject, @Content,
                        @Classification, @Status, @Priority, @Notes, @HasAttachments,
                        @CreatedDate, @CreatedBy, @FromDepartment, @ToDepartment, 'InternalDocument'
                    )";

                using var cmd = new SqlCommand(query, connection, transaction);
                cmd.Parameters.AddWithValue("@ProtocolNumber", model.ProtocolNumber);
                cmd.Parameters.AddWithValue("@ProtocolDate", model.ProtocolDate);
                cmd.Parameters.AddWithValue("@ProtocolTime", model.ProtocolTime);
                cmd.Parameters.AddWithValue("@DocumentType", (int)DocumentType.Internal);
                cmd.Parameters.AddWithValue("@Subject", model.Subject);
                cmd.Parameters.AddWithValue("@Content", (object?)model.Content ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Classification", (int)model.Classification);
                cmd.Parameters.AddWithValue("@Status", (int)model.Status);
                cmd.Parameters.AddWithValue("@Priority", (int)model.Priority);
                cmd.Parameters.AddWithValue("@Notes", (object?)model.Notes ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@HasAttachments", false);
                cmd.Parameters.AddWithValue("@CreatedDate", model.CreatedDate);
                cmd.Parameters.AddWithValue("@CreatedBy", model.CreatedBy);
                cmd.Parameters.AddWithValue("@FromDepartment",
                    (object?)model.FromDepartment ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ToDepartment",
                    (object?)model.ToDepartment ?? DBNull.Value);

                var documentId = (int)await cmd.ExecuteScalarAsync();
                transaction.Commit();
                return documentId;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        // ==================== SHARED ====================

        public async Task<int> GetCountAsync(DocumentType type)
        {
            var discriminator = type switch
            {
                DocumentType.Incoming => "IncomingDocument",
                DocumentType.Outgoing => "OutgoingDocument",
                DocumentType.Internal => "InternalDocument",
                _ => throw new ArgumentException($"Unknown DocumentType: {type}")
            };

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var cmd = new SqlCommand(
                "SELECT COUNT(*) FROM Documents WHERE Discriminator = @Discriminator", connection);
            cmd.Parameters.AddWithValue("@Discriminator", discriminator);
            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        public async Task<int> GetTodayCountAsync(DocumentType type)
        {
            var discriminator = type switch
            {
                DocumentType.Incoming => "IncomingDocument",
                DocumentType.Outgoing => "OutgoingDocument",
                DocumentType.Internal => "InternalDocument",
                _ => throw new ArgumentException($"Unknown DocumentType: {type}")
            };

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var cmd = new SqlCommand(@"
                SELECT COUNT(*) FROM Documents
                WHERE Discriminator = @Discriminator
                AND CAST(CreatedDate AS DATE) = @Today", connection);
            cmd.Parameters.AddWithValue("@Discriminator", discriminator);
            cmd.Parameters.AddWithValue("@Today", DateTime.Now.Date);
            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        // ==================== ATTACHMENTS ====================

        public async Task InsertAttachmentAsync(DocumentAttachment attachment)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                var query = @"
                    INSERT INTO DocumentAttachments (
                        DocumentId, FileName, OriginalFileName, FilePath, FileSize, FileExtension,
                        ContentType, UploadedDate, UploadedBy, Category, DisplayOrder,
                        IsPrimaryDocument, FileHash
                    ) VALUES (
                        @DocumentId, @FileName, @OriginalFileName, @FilePath, @FileSize, @FileExtension,
                        @ContentType, @UploadedDate, @UploadedBy, @Category, @DisplayOrder,
                        @IsPrimaryDocument, @FileHash
                    )";

                using (var cmd = new SqlCommand(query, connection, transaction))
                {
                    cmd.Parameters.AddWithValue("@DocumentId", attachment.DocumentId);
                    cmd.Parameters.AddWithValue("@FileName", attachment.FileName);
                    cmd.Parameters.AddWithValue("@OriginalFileName", attachment.OriginalFileName);
                    cmd.Parameters.AddWithValue("@FilePath", attachment.FilePath);
                    cmd.Parameters.AddWithValue("@FileSize", attachment.FileSize);
                    cmd.Parameters.AddWithValue("@FileExtension", (object?)attachment.FileExtension ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ContentType", (object?)attachment.ContentType ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@UploadedDate", attachment.UploadedDate);
                    cmd.Parameters.AddWithValue("@UploadedBy", attachment.UploadedBy);
                    cmd.Parameters.AddWithValue("@Category", (int)attachment.Category);
                    cmd.Parameters.AddWithValue("@DisplayOrder", attachment.DisplayOrder);
                    cmd.Parameters.AddWithValue("@IsPrimaryDocument", attachment.IsPrimaryDocument);
                    cmd.Parameters.AddWithValue("@FileHash", (object?)attachment.FileHash ?? DBNull.Value);
                    await cmd.ExecuteNonQueryAsync();
                }

                using (var cmd = new SqlCommand(
                    "UPDATE Documents SET HasAttachments = 1 WHERE DocumentId = @DocumentId",
                    connection, transaction))
                {
                    cmd.Parameters.AddWithValue("@DocumentId", attachment.DocumentId);
                    await cmd.ExecuteNonQueryAsync();
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<List<DocumentAttachment>> GetAttachmentsByDocumentIdAsync(int documentId)
        {
            var attachments = new List<DocumentAttachment>();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var cmd = new SqlCommand(
                "SELECT * FROM DocumentAttachments WHERE DocumentId = @DocumentId ORDER BY DisplayOrder",
                connection);
            cmd.Parameters.AddWithValue("@DocumentId", documentId);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                attachments.Add(AttachmentMapper.MapToDocumentAttachment(reader));

            return attachments;
        }

        // ==================== DROPDOWN ====================

        public async Task<List<Institution>> GetInstitutionsAsync()
        {
            var institutions = new List<Institution>();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var cmd = new SqlCommand(
                "SELECT InstitutionId, Name, ShortName FROM Institutions WHERE IsActive = 1 ORDER BY Name",
                connection);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                institutions.Add(new Institution
                {
                    InstitutionId = reader.GetInt32(reader.GetOrdinal("InstitutionId")),
                    Name = reader.GetString(reader.GetOrdinal("Name")),
                    ShortName = reader.IsDBNull(reader.GetOrdinal("ShortName"))
                        ? null : reader.GetString(reader.GetOrdinal("ShortName"))
                });
            }

            return institutions;
        }

        // ==================== USERS ====================

        public async Task<List<Users>> GetActiveUsersAsync()
        {
            var users = new List<Users>();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var cmd = new SqlCommand(
                "SELECT Id, FirstName, LastName, UserName FROM Users WHERE IsActive = 1 AND ROLE = 3 ORDER BY FirstName, LastName",
                connection);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                users.Add(new Users
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                    LastName = reader.GetString(reader.GetOrdinal("LastName")),
                    UserName = reader.GetString(reader.GetOrdinal("UserName"))
                });
            }

            return users;
        }

        // ==================== PERMISSIONS ====================

        public async Task InsertDocumentPermissionsAsync(int documentId, List<int> userIds)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                foreach (var userId in userIds)
                {
                    using var cmd = new SqlCommand(
                        "INSERT INTO DocumentPermissions (DocumentId, UserId) VALUES (@DocumentId, @UserId)",
                        connection, transaction);
                    cmd.Parameters.AddWithValue("@DocumentId", documentId);
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    await cmd.ExecuteNonQueryAsync();
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }
}