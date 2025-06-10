using DevExpress.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FuelEase.State.WindowService
{
    public class WindowService : IWindowService
    {
        public string Title { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public DXWindowState WindowState { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public bool IsWindowAlive => throw new NotImplementedException();

        public void Activate()
        {
            throw new NotImplementedException();
        }

        public void Close()
        {
            throw new NotImplementedException();
        }

        public void Hide()
        {
            throw new NotImplementedException();
        }

        public void Restore()
        {
            throw new NotImplementedException();
        }

        public void Show(string documentType, object viewModel, object parameter, object parentViewModel)
        {
            throw new NotImplementedException();
        }
    }
}
