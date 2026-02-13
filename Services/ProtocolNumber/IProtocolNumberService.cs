using System.Threading.Tasks;

namespace eProtokoll.Services.ProtocolNumber
{
    public enum DocumentType
    {
        Incoming,
        Outgoing,
        Internal
    }

    public interface IProtocolNumberService
    {
        Task<string> GenerateNextProtocolNumberAsync(DocumentType type);
        Task<string> PeekNextProtocolNumberAsync(DocumentType type);
        string FormatProtocolNumber(string prefix, int number, int year, string format, int padding, bool showYear);
    }
}
