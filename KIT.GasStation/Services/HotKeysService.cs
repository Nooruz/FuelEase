using System;
using System.Collections.Generic;
using System.Windows.Input;

namespace KIT.GasStation.Services
{
    public class HotKeysService : IHotKeysService
    {
        #region Private Members

        private readonly Dictionary<Key, HotKeyAction> _hotKeys = new();

        #endregion

        #region Public Properties

        public HotKeysService()
        {
            TryAddHotKey(Properties.HotKeys.Default.FuelSaleCash, HotKeyAction.FuelSaleCash);
            TryAddHotKey(Properties.HotKeys.Default.FuelSaleCashless, HotKeyAction.FuelSaleCashless);
            TryAddHotKey(Properties.HotKeys.Default.FuelSaleTicket, HotKeyAction.FuelSaleTicket);
            TryAddHotKey(Properties.HotKeys.Default.FuelSale, HotKeyAction.FuelSale);
            TryAddHotKey(Properties.HotKeys.Default.StartFullFueling, HotKeyAction.StartFullFueling);
        }

        #endregion

        public event Action<HotKeyAction> OnHotKeyPressed;

        public void HandleKeyPress(Key key)
        {
            if (_hotKeys.TryGetValue(key, out var action))
            {
                OnHotKeyPressed?.Invoke(action);
            }
        }

        #region Helpers

        private void TryAddHotKey(string value, HotKeyAction action)
        {
            if (Enum.TryParse<Key>(value, ignoreCase: true, out var parsedKey))
            {
                _hotKeys[parsedKey] = action;
            }
        }

        #endregion
    }
}
