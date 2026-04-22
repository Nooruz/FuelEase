using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using KIT.GasStation.State.CashRegisters;
using KIT.GasStation.ViewModels.Base;
using System;
using System.Threading.Tasks;

namespace KIT.GasStation.ViewModels.Details
{
    /// <summary>
    /// Внесение или изятие наличных средств в кассе. Этот ViewModel используется для отображения диалогового окна, в котором пользователь может ввести сумму наличных, которую он хочет внести или изъять из кассы.
    /// </summary>
    public sealed class CashAmountViewModel : BaseViewModel
    {
        #region Private Members

        private decimal _amount;
        private readonly ICashRegisterStore _cashRegisterStore;

        #endregion

        #region Public Properties

        /// <summary>
        /// Сумма наличных, введенная пользователем.
        /// </summary>
        public decimal Amount
        {
            get => _amount;
            set
            {
                _amount = value;
                OnPropertyChanged(nameof(Amount));
            }
        }
        public CashOperationType OperationType { get; set; }

        #endregion

        #region Constructors

        public CashAmountViewModel(ICashRegisterStore cashRegisterStore, CashOperationType operationType)
        {
            _cashRegisterStore = cashRegisterStore;
            OperationType = operationType;
        }

        #endregion

        #region Public Voids

        [Command]
        public async Task Ok()
        {
            try
            {
                if (Amount <= 0)
                {
                    MessageBoxService.ShowMessage("Сумма должна быть больше нуля.", "Ошибка", MessageButton.OK, MessageIcon.Exclamation);
                    return;
                }

                if (OperationType == CashOperationType.Deposit)
                {
                    await _cashRegisterStore.DepositAsync(Amount);
                }
                else if (OperationType == CashOperationType.Withdrawal)
                {
                    await _cashRegisterStore.WithdrawalAsync(Amount);
                }
            }
            catch (Exception e)
            {
                MessageBoxService.ShowMessage($"ККМ вернула ошибку.\n{e.Message}", "Ошибка", MessageButton.OK, MessageIcon.Error);
            }
            finally
            {
                CurrentWindowService?.Close();
            }
        }

        #endregion
    }

    public enum CashOperationType
    {
        Deposit,
        Withdrawal
    }
}
