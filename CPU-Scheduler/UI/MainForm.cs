using System;
using System.Drawing;
using System.Windows.Forms;

namespace CPU_Scheduler.UI
{
    public class MainForm : Form
    {
        private Panel drawPanel;

        public MainForm()
        {
            this.Text = "CPU Scheduler";
            this.WindowState = FormWindowState.Maximized;

            this.Load += MainForm_Load;
            this.Resize += MainForm_Resize;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            drawPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White
            };

            this.Controls.Add(drawPanel);
            drawPanel.Paint += DrawPanel_Paint;
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            drawPanel.Invalidate(); // redraw when resized
        }

        private void DrawPanel_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            int width = drawPanel.Width;
            int height = drawPanel.Height;

            // Just an example: drawing 5 process blocks in center
            int boxWidth = (int)(width * 0.1);
            int boxHeight = (int)(height * 0.2);
            int spacing = (int)(width * 0.02);
            int startX = (width - (5 * boxWidth + 4 * spacing)) / 2;
            int y = height / 2 - boxHeight / 2;

            for (int i = 0; i < 5; i++)
            {
                Rectangle box = new Rectangle(startX + i * (boxWidth + spacing), y, boxWidth, boxHeight);
                g.FillRectangle(Brushes.LightBlue, box);
                g.DrawRectangle(Pens.Black, box);
                g.DrawString($"P{i + 1}", this.Font, Brushes.Black, box.X + 10, box.Y + 10);
            }
        }
    }
}
