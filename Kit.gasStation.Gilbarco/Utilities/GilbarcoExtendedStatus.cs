namespace KIT.GasStation.Gilbarco.Utilities
{
    /// <summary>
    /// Расширенный статус ТРК Gilbarco
    /// </summary>
    public class GilbarcoExtendedStatus
    {
        public int PumpId { get; set; }
        public bool PriceLevelNeeded { get; set; } // 0 = Needed, 1 = Not Needed
        public bool GradeSelectionNeeded { get; set; }
        public bool IsNozzleLifted { get; set; } // 0 = Off/In, 1 = On/Out
        public bool PushToStartNeeded { get; set; }
        public int SelectedGrade { get; set; } // 1-F = Grade Digit
    }
}
