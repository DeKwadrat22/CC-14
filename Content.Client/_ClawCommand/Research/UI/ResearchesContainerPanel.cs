using System.Linq;
using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._ClawCommand.Research.UI;

/// <summary>
/// Draws prereq lines between FancyResearchConsoleItem children.
/// </summary>
public sealed partial class ResearchesContainerPanel : LayoutContainer
{
    protected override void Draw(DrawingHandleScreen handle)
    {
        foreach (var child in Children)
        {
            if (child is not FancyResearchConsoleItem item)
                continue;

            if (item.Prototype.TechnologyPrerequisites.Count <= 0)
                continue;

            var list = Children.Where(x => x is FancyResearchConsoleItem second && item.Prototype.TechnologyPrerequisites.Contains(second.Prototype.ID));
            foreach (var second in list)
            {
                if (second is not FancyResearchConsoleItem secondItem)
                    continue;

                var endSide = item.Prototype.LineConnectSides.TryGetValue(secondItem.Prototype.ID, out var side)
                    ? side
                    : "Center";

                var startCoords = new Vector2(item.PixelPosition.X + item.PixelWidth / 2, item.PixelPosition.Y + item.PixelHeight / 2);
                var endCoords = new Vector2(secondItem.PixelPosition.X + secondItem.PixelWidth / 2, secondItem.PixelPosition.Y + secondItem.PixelHeight / 2);

                var lineColor = Color.White;

                // If the prerequisite has a custom color, use it.
                // Otherwise, if the target has a custom color, use that instead.
                if (secondItem.Prototype.LineColor != Color.White)
                    lineColor = secondItem.Prototype.LineColor;
                else if (item.Prototype.LineColor != Color.White)
                    lineColor = item.Prototype.LineColor;

                // Draw an orthogonal path based on the side.
                if (endSide == "Bottom" || endSide == "Top")
                {
                    // Vertical first, then horizontal
                    handle.DrawLine(startCoords, new(startCoords.X, endCoords.Y), lineColor);
                    handle.DrawLine(new(startCoords.X, endCoords.Y), endCoords, lineColor);
                }
                else
                {
                    // Horizontal first, then vertical
                    handle.DrawLine(startCoords, new(endCoords.X, startCoords.Y), lineColor);
                    handle.DrawLine(new(endCoords.X, startCoords.Y), endCoords, lineColor);
                }
            }
        }
    }
}
