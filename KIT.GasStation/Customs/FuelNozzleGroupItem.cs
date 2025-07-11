using System.Collections.Specialized;
using System.Windows.Controls;
using System.Windows.Media;

namespace KIT.GasStation.Customs
{
    public class FuelNozzleGroupItem : GroupItem
    {
        public FuelNozzleGroupItem()
        {
            Loaded += (s, e) => SubscribeToItemsChanges();
        }

        private void SubscribeToItemsChanges()
        {
            // Используем VisualTreeHelper для доступа к Items, если это возможно
            // Это пример и может потребовать доработки в зависимости от вашей структуры
            var itemsPresenter = VisualTreeHelper.GetChild(this, 0) as ItemsPresenter;
            var itemsControl = ItemsControl.GetItemsOwner(itemsPresenter) as ItemsControl;
            if (itemsControl?.Items is INotifyCollectionChanged notifyCollection)
            {
                notifyCollection.CollectionChanged += OnCollectionChanged;
            }
        }

        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // Обработка изменений в коллекции
        }


    }
}
