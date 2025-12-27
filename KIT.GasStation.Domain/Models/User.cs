using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KIT.GasStation.Domain.Models
{
    /// <summary>
    /// Пользователь
    /// </summary>
    [Display(Name = "Пользователь")]
    public class User : DomainObject
    {
        #region Private Members

        private string _fullName;
        private string? _password = string.Empty;
        private DateTime _createdDate;
        private int _userRoleId;
        private bool _deleted;
        private bool _isAdmin;
        private UserType _userType;
        private bool _isDeleted;

        #endregion

        #region Public Properties

        /// <summary>
        /// ФИО
        /// </summary>
        public string FullName
        {
            get => _fullName;
            set
            {
                _fullName = value;
                OnPropertyChanged(nameof(FullName));
            }
        }

        /// <summary>
        /// Пароль
        /// </summary>
        public string? Password
        {
            get => _password;
            set
            {
                _password = value;
                OnPropertyChanged(nameof(Password));
            }
        }

        /// <summary>
        /// Дата создании
        /// </summary>
        public DateTime CreatedDate
        {
            get => _createdDate;
            set
            {
                _createdDate = value;
                OnPropertyChanged(nameof(CreatedDate));
            }
        }

        /// <summary>
        /// Роль Id
        /// </summary>
        public int UserRoleId
        {
            get => _userRoleId;
            set
            {
                _userRoleId = value;
                OnPropertyChanged(nameof(UserRoleId));
            }
        }

        /// <summary>
        /// Является ли пользователь Администратором
        /// </summary>
        public bool IsAdmin
        {
            get => _isAdmin;
            set
            {
                _isAdmin = value;
                OnPropertyChanged(nameof(IsAdmin));
            }
        }

        /// <summary>
        /// Является ли пользователь удаленным
        /// </summary>
        public bool IsDeleted
        {
            get => _isDeleted;
            set
            {
                _isDeleted = value;
                OnPropertyChanged(nameof(IsDeleted));
            }
        }

        /// <summary>
        /// Дата создания записи
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Дата обновления записи
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Дата удаления записи
        /// </summary>
        public DateTime? DeletedAt { get; set; }

        [NotMapped]
        public UserType UserType => UserRoleId switch
        {
            1 => UserType.Admin,
            2 => UserType.Cashier,
            _ => UserType.None
        };

        /// <summary>
        /// Роль
        /// </summary>
        public UserRole UserRole { get; set; }

        /// <summary>
        /// Смены
        /// </summary>
        public ICollection<Shift> Shifts { get; set; }

        #endregion

        #region Public Voids

        public override void Update(DomainObject updatedItem)
        {
            if (updatedItem is User user)
            {
                FullName = user.FullName;
                Password = user.Password;
                UserRoleId = user.UserRoleId;
                IsDeleted = user.IsDeleted;
                CreatedAt = user.CreatedAt;
                UpdatedAt = user.UpdatedAt;
                DeletedAt = user.DeletedAt;
            }
        }

        #endregion
    }

    /// <summary>
    /// Тип пользователей
    /// </summary>
    public enum UserType
    {
        None,
        Admin,
        Cashier
    }
}
