using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria.GameContent;

namespace NetSimplified;

/// <summary>
///     UI 绘制辅助：仅使用原版贴图（<see cref="TextureAssets.MagicPixel" /> 纯白像素）绘制面板边框，
///     文字使用 <c>spriteBatch.DrawString</c>。
/// </summary>
internal static class UIDrawing
{
    /// <summary>绘制一个带边框的面板（边框向外扩展 <paramref name="borderWidth" /> 像素）</summary>
    public static void DrawPanel(SpriteBatch spriteBatch, Rectangle rect, Color borderColor, Color backgroundColor, int borderWidth = 1) {
        var pixel = TextureAssets.MagicPixel.Value;

        spriteBatch.Draw(pixel, new Rectangle(rect.X - borderWidth, rect.Y - borderWidth, rect.Width + borderWidth * 2, borderWidth), borderColor);
        spriteBatch.Draw(pixel, new Rectangle(rect.X - borderWidth, rect.Y + rect.Height, rect.Width + borderWidth * 2, borderWidth), borderColor);
        spriteBatch.Draw(pixel, new Rectangle(rect.X - borderWidth, rect.Y - borderWidth, borderWidth, rect.Height + borderWidth * 2), borderColor);
        spriteBatch.Draw(pixel, new Rectangle(rect.X + rect.Width, rect.Y - borderWidth, borderWidth, rect.Height + borderWidth * 2), borderColor);
        spriteBatch.Draw(pixel, rect, backgroundColor);
    }

    /// <summary>以固定缩放绘制文字</summary>
    public static void DrawText(SpriteBatch spriteBatch, string text, Vector2 position, Color color, float scale = 0.7f) {
        spriteBatch.DrawString(FontAssets.MouseText.Value, text, position, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
    }

    /// <summary>测量缩放后的文字宽度</summary>
    public static Vector2 MeasureText(string text, float scale = 0.7f) {
        return FontAssets.MouseText.Value.MeasureString(text) * scale;
    }
}
