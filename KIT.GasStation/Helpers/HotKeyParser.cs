using System.Globalization;
using System.Windows.Input;

namespace KIT.GasStation.Helpers
{
    public static class HotKeyParser
    {
        private static readonly KeyGestureConverter _conv = new();

        public static bool TryParse(string? text, out KeyGesture? gesture)
        {
            gesture = null;
            if (string.IsNullOrWhiteSpace(text)) return false;

            try
            {
                var obj = _conv.ConvertFromString(null, CultureInfo.InvariantCulture, text.Trim());
                gesture = obj as KeyGesture;

                // Доп. защита: не принимаем "None"
                return gesture != null && gesture.Key != Key.None;
            }
            catch
            {
                return false;
            }
        }
    }
}
