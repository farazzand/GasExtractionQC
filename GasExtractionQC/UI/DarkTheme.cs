using System.Drawing;
using System.Windows.Forms;

namespace GasExtractionQC.UI
{
    public static class DarkTheme
    {
        // Color palette
        public static readonly Color Background = Color.FromArgb(32, 32, 32);
        public static readonly Color Surface = Color.FromArgb(45, 45, 45);
        public static readonly Color SurfaceLight = Color.FromArgb(60, 60, 60);
        public static readonly Color Border = Color.FromArgb(80, 80, 80);
        public static readonly Color TextPrimary = Color.FromArgb(240, 240, 240);
        public static readonly Color TextSecondary = Color.FromArgb(180, 180, 180);
        public static readonly Color Accent = Color.FromArgb(0, 150, 255);
        
        // Status colors
        public static readonly Color StatusGreen = Color.FromArgb(76, 175, 80);
        public static readonly Color StatusYellow = Color.FromArgb(255, 193, 7);
        public static readonly Color StatusRed = Color.FromArgb(244, 67, 54);
        public static readonly Color StatusGray = Color.FromArgb(120, 120, 120);

        public static void ApplyTo(Control control)
        {
            control.BackColor = Background;
            control.ForeColor = TextPrimary;

            if (control is Form form)
            {
                form.BackColor = Background;
            }
            else if (control is Button button)
            {
                button.BackColor = Surface;
                button.ForeColor = TextPrimary;
                button.FlatStyle = FlatStyle.Flat;
                button.FlatAppearance.BorderColor = Border;
                button.FlatAppearance.MouseOverBackColor = SurfaceLight;
            }
            else if (control is TextBox textBox)
            {
                textBox.BackColor = Surface;
                textBox.ForeColor = TextPrimary;
                textBox.BorderStyle = BorderStyle.FixedSingle;
            }
            else if (control is ComboBox comboBox)
            {
                comboBox.BackColor = Surface;
                comboBox.ForeColor = TextPrimary;
                comboBox.FlatStyle = FlatStyle.Flat;
            }
            else if (control is NumericUpDown numeric)
            {
                numeric.BackColor = Surface;
                numeric.ForeColor = TextPrimary;
            }
            else if (control is GroupBox groupBox)
            {
                groupBox.ForeColor = TextSecondary;
            }
            else if (control is TabControl tabControl)
            {
                tabControl.BackColor = Background;
            }
            else if (control is TabPage tabPage)
            {
                tabPage.BackColor = Background;
            }
            else if (control is DataGridView grid)
            {
                grid.BackgroundColor = Background;
                grid.ForeColor = TextPrimary;
                grid.GridColor = Border;
                grid.DefaultCellStyle.BackColor = Surface;
                grid.DefaultCellStyle.ForeColor = TextPrimary;
                grid.DefaultCellStyle.SelectionBackColor = Accent;
                grid.DefaultCellStyle.SelectionForeColor = TextPrimary;
                grid.ColumnHeadersDefaultCellStyle.BackColor = SurfaceLight;
                grid.ColumnHeadersDefaultCellStyle.ForeColor = TextPrimary;
                grid.EnableHeadersVisualStyles = false;
                grid.BorderStyle = BorderStyle.None;
            }
            else if (control is Panel panel)
            {
                panel.BackColor = Surface;
            }

            // Apply recursively to child controls
            foreach (Control child in control.Controls)
            {
                ApplyTo(child);
            }
        }
    }
}