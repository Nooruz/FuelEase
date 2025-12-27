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
        event Action<int> OnNumberKeyPressed;

        void HandleKeyPress(Key key);

        void HandleNumberKeyPress(int number);
    }
}
