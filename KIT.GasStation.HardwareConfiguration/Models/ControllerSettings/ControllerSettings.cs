using System.ComponentModel;
using System.Xml.Serialization;

namespace KIT.GasStation.HardwareConfigurations.Models
{
    [Serializable]
    [XmlInclude(typeof(LanfengControllerSettings))]
    [XmlInclude(typeof(PKElectronicsControllerSettings))]
    [XmlInclude(typeof(GilbarcoControllerSettings))]
    public abstract class ControllerSettings : INotifyPropertyChanged
    {
        #region Public Properties

        [XmlAttribute]
        public string CommonSetting { get; set; }

        #endregion

        #region Public Voids

        public abstract void SetStatus(object status);

        public abstract object GetStatus();

        public virtual bool GetIsLifted()
        {
            return false;
        }

        public virtual void SetIsLifted(bool isLifted)
        {
            
        }

        public virtual void SetConfig(object config)
        {

        }

        public virtual object GetConfig()
        {
            return new object();
        }

        #endregion

        #region PropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
