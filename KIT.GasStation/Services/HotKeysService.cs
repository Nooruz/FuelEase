using System;
using System.Windows.Input;

namespace KIT.GasStation.Services
{
    public class HotKeysService : IHotKeysService
    {
        public event Action<Key> OnHotKeyPressed;

        public void HandleKeyPress(Key key)
        {
            OnHotKeyPressed?.Invoke(key);
        }
    }
}
