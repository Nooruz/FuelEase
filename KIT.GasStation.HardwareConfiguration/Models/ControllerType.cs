using System.ComponentModel.DataAnnotations;

namespace KIT.GasStation.HardwareConfigurations.Models
{
    /// <summary>
    /// Тип контроллера
    /// </summary>
    public enum ControllerType
    {
        [Display(Name = "Не выбрано")]
        None,

        [Display(Name = "Lanfeng")]
        Lanfeng,

        [Display(Name = "Gilbarco")]
        Gilbarco,

        [Display(Name = "Эмулятор")]
        Emulator,

        [Display(Name = "ПК электроникс")]
        PKElectronics,

        [Display(Name = "Технопроект Газ")]
        TechnoProjekt
    }
}
