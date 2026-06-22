using DLS.Description;
using DLS.Game;
using Seb.Helpers;
using Seb.Types;
using Seb.Vis;
using Seb.Vis.UI;
using UnityEngine;

namespace DLS.Graphics
{
	public static class WireStyleMenu
	{
		static readonly UIHandle ID_ColourPicker = new("WireStyle_ColourPicker");

		static readonly int[] HueOrder =
		{
			0,  8,  1,  9,
			2, 10,  3, 11,
			12, 13,  4, 14,
			5, 15,  6,  7
		};

		static readonly string[] PatternLabels = { "NONE", "DASHED", "DOUBLE" };
		static readonly bool[] PatternEnabled = { true, true, true };

		static PinInstance sourcePin;
		static bool contextPinIsSource;
		static bool hasCustomColour;
		static Color customColour = Color.red;
		static WirePattern pattern;
		static SliderState opacitySlider = new SliderState { progressT = 1f };

		static bool WireMatchesContextPin(WireInstance w) =>
			contextPinIsSource
				? PinAddress.Equals(w.SourcePin.Address, sourcePin.Address)
				: PinAddress.Equals(w.TargetPin.Address, sourcePin.Address);

		public static void OnMenuOpened()
		{
			sourcePin = (PinInstance)ContextMenu.interactionContext;
			contextPinIsSource = sourcePin.IsSourcePin;

			foreach (WireInstance w in Project.ActiveProject.ViewedChip.Wires)
			{
				if (w.IsFullyConnected && WireMatchesContextPin(w))
				{
					hasCustomColour = w.HasCustomColour;
					Color baseCol = w.HasCustomColour ? w.CustomColour : DrawSettings.GetStateColour(true, (uint)sourcePin.Colour);
					customColour = new Color(baseCol.r, baseCol.g, baseCol.b, 1f);
					opacitySlider.progressT = w.HasCustomColour ? Mathf.Clamp01(w.CustomColour.a) : 1f;
					pattern = w.Pattern;
					UI.GetColourPickerState(ID_ColourPicker).SetRGB(customColour);
					return;
				}
			}

			// No wire connected — restore from pin's saved style if any
			hasCustomColour = sourcePin.HasInheritedWireColour;
			Color savedCol = sourcePin.HasInheritedWireColour
				? sourcePin.InheritedWireColour
				: DrawSettings.GetStateColour(true, (uint)sourcePin.Colour);
			customColour = new Color(savedCol.r, savedCol.g, savedCol.b, 1f);
			opacitySlider.progressT = sourcePin.HasInheritedWireColour ? Mathf.Clamp01(sourcePin.InheritedWireColour.a) : 1f;
			UI.GetColourPickerState(ID_ColourPicker).SetRGB(customColour);
			pattern = sourcePin.InheritedWirePattern;
		}

		public static void DrawMenu()
		{
			DrawSettings.UIThemeDLS theme = DrawSettings.ActiveUITheme;
			ButtonTheme btnTheme = theme.ButtonTheme;

			float colW     = 10f;
			float colGap   = 1.5f;
			float totalW   = colW * 2 + colGap;   // 21.5
			float swatchGap = 0.2f;
			float pad      = 0.5f;
			float btnH     = DrawSettings.ButtonHeight;  // 2.5
			float sliderTrackH = 0.4f;   // thin track — handle = trackH * 1.5 (small)
			float sliderSlotH  = 1.5f;   // allocated vertical space (handle extends beyond track)
			float swatchSize = (colW - 3 * swatchGap) / 4f;  // ≈2.35, gridH ≈ colW
			float panPad   = DrawSettings.PanelUIPadding * 0.5f;  // 1.15

			// content layout: swatches/picker | DEFAULT | slider slot | pattern | OK
			float contentH = colW + pad + btnH + pad + sliderSlotH + pad + btnH + pad + btnH;

			// Anchor content to top-right corner
			Vector2 topLeft = new Vector2(
				UI.Width - 0.5f - panPad - totalW,
				UI.Height - 0.5f - panPad
			);

			// All Y positions pre-computed from topLeft (never from PrevBounds)
			float yDefault = topLeft.y - colW - pad;
			float ySlider  = yDefault - btnH - pad;
			float yPattern = ySlider - sliderSlotH - pad;
			float yOK      = yPattern - btnH - pad;

			// Fixed panel rendered first (renders behind content)
			Draw.ID panelID = UI.ReservePanel();
			Bounds2D contentBounds = Bounds2D.CreateFromTopLeftAndSize(topLeft, new Vector2(totalW, contentH));
			MenuHelper.DrawReservedMenuPanel(panelID, contentBounds);

			Bounds2D panelBounds = Bounds2D.Grow(contentBounds, DrawSettings.PanelUIPadding);

			// ---- Left column: 4×4 hue swatches ----
			for (int i = 0; i < HueOrder.Length; i++)
			{
				int colourIndex = HueOrder[i];
				int col = i % 4;
				int row = i / 4;
				Vector2 swatchPos = topLeft + new Vector2(col * (swatchSize + swatchGap), -row * (swatchSize + swatchGap));
				Color swatchCol = DrawSettings.GetStateColour(true, (uint)colourIndex);

				UI.DrawPanel(swatchPos, new Vector2(swatchSize, swatchSize), swatchCol, Anchor.TopLeft);
				Bounds2D sb = UI.PrevBounds;
				bool hover = UI.MouseInsideBounds(sb);
				bool isCurrent = hasCustomColour && ApproxEqual(customColour, swatchCol);

				float ow = isCurrent ? 0.18f : 0.05f;
				Color oc = isCurrent ? Color.white : new Color(0.25f, 0.25f, 0.25f);
				if (hover) oc = Color.white;
				UI.DrawLine(sb.TopLeft, sb.TopRight, ow, oc);
				UI.DrawLine(sb.BottomLeft, sb.TopLeft, ow, oc);
				UI.DrawLine(sb.TopRight, sb.BottomRight, ow, oc);
				UI.DrawLine(sb.BottomLeft, sb.BottomRight, ow, oc);

				if (hover && InputHelper.IsMouseDownThisFrame(MouseButton.Left))
				{
					customColour = new Color(swatchCol.r, swatchCol.g, swatchCol.b, 1f);
					hasCustomColour = true;
					UI.GetColourPickerState(ID_ColourPicker).SetRGB(customColour);
				}
			}

			// ---- Right column: HSV colour picker ----
			Vector2 pickerPos = topLeft + new Vector2(colW + colGap, 0);
			Color newCol = UI.DrawColourPicker(ID_ColourPicker, pickerPos, colW, Anchor.TopLeft);
			Color newColOpaque = new Color(newCol.r, newCol.g, newCol.b, 1f);
			if (!ApproxEqual(newColOpaque, customColour))
			{
				customColour = newColOpaque;
				hasCustomColour = true;
			}

			// ---- DEFAULT toggle ----
			Vector2 posDefault = new Vector2(topLeft.x, yDefault);
			if (UI.Button("DEFAULT", btnTheme, posDefault, new Vector2(totalW, btnH), true, !hasCustomColour, false, Anchor.TopLeft))
			{
				hasCustomColour = false;
				Color pinNaturalCol = DrawSettings.GetStateColour(true, (uint)sourcePin.Colour);
				customColour = new Color(pinNaturalCol.r, pinNaturalCol.g, pinNaturalCol.b, 1f);
				opacitySlider.progressT = 1f;
				UI.GetColourPickerState(ID_ColourPicker).SetRGB(customColour);
			}

			// ---- Opacity slider ----
			// DrawSlider ignores anchor and treats pos as centre — pass actual UI-space centre
			float sliderCentreX = topLeft.x + totalW * 0.5f;
			float sliderCentreY = ySlider - sliderSlotH * 0.5f;
			UI.DrawSlider(new Vector2(sliderCentreX, sliderCentreY), new Vector2(totalW, sliderTrackH), Anchor.Centre, ref opacitySlider);

			// Auto-enable custom colour if user changes opacity while on default
			if (opacitySlider.progressT < 0.99f && !hasCustomColour)
				hasCustomColour = true;

			// ---- Pattern selector ----
			Vector2 posPattern = new Vector2(topLeft.x, yPattern);
			int selPat = UI.HorizontalButtonGroup(PatternLabels, PatternEnabled, btnTheme, posPattern, totalW, DrawSettings.DefaultButtonSpacing, (int)pattern, Anchor.TopLeft);
			if (selPat >= 0) pattern = (WirePattern)selPat;

			// ---- OK button ----
			Vector2 posOK = new Vector2(topLeft.x, yOK);
			if (UI.Button("OK", btnTheme, posOK, new Vector2(totalW, btnH), true, false, false, Anchor.TopLeft)
				|| KeyboardShortcuts.ConfirmShortcutTriggered)
			{
				ApplyToAllConnectedWires();
				UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);
				return;
			}

			if (KeyboardShortcuts.CancelShortcutTriggered)
			{
				UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);
				return;
			}

			// Close on left-click outside — checked last so buttons always process first
			if (InputHelper.IsMouseDownThisFrame(MouseButton.Left) && !UI.MouseInsideBounds(panelBounds))
			{
				ApplyToAllConnectedWires();
				UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);
				return;
			}

			ApplyToAllConnectedWires();
		}

		static bool ApproxEqual(Color a, Color b) =>
			Mathf.Abs(a.r - b.r) < 0.01f && Mathf.Abs(a.g - b.g) < 0.01f && Mathf.Abs(a.b - b.b) < 0.01f;

		static void ApplyToAllConnectedWires()
		{
			float alpha = Mathf.Clamp01(opacitySlider.progressT);

			Color pinNaturalCol = DrawSettings.GetStateColour(true, (uint)sourcePin.Colour);
			// DEFAULT resets to pin colour (not DLS logic-state green) so the wire
			// still shows the pin's assigned colour instead of falling through to
			// GetStateCol() which returns the default logic-state green.
			Color appliedColour = hasCustomColour
				? new Color(customColour.r, customColour.g, customColour.b, alpha)
				: new Color(pinNaturalCol.r, pinNaturalCol.g, pinNaturalCol.b, 1f);

			foreach (WireInstance w in Project.ActiveProject.ViewedChip.Wires)
			{
				if (w.IsFullyConnected && WireMatchesContextPin(w))
				{
					w.HasCustomColour = hasCustomColour;
					w.CustomColour = hasCustomColour ? appliedColour : Color.clear;
					w.Pattern = pattern;
				}
			}

			// Persist style on the pin so future wires from this pin inherit it.
			// Always keep HasInheritedWireColour = true so the wire uses the pin
			// colour (not the logic-state fallback) even in DEFAULT mode.
			sourcePin.HasInheritedWireColour = true;
			sourcePin.InheritedWireColour = appliedColour;
			sourcePin.InheritedWirePattern = pattern;
		}
	}
}
