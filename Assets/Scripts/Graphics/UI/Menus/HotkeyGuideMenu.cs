using DLS.Game;
using Seb.Types;
using Seb.Vis;
using Seb.Vis.UI;
using UnityEngine;

namespace DLS.Graphics
{
	public static class HotkeyGuideMenu
	{
		const float MenuWidth   = 70f;
		const float ColGap      = 2f;
		const float HalfW       = (MenuWidth - ColGap) / 2f; // 34
		const float ColKeyW     = 18f;
		const float RowH        = 2.2f;
		const float SectionPadY = 0.8f;
		const float EntryPadX   = 1.0f;

		// Section indices: 0=EDITOR, 1=PLACEMENT, 2=CHIP, 3=SIMULATION
		static readonly (string key, string desc)[][] Sections =
		{
			// 0 – EDITOR
			new[]
			{
				("Ctrl+C",         "Copy selection"),
				("Ctrl+V",         "Paste"),
				("Ctrl+Z",         "Undo"),
				("Ctrl+Y/Shift+Z", "Redo"),
				("Del/Backspace",  "Delete"),
				("Ctrl+G",         "Toggle grid"),
				("Ctrl+R",         "Reset camera"),
				("Ctrl+A",         "Add annotation"),
				("Alt/Shift+D",    "Duplicate chip"),
			},
			// 1 – PLACEMENT & CAMERA
			new[]
			{
				("Esc",   "Cancel / close"),
				("Enter", "Confirm"),
				("Shift", "Straight line"),
				("Ctrl",  "Grid snap"),
				("Alt",   "Pan camera"),
			},
			// 2 – CHIP
			new[]
			{
				("Ctrl+N", "New chip"),
				("Ctrl+S", "Save"),
				("Ctrl+F", "Search"),
				("Ctrl+L", "Library"),
				("Ctrl+P", "Preferences"),
				("Ctrl+Q", "Quit"),
				("F1",     "Hotkeys"),
			},
			// 3 – SIMULATION
			new[]
			{
				("Ctrl+Space", "Pause / resume"),
				("Space",      "Step forward"),
			},
		};

		static readonly string[] SectionHeaders =
		{
			"EDITOR",
			"PLACEMENT & CAMERA",
			"CHIP",
			"SIMULATION",
		};

		// Left column: EDITOR + SIMULAÇÃO | Right column: COLOCAÇÃO + CHIP
		static readonly int[] LeftSections  = { 0, 3 };
		static readonly int[] RightSections = { 1, 2 };

		public static void DrawMenu()
		{
			DrawSettings.UIThemeDLS theme = DrawSettings.ActiveUITheme;
			MenuHelper.DrawBackgroundOverlay();
			Draw.ID panelID = UI.ReservePanel();

			Color headerCol = new Color(0.46f, 1f, 0.54f);
			Color keyCol    = new Color(1f, 0.85f, 0.3f);
			Color descCol   = Color.white;
			Color dimCol    = new Color(1f, 1f, 1f, 0.45f);

			float totalHeight = CalculateTotalHeight();
			Vector2 topLeft = UI.Centre + new Vector2(-MenuWidth * 0.5f, totalHeight * 0.5f);
			Vector2 cursor  = topLeft;

			Bounds2D contentBounds = Bounds2D.CreateFromTopLeftAndSize(topLeft, new Vector2(MenuWidth, totalHeight));

			using (UI.CreateMaskScope(contentBounds))
			{
				// Title
				UI.DrawText("HOTKEYS", theme.FontBold, theme.FontSizeRegular * 1.3f,
					cursor + new Vector2(MenuWidth * 0.5f, -RowH * 0.5f), Anchor.TextFirstLineCentre, headerCol);
				cursor.y -= RowH + SectionPadY;

				// Divider
				UI.DrawLine(cursor + Vector2.right * 0.5f, cursor + new Vector2(MenuWidth - 0.5f, 0f), 0.07f, dimCol);
				cursor.y -= SectionPadY;

				// Two columns
				float colStartY = cursor.y;
				DrawColumn(new Vector2(cursor.x,            colStartY), LeftSections,  theme, headerCol, keyCol, descCol);
				DrawColumn(new Vector2(cursor.x + HalfW + ColGap, colStartY), RightSections, theme, headerCol, keyCol, descCol);

				float colHeight = Mathf.Max(ColumnHeight(LeftSections), ColumnHeight(RightSections));
				cursor.y = colStartY - colHeight;

				// Vertical separator between columns
				float sepX = cursor.x + HalfW + ColGap * 0.5f;
				UI.DrawLine(new Vector2(sepX, colStartY), new Vector2(sepX, cursor.y), 0.06f, dimCol);

				// Close button
				cursor.y -= SectionPadY * 2f;
				if (UI.Button("CLOSE", theme.ButtonTheme,
					cursor + new Vector2(MenuWidth * 0.5f, 0),
					new Vector2(14f, DrawSettings.ButtonHeight), true, false, false, Anchor.CentreTop))
				{
					UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);
				}
			}

			MenuHelper.DrawReservedMenuPanel(panelID, contentBounds);

			if (KeyboardShortcuts.CancelShortcutTriggered)
				UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);
		}

		static void DrawColumn(Vector2 topLeft, int[] sectionIndices, DrawSettings.UIThemeDLS theme, Color headerCol, Color keyCol, Color descCol)
		{
			Vector2 cursor = topLeft;
			foreach (int s in sectionIndices)
			{
				cursor.y -= SectionPadY;
				UI.DrawText(SectionHeaders[s], theme.FontBold, theme.FontSizeRegular * 0.78f,
					cursor + new Vector2(EntryPadX, -RowH * 0.35f), Anchor.TextCentreLeft, headerCol);
				cursor.y -= RowH * 0.75f;

				foreach ((string key, string desc) in Sections[s])
				{
					float rowTop = cursor.y;
					UI.DrawPanel(cursor, new Vector2(ColKeyW, RowH), new Color(0.12f, 0.12f, 0.12f), Anchor.TopLeft);
					UI.DrawText(key, theme.FontBold, theme.FontSizeRegular * 0.78f,
						new Vector2(cursor.x + ColKeyW - EntryPadX, rowTop - RowH * 0.5f),
						Anchor.TextCentreRight, keyCol);
					UI.DrawText(desc, theme.FontRegular, theme.FontSizeRegular * 0.78f,
						new Vector2(cursor.x + ColKeyW + EntryPadX, rowTop - RowH * 0.5f),
						Anchor.TextCentreLeft, descCol);
					cursor.y -= RowH;
				}
			}
		}

		static float ColumnHeight(int[] sectionIndices)
		{
			float h = 0;
			foreach (int s in sectionIndices)
			{
				h += SectionPadY + RowH * 0.75f;
				h += Sections[s].Length * RowH;
			}
			return h;
		}

		static float CalculateTotalHeight()
		{
			float h = RowH + SectionPadY;
			h += SectionPadY;
			h += Mathf.Max(ColumnHeight(LeftSections), ColumnHeight(RightSections));
			h += SectionPadY * 2f + DrawSettings.ButtonHeight;
			return h;
		}
	}
}
