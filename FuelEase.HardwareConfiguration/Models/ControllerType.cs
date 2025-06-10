using System.ComponentModel.DataAnnotations;

namespace FuelEase.HardwareConfigurations.Models
{
    /// <summary>
    /// Тип контроллера
    /// </summary>
    public enum ControllerType
    {
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
