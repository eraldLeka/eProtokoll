using eProtokoll.Models;
using eProtokoll.Services.Mappers;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

namespace eProtokoll.Services
{
    public class ProtocolNumberService : IProtocolNumberService
    {
        private readonly string _connectionString;

        public ProtocolNumberService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // Return the next protocol number without incrementing the counter
        public async Task<string> PeekNextIncomingProtocolNumberAsync()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var currentYear = DateTime.Now.Year;
                ProtocolSettings settings = null;

                var query = "SELECT * FROM ProtocolSettings WHERE ProtocolSettingsId = 1";
                using (var command = new SqlCommand(query, connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            settings = ProtocolSettingsMapper.MapToProtocolSettings(reader);
                        }
                    }
                }

                if (settings == null)
                {
                    // default settings
                    settings = new ProtocolSettings
                    {
                        Year = currentYear,
                        IncomingPrefix = "H",
                        IncomingCurrentNumber = 0,
                        IncomingStartNumber = 1,
                        ProtocolNumberFormat = "{PREFIX}-{NUMBER}/{YEAR}",
                        NumberPadding = 4,
                        ShowYearInNumber = true
                    };
                }

                int nextNumber = settings.IncomingCurrentNumber + 1;
                if (settings.AutoResetYearly && settings.Year != currentYear)
                {
                    nextNumber = settings.IncomingStartNumber;
                }

                var number = nextNumber.ToString(new string('0', settings.NumberPadding));
                var protocolNumber = settings.ProtocolNumberFormat
                    .Replace("{PREFIX}", settings.IncomingPrefix ?? "H")
                    .Replace("{NUMBER}", number)
                    .Replace("{YEAR}", settings.ShowYearInNumber ? DateTime.Now.Year.ToString() : "")
                    .Replace("{SUFFIX}", settings.IncomingSuffix ?? "");

                protocolNumber = protocolNumber.Replace("//", "/").Replace("--", "-").Trim('-', '/');
                return protocolNumber;
            }
        }

        public async Task<string> GenerateNextIncomingProtocolNumberAsync()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        var currentYear = DateTime.Now.Year;
                        ProtocolSettings settings = null;

                        var query = "SELECT * FROM ProtocolSettings WHERE ProtocolSettingsId = 1";
                        using (var command = new SqlCommand(query, connection, transaction))
                        {
                            using (var reader = await command.ExecuteReaderAsync())
                            {
                                if (await reader.ReadAsync())
                                {
                                    settings = ProtocolSettingsMapper.MapToProtocolSettings(reader);
                                }
                            }
                        }

                        if (settings == null)
                        {
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

                            using (var command = new SqlCommand(insertQuery, connection, transaction))
                            {
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

                            settings = new ProtocolSettings
                            {
                                Year = currentYear,
                                IncomingPrefix = "H",
                                IncomingCurrentNumber = 0,
                                IncomingStartNumber = 1,
                                ProtocolNumberFormat = "{PREFIX}-{NUMBER}/{YEAR}",
                                NumberPadding = 4,
                                ShowYearInNumber = true
                            };
                        }

                        if (settings.AutoResetYearly && settings.Year != currentYear)
                        {
                            var resetQuery = @"UPDATE ProtocolSettings SET
                                Year = @Year,
                                IncomingCurrentNumber = @ResetNumber,
                                OutgoingCurrentNumber = @ResetNumber,
                                InternalCurrentNumber = @ResetNumber
                                WHERE ProtocolSettingsId = 1";

                            using (var command = new SqlCommand(resetQuery, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@Year", currentYear);
                                command.Parameters.AddWithValue("@ResetNumber", settings.IncomingStartNumber - 1);
                                await command.ExecuteNonQueryAsync();
                            }

                            settings.Year = currentYear;
                            settings.IncomingCurrentNumber = settings.IncomingStartNumber - 1;
                        }

                        var updateQuery = @"UPDATE ProtocolSettings SET 
                            IncomingCurrentNumber = IncomingCurrentNumber + 1 
                            WHERE ProtocolSettingsId = 1";

                        using (var command = new SqlCommand(updateQuery, connection, transaction))
                        {
                            await command.ExecuteNonQueryAsync();
                        }

                        settings.IncomingCurrentNumber++;

                        var number = settings.IncomingCurrentNumber.ToString(new string('0', settings.NumberPadding));
                        var protocolNumber = settings.ProtocolNumberFormat
                            .Replace("{PREFIX}", settings.IncomingPrefix ?? "H")
                            .Replace("{NUMBER}", number)
                            .Replace("{YEAR}", settings.ShowYearInNumber ? DateTime.Now.Year.ToString() : "")
                            .Replace("{SUFFIX}", settings.IncomingSuffix ?? "");

                        protocolNumber = protocolNumber.Replace("//", "/").Replace("--", "-").Trim('-', '/');

                        transaction.Commit();
                        return protocolNumber;
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public string FormatProtocolNumber(string prefix, int number, int year, string format, int padding, bool showYear)
        {
            var result = format ?? string.Empty;

            result = result.Replace("{PREFIX}", prefix ?? "");
            result = result.Replace("{NUMBER}", number.ToString("D" + padding));

            if (showYear)
            {
                result = result.Replace("{YEAR}", year.ToString());
            }
            else
            {
                result = result.Replace("{YEAR}", "");
            }

            while (result.Contains("//")) result = result.Replace("//", "/");
            while (result.Contains("--")) result = result.Replace("--", "-");

            return result.Trim('/', '-');
        }
    }
}