using DLS.Description;
using DLS.Game;
using Seb.Helpers;
using Seb.Types;
using Seb.Vis;
using Seb.Vis.UI;
using UnityEngine;

namespace DLS.Graphics
{
	public static class PinColourMenu
	{
		// Grid: 4 cols x 4 rows. Display in hue order (not enum order).
		// Row 0: Red, RedOrange, Orange, YellowOrange
		// Row 1: Yellow, Lime, Green, Teal
		// Row 2: Cyan, SkyBlue, Blue, Indigo
		// Row 3: Violet, Magenta, Pink, White
		static readonly int[] HueOrder =
		{
			0,  8,  1,  9,   // Red → RedOrange → Orange → YellowOrange
			2, 10,  3, 11,   // Yellow → Lime → Green → Teal
			12, 13,  4, 14,  // Cyan → SkyBlue → Blue → Indigo
			5, 15,  6,  7    // Violet → Magenta → Pink → White
		};

		const int Cols = 4;
		const int Rows = 4;
		const float SwatchSize = 2.8f;
		const float Gap = 0.35f;

		public static void DrawMenu()
		{
			UI.DrawFullscreenPanel(DrawSettings.ActiveUITheme.MenuBackgroundOverlayCol);
			Draw.ID panelID = UI.ReservePanel();

			using (UI.BeginBoundsScope(true))
			{
				float totalW = Cols * SwatchSize + (Cols - 1) * Gap;
				float totalH = Rows * SwatchSize + (Rows - 1) * Gap;
				Vector2 topLeft = UI.Centre + new Vector2(-totalW / 2, totalH / 2);

				PinColour currentColour = ContextMenu.GetCurrentPinColour();
				PinColour clicked = currentColour;
				bool anyClicked = false;

				for (int i = 0; i < HueOrder.Length; i++)
				{
					int colourIndex = HueOrder[i];
					int col = i % Cols;
					int row = i / Cols;

					Vector2 pos = topLeft + new Vector2(col * (SwatchSize + Gap), -row * (SwatchSize + Gap));
					Color swatchCol = DrawSettings.GetStateColour(true, (uint)colourIndex);

					UI.DrawPanel(pos, new Vector2(SwatchSize, SwatchSize), swatchCol, Anchor.TopLeft);
					Bounds2D bounds = UI.PrevBounds;
					bool hover = UI.MouseInsideBounds(bounds);
					bool isCurrent = (int)currentColour == colourIndex;

					float outlineW = isCurrent ? 0.20f : 0.07f;
					Color outlineCol = isCurrent ? Color.white : new Color(0.25f, 0.25f, 0.25f);
					if (hover) outlineCol = Color.white;

					UI.DrawLine(bounds.TopLeft, bounds.TopRight, outlineW, outlineCol);
					UI.DrawLine(bounds.BottomLeft, bounds.TopLeft, outlineW, outlineCol);
					UI.DrawLine(bounds.TopRight, bounds.BottomRight, outlineW, outlineCol);
					UI.DrawLine(bounds.BottomLeft, bounds.BottomRight, outlineW, outlineCol);

					if (hover && InputHelper.IsMouseDownThisFrame(MouseButton.Left))
					{
						clicked = (PinColour)colourIndex;
						anyClicked = true;
					}
				}

				MenuHelper.DrawReservedMenuPanel(panelID, UI.GetCurrentBoundsScope());

				if (anyClicked)
				{
					ContextMenu.ApplyPinColour(clicked);
					UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);
				}
				else if (KeyboardShortcuts.CancelShortcutTriggered || InputHelper.IsMouseDownThisFrame(MouseButton.Right))
				{
					UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);
				}
			}
		}
	}
}
