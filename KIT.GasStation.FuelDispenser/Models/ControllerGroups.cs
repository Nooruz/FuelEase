namespace KIT.GasStation.FuelDispenser.Models
{
    public static class ControllerGroups
    {
        private static string Norm(string s) =>
            string.IsNullOrWhiteSpace(s) ? "" : s.Trim();

        public static string For(string controllerName, string columnName)
            => $"{Norm(controllerName)}/{Norm(columnName)}";
    }
}
