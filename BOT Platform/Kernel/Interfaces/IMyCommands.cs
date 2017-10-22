using BOT_Platform.Kernel.Bots;
using VkNet.Model;

namespace BOT_Platform.Kernel.Interfaces
{
    interface IMyCommands
    {
        void AddMyCommandInPlatform();
        bool NeedCommandInfo(Message message, string args, Bot bot);
    }
}
