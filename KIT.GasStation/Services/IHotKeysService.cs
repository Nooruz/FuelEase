using System;
using System.Windows.Input;

namespace KIT.GasStation.Services
{
    public enum HotKeyAction
    {
        FuelSale,
        FuelSaleCashless,
        FuelSaleCash,
        FuelSaleTicket,
        StartFullFueling
    }


    public interface IHotKeysService
    {
        event Action<HotKeyAction> OnHotKeyPressed;

        void HandleKeyPress(Key key);
    }
}
