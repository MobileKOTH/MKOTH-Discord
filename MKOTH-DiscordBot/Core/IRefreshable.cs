using System.Threading.Tasks;

namespace MKOTHDiscordBot.Core
{
    public interface IRefreshable
    {
        Task RefreshAsync();
    }
}
