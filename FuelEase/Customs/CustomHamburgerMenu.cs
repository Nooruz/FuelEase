using DevExpress.Xpf.WindowsUI;
using FuelEase.ViewModels.Base;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;

namespace FuelEase.Customs
{
    public class CustomHamburgerMenu : HamburgerMenu
    {
        #region Dependency Properties

        public static readonly DependencyProperty ContentsProperty =
            DependencyProperty.Register(nameof(Contents), typeof(ObservableCollection<BaseViewModel>), typeof(CustomHamburgerMenu), 
                new PropertyMetadata(null, OnContentsPropertyChanged));

        public static readonly DependencyProperty SelectedBaseViewModelProperty =
            DependencyProperty.Register(nameof(SelectedBaseViewModel), typeof(BaseViewModel), typeof(CustomHamburgerMenu), new PropertyMetadata(null));

        #endregion

        #region Public Properties

        public ObservableCollection<BaseViewModel> Contents
        {
            get => (ObservableCollection<BaseViewModel>)GetValue(ContentsProperty);
            set => SetValue(ContentsProperty, value);
        }

        public BaseViewModel SelectedBaseViewModel
        {
            get => (BaseViewModel)GetValue(SelectedBaseViewModelProperty);
            set => SetValue(SelectedBaseViewModelProperty, value);
        }

        #endregion

        #region Private Voids

        private static void OnContentsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CustomHamburgerMenu customMenu)
            {
                if (e.OldValue is ObservableCollection<BaseViewModel> oldCollection)
                {
                    // Отписываемся от события CollectionChanged у старой коллекции
                    oldCollection.CollectionChanged -= customMenu.OnContentsChanged;
                }

                if (e.NewValue is ObservableCollection<BaseViewModel> newCollection)
                {
                    // Подписываемся на событие CollectionChanged у новой коллекции
                    newCollection.CollectionChanged += customMenu.OnContentsChanged;
                }
            }
        }

        private void OnContentsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            // Логика обработки изменений в коллекции
            if (Contents.Count == 0)
            {
                // Например, устанавливаем в null, если коллекция пустая
                SetValue(ContentsProperty, new BaseViewModel());
            }
        }

        #endregion
    }
}
