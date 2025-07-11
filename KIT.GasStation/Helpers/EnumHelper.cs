using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace KIT.GasStation.Helpers
{
    public static class EnumHelper
    {
        public static List<KeyValuePair<T, string>> GetLocalizedEnumValues<T>() where T : Enum
        {
            return Enum.GetValues(typeof(T))
                       .Cast<T>()
                       .Where(e => HasDisplayAttribute(e)) // Фильтрация элементов
                       .Select(e => new KeyValuePair<T, string>(e, GetEnumDisplayName(e)))
                       .ToList();
        }

        public static string GetEnumDisplayName<T>(T value) where T : Enum
        {
            var field = value.GetType().GetField(value.ToString());
            var attribute = field.GetCustomAttribute<DisplayAttribute>();
            return attribute == null ? string.Empty : attribute.Name;
        }

        private static bool HasDisplayAttribute<T>(T value) where T : Enum
        {
            var field = value.GetType().GetField(value.ToString());
            var attribute = field.GetCustomAttribute<DisplayAttribute>();
            return attribute != null;
        }
    }
}
