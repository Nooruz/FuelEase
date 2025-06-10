using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace FuelEase.Helpers
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
