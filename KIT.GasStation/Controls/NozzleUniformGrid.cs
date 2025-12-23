using System.Windows.Controls.Primitives;

namespace KIT.GasStation.Controls
{
    public class NozzleUniformGrid : UniformGrid
    {
        public int MaxColumns { get; set; } = 4;

        protected override System.Windows.Size MeasureOverride(System.Windows.Size constraint)
        {
            var count = InternalChildren.Count;

            // 1..4 => 1 ряд, 5..8 => 2 ряда по 4
            Columns = count <= 0 ? 1
                   : count <= MaxColumns ? count
                   : MaxColumns;

            return base.MeasureOverride(constraint);
        }
    }
}
