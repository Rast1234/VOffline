using System.Threading.Tasks;
using VkNet.Utils;

namespace VOffline.Services.Vk
{
    public delegate Task<VkCollection<T>> PageGetter<T>(decimal count, decimal offset);
}