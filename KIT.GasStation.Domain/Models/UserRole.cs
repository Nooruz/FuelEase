namespace KIT.GasStation.Domain.Models
{
    public class UserRole : DomainObject
    {
        #region Private Members

        private string _name;

        #endregion

        #region Public Properties

        /// <summary>
        /// Наименование
        /// </summary>
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }

        /// <summary>
        /// Пользователи
        /// </summary>
        public ICollection<User> Users { get; set; }

        #endregion

        public override void Update(DomainObject updatedItem)
        {

        }
    }
}
