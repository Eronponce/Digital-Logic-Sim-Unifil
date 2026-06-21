using System;
using System.Collections.Generic;
using System.Linq;
using DLS.Description;
using DLS.Game;
using Seb.Helpers;
using Seb.Types;
using Seb.Vis;
using Seb.Vis.UI;
using UnityEngine;

namespace DLS.Graphics
{
	public static class ChipCustomizationMenu
	{
		static readonly string[] nameDisplayOptions =
		{
			"Name: Middle",
			"Name: Top",
			"Name: Hidden"
		};

		static readonly Color[] SwatchColors =
		{
			new(0.90f, 0.20f, 0.20f), // red
			new(1.00f, 0.50f, 0.10f), // orange
			new(0.95f, 0.85f, 0.10f), // yellow
			new(0.20f, 0.80f, 0.20f), // green
			new(0.10f, 0.75f, 0.50f), // teal
			new(0.20f, 0.50f, 1.00f), // blue
			new(0.60f, 0.25f, 1.00f), // purple
			new(0.95f, 0.25f, 0.75f), // pink
			new(1.00f, 1.00f, 1.00f), // white
			new(0.70f, 0.70f, 0.70f), // light gray
			new(0.35f, 0.35f, 0.35f), // dark gray
			new(0.08f, 0.08f, 0.08f), // near black
		};


		// ---- State ----
		static SubChipInstance[] subChipsWithDisplays;
		static string displayLabelString;
		static string colHexCodeString;

		static readonly UIHandle ID_DisplaysScrollView = new("CustomizeMenu_DisplaysScroll");
		static readonly UIHandle ID_ColourPicker = new("CustomizeMenu_ChipCol");
		static readonly UIHandle ID_ColourHexInput = new("CustomizeMenu_ChipColHexInput");
		static readonly UIHandle ID_NameDisplayOptions = new("CustomizeMenu_NameDisplayOptions");
		static readonly UI.ScrollViewDrawElementFunc drawDisplayScrollEntry = DrawDisplayScroll;
		static readonly Func<string, bool> hexStringInputValidator = ValidateHexStringInput;

		public static void OnMenuOpened()
		{
			DevChipInstance chip = Project.ActiveProject.ViewedChip;
			subChipsWithDisplays = chip.GetSubchips().Where(c => c.Description.HasDisplay()).OrderBy(c => c.Position.x).ThenBy(c => c.Position.y).ToArray();
			CustomizationSceneDrawer.OnCustomizationMenuOpened();
			displayLabelString = $"DISPLAYS ({subChipsWithDisplays.Length}):";

			InitUIFromChipDescription();
		}

		public static void DrawMenu()
		{
			// Don't draw menu when placing display
			if (CustomizationSceneDrawer.IsPlacingDisplay) return;

			const float width = 20;
			const float pad = UILayoutHelper.DefaultSpacing;
			const float pw = width - pad * 2;

			DrawSettings.UIThemeDLS theme = DrawSettings.ActiveUITheme;
			UI.DrawPanel(UI.TopLeft, new Vector2(width, UI.Height), theme.MenuPanelCol, Anchor.TopLeft);

			// ---- Cancel/confirm buttons ----
			int cancelConfirmButtonIndex = MenuHelper.DrawButtonPair("CANCEL", "CONFIRM", UI.TopLeft + Vector2.down * pad, pw, false);

			// ---- Chip name UI ----
			int nameDisplayMode = UI.WheelSelector(ID_NameDisplayOptions, nameDisplayOptions, NextPos(), new Vector2(pw, DrawSettings.ButtonHeight), theme.OptionsWheel, Anchor.TopLeft);
			ChipSaveMenu.ActiveCustomizeDescription.NameLocation = (NameDisplayLocation)nameDisplayMode;

			// ---- Colour swatches ----
			DrawColourSwatches(NextPos(), pw);

			// ---- Chip colour UI ----
			Color newCol = UI.DrawColourPicker(ID_ColourPicker, NextPos(), pw, Anchor.TopLeft);
			InputFieldTheme inputTheme = MenuHelper.Theme.ChipNameInputField;
			inputTheme.fontSize = MenuHelper.Theme.FontSizeRegular;

			InputFieldState hexColInput = UI.InputField(ID_ColourHexInput, inputTheme, NextPos(), new Vector2(pw, DrawSettings.ButtonHeight), "#", Anchor.TopLeft, 1, hexStringInputValidator);

			if (newCol != ChipSaveMenu.ActiveCustomizeDescription.Colour)
			{
				ChipSaveMenu.ActiveCustomizeDescription.Colour = newCol;
				UpdateChipColHexStringFromColour(newCol);
			}
			else if (colHexCodeString != hexColInput.text)
			{
				UpdateChipColFromHexString(hexColInput.text);
			}

			// ---- Displays UI ----
			Color labelCol = ColHelper.Darken(theme.MenuPanelCol, 0.01f);
			Vector2 labelPos = NextPos(1);
			UI.TextWithBackground(labelPos, new Vector2(pw, DrawSettings.ButtonHeight), Anchor.TopLeft, displayLabelString, theme.FontBold, theme.FontSizeRegular, Color.white, labelCol);

			float scrollViewHeight = 20;
			float scrollViewSpacing = UILayoutHelper.DefaultSpacing;
			UI.DrawScrollView(ID_DisplaysScrollView, NextPos(), new Vector2(pw, scrollViewHeight), scrollViewSpacing, Anchor.TopLeft, theme.ScrollTheme, drawDisplayScrollEntry, subChipsWithDisplays.Length);

			Vector2 NextPos(float extraPadding = 0)
			{
				return UI.PrevBounds.BottomLeft + Vector2.down * (pad + extraPadding);
			}

			// Cancel
			if (cancelConfirmButtonIndex == 0)
			{
				RevertChanges();
				UIDrawer.SetActiveMenu(UIDrawer.MenuType.ChipSave);
			}
			// Confirm
			else if (cancelConfirmButtonIndex == 1)
			{
				UpdateCustomizeDescription();
				UIDrawer.SetActiveMenu(UIDrawer.MenuType.ChipSave);
			}
		}

		static void DrawDisplayScroll(Vector2 pos, float width, int i, bool isLayoutPass)
		{
			DrawSettings.UIThemeDLS theme = DrawSettings.ActiveUITheme;

			SubChipInstance subChip = subChipsWithDisplays[i];
			ChipDescription chipDesc = subChip.Description;
			string label = subChip.Label;
			string displayName = string.IsNullOrWhiteSpace(label) ? chipDesc.Name : label;

			// Don't allow adding same display multiple times
			bool enabled = CustomizationSceneDrawer.SelectedDisplay == null || subChip.ID != CustomizationSceneDrawer.SelectedDisplay.Desc.SubChipID; // display is removed from list when selected, so check manually here
			foreach (DisplayInstance d in ChipSaveMenu.ActiveCustomizeChip.Displays)
			{
				if (d.Desc.SubChipID == subChip.ID)
				{
					enabled = false;
					break;
				}
			}

			// Display selected, start placement
			if (UI.Button(displayName, theme.ButtonTheme, pos, new Vector2(width, 0), enabled, false, true, Anchor.TopLeft))
			{
				SubChipDescription subChipDesc = new(chipDesc.Name, subChipsWithDisplays[i].ID, string.Empty, Vector2.zero, null);
				SubChipInstance instance = new(chipDesc, subChipDesc);
				CustomizationSceneDrawer.StartPlacingDisplay(instance);
			}
		}

		static void RevertChanges()
		{
			ChipSaveMenu.RevertCustomizationStateToBeforeEnteringCustomizeMenu();
			InitUIFromChipDescription();
		}

		static void InitUIFromChipDescription()
		{
			// Init col picker to chip colour
			ColourPickerState chipColourPickerState = UI.GetColourPickerState(ID_ColourPicker);
			Color.RGBToHSV(ChipSaveMenu.ActiveCustomizeDescription.Colour, out chipColourPickerState.hue, out chipColourPickerState.sat, out chipColourPickerState.val);
			UpdateChipColHexStringFromColour(chipColourPickerState.GetRGB());

			// Init name display mode
			WheelSelectorState nameDisplayWheelState = UI.GetWheelSelectorState(ID_NameDisplayOptions);
			nameDisplayWheelState.index = (int)ChipSaveMenu.ActiveCustomizeDescription.NameLocation;
		}

		static void UpdateCustomizeDescription()
		{
			List<DisplayInstance> displays = ChipSaveMenu.ActiveCustomizeChip.Displays;
			ChipSaveMenu.ActiveCustomizeDescription.Displays = displays.Select(s => s.Desc).ToArray();
		}

		static void UpdateChipColHexStringFromColour(Color col)
		{
			int colInt = (byte)(col.r * 255) << 16 | (byte)(col.g * 255) << 8 | (byte)(col.b * 255);
			colHexCodeString = "#" + $"{colInt:X6}";
			UI.GetInputFieldState(ID_ColourHexInput).SetText(colHexCodeString, false);
		}

		static void UpdateChipColFromHexString(string hexString)
		{
			colHexCodeString = hexString;
			hexString = hexString.Replace("#", "");
			hexString = hexString.PadRight(6, '0');

			if (ColHelper.TryParseHexCode(hexString, out Color col))
			{
				UI.GetColourPickerState(ID_ColourPicker).SetRGB(col);
				ChipSaveMenu.ActiveCustomizeDescription.Colour = col;
			}
		}

		static void DrawColourSwatches(Vector2 topLeft, float width)
		{
			const int cols = 6;
			const int rows = 2;
			const float gap = 0.3f;
			const float swatchH = 1.5f;
			float swatchW = (width - gap * (cols - 1)) / cols;
			Vector2 swatchSize = new(swatchW, swatchH);

			for (int row = 0; row < rows; row++)
			{
				for (int col = 0; col < cols; col++)
				{
					int index = row * cols + col;
					if (index >= SwatchColors.Length) break;

					Color swatchCol = SwatchColors[index];
					Vector2 pos = topLeft + new Vector2(col * (swatchW + gap), -row * (swatchH + gap));

					UI.DrawPanel(pos, swatchSize, swatchCol, Anchor.TopLeft);
					Bounds2D swatchBounds = UI.PrevBounds;
					bool hover = UI.MouseInsideBounds(swatchBounds);

					if (hover)
					{
						UI.DrawLine(swatchBounds.TopLeft, swatchBounds.TopRight, 0.12f, Color.white);
						UI.DrawLine(swatchBounds.BottomLeft, swatchBounds.TopLeft, 0.12f, Color.white);
						UI.DrawLine(swatchBounds.TopRight, swatchBounds.BottomRight, 0.12f, Color.white);
						UI.DrawLine(swatchBounds.BottomLeft, swatchBounds.BottomRight, 0.12f, Color.white);

						if (InputHelper.IsMouseDownThisFrame(MouseButton.Left))
						{
							ChipSaveMenu.ActiveCustomizeDescription.Colour = swatchCol;
							UI.GetColourPickerState(ID_ColourPicker).SetRGB(swatchCol);
							UpdateChipColHexStringFromColour(swatchCol);
						}
					}
				}
			}

			float totalH = rows * swatchH + (rows - 1) * gap;
			UI.OverridePreviousBounds(Bounds2D.CreateFromTopLeftAndSize(topLeft, new Vector2(width, totalH)));
		}

		static bool ValidateHexStringInput(string text)
		{
			if (string.IsNullOrWhiteSpace(text)) return true;

			int numHexDigits = 0;

			for (int i = 0; i < text.Length; i++)
			{
				if (i == 0 && text[i] == '#') continue;

				if (Uri.IsHexDigit(text[i]))
				{
					numHexDigits++;
				}
				else return false;
			}

			return numHexDigits <= 6;
		}
	}
}