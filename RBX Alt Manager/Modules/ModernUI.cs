using System;
using System.Drawing;
using System.Windows.Forms;

namespace RBX_Alt_Manager.Modules
{
    /// <summary>
    /// Modern themed UI panel with soft shadows and gradients
    /// </summary>
    public class ModernPanel : Panel
    {
        public Color GradientStart { get; set; } = Color.FromArgb(250, 250, 250);
        public Color GradientEnd { get; set; } = Color.FromArgb(240, 240, 240);
        public bool UseGradient { get; set; } = true;
        public int ShadowDepth { get; set; } = 3;
        public int CornerRadius { get; set; } = 8;
        public Color BorderColor { get; set; } = Color.FromArgb(220, 220, 220);
        public int BorderWidth { get; set; } = 1;

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // Draw rounded rectangle with gradient
            using (var path = CreateRoundedRectanglePath(ClientRectangle, CornerRadius))
            {
                // Fill with gradient or solid color
                if (UseGradient)
                {
                    using (var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                        ClientRectangle, GradientStart, GradientEnd, 90f))
                    {
                        e.Graphics.FillPath(brush, path);
                    }
                }
                else
                {
                    using (var brush = new SolidBrush(BackColor))
                    {
                        e.Graphics.FillPath(brush, path);
                    }
                }

                // Draw border
                if (BorderWidth > 0)
                {
                    using (var pen = new Pen(BorderColor, BorderWidth))
                    {
                        e.Graphics.DrawPath(pen, path);
                    }
                }
            }
        }

        private static System.Drawing.Drawing2D.GraphicsPath CreateRoundedRectanglePath(Rectangle bounds, int radius)
        {
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            int diameter = radius * 2;

            path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();

            return path;
        }
    }

    /// <summary>
    /// Modern styled tab control with clean appearance
    /// </summary>
    public class ModernTabControl : TabControl
    {
        public Color TabBarColor { get; set; } = Color.FromArgb(245, 245, 245);
        public Color ActiveTabColor { get; set; } = Color.White;
        public Color InactiveTabColor { get; set; } = Color.FromArgb(240, 240, 240);
        public Color TextColor { get; set; } = Color.FromArgb(60, 60, 60);
        public Color ActiveTextColor { get; set; } = Color.FromArgb(30, 30, 30);
        public Color AccentColor { get; set; } = Color.FromArgb(0, 120, 215);
        public int TabHeight { get; set; } = 35;

        public ModernTabControl()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | 
                     ControlStyles.OptimizedDoubleBuffer | 
                     ControlStyles.UserPaint, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // Draw tab bar background
            Rectangle tabBarRect = new Rectangle(0, 0, Width, TabHeight);
            using (var brush = new SolidBrush(TabBarColor))
            {
                e.Graphics.FillRectangle(brush, tabBarRect);
            }

            // Draw tabs
            for (int i = 0; i < TabCount; i++)
            {
                Rectangle tabRect = GetTabRect(i);
                tabRect.Height = TabHeight;
                
                bool isActive = SelectedIndex == i;
                
                // Draw tab background
                Color tabColor = isActive ? ActiveTabColor : InactiveTabColor;
                using (var brush = new SolidBrush(tabColor))
                {
                    e.Graphics.FillRectangle(brush, tabRect);
                }

                // Draw accent line for active tab
                if (isActive)
                {
                    using (var brush = new SolidBrush(AccentColor))
                    {
                        e.Graphics.FillRectangle(brush, tabRect.X, tabRect.Bottom - 3, tabRect.Width, 3);
                    }
                }

                // Draw tab text
                string text = TabPages[i].Text;
                Color textColor = isActive ? ActiveTextColor : TextColor;
                
                using (var font = new Font("Segoe UI", 9f, isActive ? FontStyle.Bold : FontStyle.Regular))
                using (var brush = new SolidBrush(textColor))
                {
                    StringFormat sf = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center
                    };
                    
                    RectangleF textRect = new RectangleF(tabRect.X, tabRect.Y, tabRect.Width, tabRect.Height);
                    e.Graphics.DrawString(text, font, brush, textRect, sf);
                }
            }

            // Draw content area border
            Rectangle contentRect = new Rectangle(0, TabHeight, Width, Height - TabHeight);
            using (var pen = new Pen(Color.FromArgb(220, 220, 220), 1))
            {
                e.Graphics.DrawLine(pen, 0, TabHeight, Width, TabHeight);
            }
        }

        protected override void OnTabIndexChanged(EventArgs e)
        {
            base.OnTabIndexChanged(e);
            Invalidate();
        }
    }

    /// <summary>
    /// Modern styled button with subtle hover effects
    /// </summary>
    public class ModernButton : Button
    {
        public Color NormalColor { get; set; } = Color.FromArgb(0, 120, 215);
        public Color HoverColor { get; set; } = Color.FromArgb(0, 140, 235);
        public Color PressedColor { get; set; } = Color.FromArgb(0, 100, 195);
        public int CornerRadius { get; set; } = 6;

        private bool _isHovered;
        private bool _isPressed;

        public ModernButton()
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            Cursor = Cursors.Hand;
            Font = new Font("Segoe UI", 9f, FontStyle.Regular);
            ForeColor = Color.White;
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _isHovered = true;
            base.OnMouseEnter(e);
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _isHovered = false;
            base.OnMouseLeave(e);
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs mevent)
        {
            _isPressed = true;
            base.OnMouseDown(mevent);
            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs mevent)
        {
            _isPressed = false;
            base.OnMouseUp(mevent);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            base.OnPaint(pevent);

            pevent.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            Color bgColor = _isPressed ? PressedColor : (_isHovered ? HoverColor : NormalColor);

            using (var path = CreateRoundedRectanglePath(ClientRectangle, CornerRadius))
            using (var brush = new SolidBrush(bgColor))
            {
                pevent.Graphics.FillPath(brush, path);
            }
        }

        private static System.Drawing.Drawing2D.GraphicsPath CreateRoundedRectanglePath(Rectangle bounds, int radius)
        {
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            int diameter = radius * 2;

            path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();

            return path;
        }
    }
}
