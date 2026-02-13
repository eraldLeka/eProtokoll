using eProtokoll.Models; // Marrim enum-in DocumentType nga modeli
using eProtokoll.Services.Mappers;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

namespace eProtokoll.Services.ProtocolNumber
{
    public class ProtocolNumberService : IProtocolNumberService
    {
        private readonly string _connectionString;

        public ProtocolNumberService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // Marr numrin e ardhshëm pa e rritur (Peek)
        public async Task<string> PeekNextProtocolNumberAsync(DocumentType type)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var currentYear = DateTime.Now.Year;
            ProtocolSettings settings = await GetProtocolSettingsAsync(connection);

            int nextNumber = GetCurrentNumber(settings, type) + 1;

            if (settings.AutoResetYearly && settings.Year != currentYear)
                nextNumber = GetStartNumber(settings, type);

            return FormatProtocolNumber(
                GetPrefix(settings, type),
                nextNumber,
                currentYear,
                settings.ProtocolNumberFormat,
                settings.NumberPadding,
                settings.ShowYearInNumber
            );
        }

        // Gjeneron numrin dhe e rrit current number
        public async Task<string> GenerateNextProtocolNumberAsync(DocumentType type)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();
            try
            {
                var currentYear = DateTime.Now.Year;
                ProtocolSettings settings = await GetProtocolSettingsAsync(connection, transaction);

                // Reset vjetor
                if (settings.AutoResetYearly && settings.Year != currentYear)
                {
                    var resetQuery = @"UPDATE ProtocolSettings SET
                        Year = @Year,
                        IncomingCurrentNumber = @ResetNumber,
                        OutgoingCurrentNumber = @ResetNumber,
                        InternalCurrentNumber = @ResetNumber
                        WHERE ProtocolSettingsId = 1";

                    using var resetCmd = new SqlCommand(resetQuery, connection, transaction);
                    resetCmd.Parameters.AddWithValue("@Year", currentYear);
                    resetCmd.Parameters.AddWithValue("@ResetNumber", settings.IncomingStartNumber - 1);
                    await resetCmd.ExecuteNonQueryAsync();

                    settings.Year = currentYear;
                    settings.IncomingCurrentNumber = settings.IncomingStartNumber - 1;
                    settings.OutgoingCurrentNumber = settings.OutgoingStartNumber - 1;
                    settings.InternalCurrentNumber = settings.InternalStartNumber - 1;
                }

                // Rrit current number bazuar në lloj dokumenti
                string updateColumn = type switch
                {
                    DocumentType.Incoming => "IncomingCurrentNumber",
                    DocumentType.Outgoing => "OutgoingCurrentNumber",
                    DocumentType.Internal => "InternalCurrentNumber",
                    _ => throw new ArgumentException("Invalid document type")
                };

                var updateQuery = $"UPDATE ProtocolSettings SET {updateColumn} = {updateColumn} + 1 WHERE ProtocolSettingsId = 1";
                using var updateCmd = new SqlCommand(updateQuery, connection, transaction);
                await updateCmd.ExecuteNonQueryAsync();

                // Përditëso objekti lokal
                IncrementCurrentNumber(settings, type);

                var number = GetCurrentNumber(settings, type);
                var protocolNumber = FormatProtocolNumber(
                    GetPrefix(settings, type),
                    number,
                    currentYear,
                    settings.ProtocolNumberFormat,
                    settings.NumberPadding,
                    settings.ShowYearInNumber
                );

                transaction.Commit();
                return protocolNumber;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        // Merr ProtocolSettings nga DB
        private async Task<ProtocolSettings> GetProtocolSettingsAsync(SqlConnection connection, SqlTransaction transaction = null)
        {
            var query = "SELECT * FROM ProtocolSettings WHERE ProtocolSettingsId = 1";
            using var command = new SqlCommand(query, connection, transaction);
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
                return ProtocolSettingsMapper.MapToProtocolSettings(reader);

            reader.Close();
            await InsertDefaultSettingsAsync(connection, transaction);
            return await GetProtocolSettingsAsync(connection, transaction);
        }

        // Insert defaults në DB
        private async Task InsertDefaultSettingsAsync(SqlConnection connection, SqlTransaction transaction)
        {
            var currentYear = DateTime.Now.Year;
            var insertQuery = @"INSERT INTO ProtocolSettings (
                Year, IncomingPrefix, IncomingStartNumber, IncomingCurrentNumber,
                OutgoingPrefix, OutgoingStartNumber, OutgoingCurrentNumber,
                InternalPrefix, InternalStartNumber, InternalCurrentNumber,
                ProtocolNumberFormat, NumberPadding, AutoResetYearly,
                ShowYearInNumber, UseSeparatorSlash, IsActive
            ) VALUES (
                @Year, @IncomingPrefix, @IncomingStartNumber, @IncomingCurrentNumber,
                @OutgoingPrefix, @OutgoingStartNumber, @OutgoingCurrentNumber,
                @InternalPrefix, @InternalStartNumber, @InternalCurrentNumber,
                @ProtocolNumberFormat, @NumberPadding, @AutoResetYearly,
                @ShowYearInNumber, @UseSeparatorSlash, @IsActive
            )";

            using var command = new SqlCommand(insertQuery, connection, transaction);
            command.Parameters.AddWithValue("@Year", currentYear);
            command.Parameters.AddWithValue("@IncomingPrefix", "H");
            command.Parameters.AddWithValue("@IncomingStartNumber", 1);
            command.Parameters.AddWithValue("@IncomingCurrentNumber", 0);
            command.Parameters.AddWithValue("@OutgoingPrefix", "D");
            command.Parameters.AddWithValue("@OutgoingStartNumber", 1);
            command.Parameters.AddWithValue("@OutgoingCurrentNumber", 0);
            command.Parameters.AddWithValue("@InternalPrefix", "B");
            command.Parameters.AddWithValue("@InternalStartNumber", 1);
            command.Parameters.AddWithValue("@InternalCurrentNumber", 0);
            command.Parameters.AddWithValue("@ProtocolNumberFormat", "{PREFIX}-{NUMBER}/{YEAR}");
            command.Parameters.AddWithValue("@NumberPadding", 4);
            command.Parameters.AddWithValue("@AutoResetYearly", true);
            command.Parameters.AddWithValue("@ShowYearInNumber", true);
            command.Parameters.AddWithValue("@UseSeparatorSlash", true);
            command.Parameters.AddWithValue("@IsActive", true);

            await command.ExecuteNonQueryAsync();
        }

        // Ndihmëse për prefix
        private string GetPrefix(ProtocolSettings s, DocumentType type) => type switch
        {
            DocumentType.Incoming => s.IncomingPrefix ?? "H",
            DocumentType.Outgoing => s.OutgoingPrefix ?? "D",
            DocumentType.Internal => s.InternalPrefix ?? "B",
            _ => throw new ArgumentException("Invalid document type")
        };

        // Ndihmëse për current number
        private int GetCurrentNumber(ProtocolSettings s, DocumentType type) => type switch
        {
            DocumentType.Incoming => s.IncomingCurrentNumber,
            DocumentType.Outgoing => s.OutgoingCurrentNumber,
            DocumentType.Internal => s.InternalCurrentNumber,
            _ => throw new ArgumentException("Invalid document type")
        };

        // Ndihmëse për start number
        private int GetStartNumber(ProtocolSettings s, DocumentType type) => type switch
        {
            DocumentType.Incoming => s.IncomingStartNumber,
            DocumentType.Outgoing => s.OutgoingStartNumber,
            DocumentType.Internal => s.InternalStartNumber,
            _ => throw new ArgumentException("Invalid document type")
        };

        // Rrit current number në objektin lokal
        private void IncrementCurrentNumber(ProtocolSettings s, DocumentType type)
        {
            switch (type)
            {
                case DocumentType.Incoming:
                    s.IncomingCurrentNumber++;
                    break;
                case DocumentType.Outgoing:
                    s.OutgoingCurrentNumber++;
                    break;
                case DocumentType.Internal:
                    s.InternalCurrentNumber++;
                    break;
            }
        }

        // Formato numrin final
        public string FormatProtocolNumber(string prefix, int number, int year, string format, int padding, bool showYear)
        {
            var result = format ?? string.Empty;

            result = result.Replace("{PREFIX}", prefix ?? "");
            result = result.Replace("{NUMBER}", number.ToString("D" + padding));

            result = showYear ? result.Replace("{YEAR}", year.ToString()) : result.Replace("{YEAR}", "");

            while (result.Contains("//")) result = result.Replace("//", "/");
            while (result.Contains("--")) result = result.Replace("--", "-");

            return result.Trim('/', '-');
        }
    }
}
