using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace RBX_Alt_Manager.Modules
{
    /// <summary>
    /// Provides glow effect rendering for account status indicators
    /// </summary>
    public static class StatusGlowRenderer
    {
        // Status colors with soft, diffused appearance
        public static readonly Color OnlineColor = Color.FromArgb(255, 76, 175, 80);      // Soft green
        public static readonly Color OfflineColor = Color.FromArgb(255, 158, 158, 158);   // Soft gray
        public static readonly Color ErrorColor = Color.FromArgb(255, 244, 67, 54);       // Soft red
        public static readonly Color ReconnectingColor = Color.FromArgb(255, 255, 193, 7); // Soft yellow

        /// <summary>
        /// Gets the appropriate color for a connection state
        /// </summary>
        public static Color GetStateColor(AccountConnectionState state)
        {
            return state switch
            {
                AccountConnectionState.Online => OnlineColor,
                AccountConnectionState.Offline => OfflineColor,
                AccountConnectionState.Error => ErrorColor,
                AccountConnectionState.Reconnecting => ReconnectingColor,
                _ => OfflineColor
            };
        }

        /// <summary>
        /// Renders a smooth, diffused glow effect around a point
        /// </summary>
        public static void RenderGlow(Graphics g, Point center, float radius, AccountConnectionState state, float opacity = 0.6f)
        {
            var baseColor = GetStateColor(state);
            
            // Create multiple layers for smooth gradient glow
            int layers = 8;
            for (int i = layers; i >= 0; i--)
            {
                float layerOpacity = opacity * (1f - (i / (float)layers)) * 0.5f;
                float layerRadius = radius + (i * 2);
                
                using (var brush = new SolidBrush(Color.FromArgb(
                    (int)(255 * layerOpacity),
                    baseColor.R,
                    baseColor.G,
                    baseColor.B)))
                {
                    g.FillEllipse(brush, new RectangleF(
                        center.X - layerRadius,
                        center.Y - layerRadius,
                        layerRadius * 2,
                        layerRadius * 2));
                }
            }
        }

        /// <summary>
        /// Renders a status indicator with glow effect at the specified rectangle
        /// </summary>
        public static void RenderStatusIndicator(Graphics g, Rectangle bounds, AccountConnectionState state, float scale = 1f)
        {
            float indicatorSize = 8f * scale;
            float glowRadius = indicatorSize * 2f;
            
            Point center = new Point(
                bounds.X + (int)(indicatorSize / 2) + 3,
                bounds.Y + bounds.Height / 2);

            // Render glow effect
            RenderGlow(g, center, glowRadius, state, 0.5f);

            // Render core indicator
            using (var brush = new SolidBrush(GetStateColor(state)))
            {
                g.FillEllipse(brush, new RectangleF(
                    bounds.X + 3f * scale,
                    bounds.Y + (bounds.Height - indicatorSize) / 2,
                    indicatorSize,
                    indicatorSize));
            }
        }

        /// <summary>
        /// Creates an animated transition between two states
        /// </summary>
        public static Color InterpolateColor(AccountConnectionState from, AccountConnectionState to, float progress)
        {
            var color1 = GetStateColor(from);
            var color2 = GetStateColor(to);
            
            progress = Math.Max(0f, Math.Min(1f, progress));
            
            return Color.FromArgb(
                (int)(color1.A + (color2.A - color1.A) * progress),
                (int)(color1.R + (color2.R - color1.R) * progress),
                (int)(color1.G + (color2.G - color1.G) * progress),
                (int)(color1.B + (color2.B - color1.B) * progress));
        }
    }

    /// <summary>
    /// Enhanced account renderer with status glow effects
    /// </summary>
    public class StatusAccountRenderer : BrightIdeasSoftware.BaseRenderer
    {
        private readonly AccountStateManager _stateManager;

        public StatusAccountRenderer(AccountStateManager stateManager)
        {
            _stateManager = stateManager;
        }

        public override void Render(Graphics g, Rectangle r)
        {
            base.Render(g, r);

            if (RowObject is Account account)
            {
                // Get connection state from state manager
                var state = _stateManager.GetState(account.UserID);
                
                // Render status glow indicator
                StatusGlowRenderer.RenderStatusIndicator(g, r, state, Program.Scale);

                // Render existing age indicator if applicable
                TimeSpan diff = DateTime.Now - account.LastUse;
                bool isOld = diff.TotalDays > 20;

                if (isOld)
                {
                    diff -= TimeSpan.FromDays(20);
                    float alpha = (float)Utilities.MapValue(diff.TotalSeconds, 0, 864000, 0, 1).Clamp(0, 1);
                    
                    using (var brush = new SolidBrush(Color.FromArgb(255, 255, 204, 77).Lerp(
                        Color.FromArgb(255, 250, 26, 13), alpha)))
                    {
                        g.FillEllipse(brush, new Rectangle(
                            (int)(r.X + 3f * Program.Scale),
                            (int)(r.Y + 2 * Program.Scale),
                            (int)(4f * Program.Scale),
                            (int)(4f * Program.Scale)));
                    }
                }

                // Render presence indicator if enabled and not offline
                if (AccountManager.General.Get<bool>("ShowPresence") && 
                    account.Presence != null && 
                    account.Presence.userPresenceType != UserPresenceType.Offline)
                {
                    float presenceOffset = isOld ? 6f * Program.Scale : 0f;
                    
                    using (var brush = new SolidBrush(Presence.Colors[account.Presence.userPresenceType]))
                    {
                        g.FillEllipse(brush, new Rectangle(
                            (int)(r.X + 3f * Program.Scale + presenceOffset),
                            (int)(r.Y + 2 * Program.Scale),
                            (int)(4f * Program.Scale),
                            (int)(4f * Program.Scale)));
                    }
                }
            }
        }
    }
}
