using System.Threading.Tasks;

namespace eProtokoll.Services
{
    public interface IProtocolNumberService
    {
        Task<string> GenerateNextIncomingProtocolNumberAsync();
        Task<string> PeekNextIncomingProtocolNumberAsync();
        string FormatProtocolNumber(string prefix, int number, int year, string format, int padding, bool showYear);
    }
}