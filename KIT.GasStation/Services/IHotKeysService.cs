using System;
using System.Windows.Input;

namespace KIT.GasStation.Services
{
    public interface IHotKeysService
    {
        event Action<Key> OnHotKeyPressed;

        void HandleKeyPress(Key key);
    }
}
