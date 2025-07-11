using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace KIT.GasStation.Helpers
{
    public static class DisplayName
    {
        public static string GetDisplayName<T>()
        {
            var type = typeof(T);
            var displayAttribute = type.GetCustomAttribute<DisplayAttribute>();
            return displayAttribute?.Name;
        }
    }
}
