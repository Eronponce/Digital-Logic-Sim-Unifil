using DLS.Game;
using Seb.Types;
using Seb.Vis;
using Seb.Vis.UI;
using UnityEngine;

namespace DLS.Graphics
{
	public static class WireLabelMenu
	{
		const string MaxLabelLength = "MY LONG LABEL TEXT";
		static WireInstance wire;
		static readonly UIHandle ID_NameField = new("WireLabelMenu_NameField");

		static readonly string[] CancelConfirmButtonNames =
		{
			"CANCEL", "CONFIRM"
		};

		static readonly bool[] ButtonGroupInteractStates = { true, true };

		public static void OnMenuOpened()
		{
			wire = (WireInstance)ContextMenu.interactionContext;

			InputFieldState inputFieldState = UI.GetInputFieldState(ID_NameField);
			inputFieldState.SetText(wire.Label);
			inputFieldState.SelectAll();
		}

		public static void DrawMenu()
		{
			UI.DrawFullscreenPanel(DrawSettings.ActiveUITheme.MenuBackgroundOverlayCol);
			float spacing = 0.8f;

			DrawSettings.UIThemeDLS theme = DrawSettings.ActiveUITheme;
			InputFieldTheme inputTheme = DrawSettings.ActiveUITheme.ChipNameInputField;
			Draw.ID panelID = UI.ReservePanel();

			using (UI.BeginBoundsScope(true))
			{
				Vector2 unpaddedSize = Draw.CalculateTextBoundsSize(MaxLabelLength, inputTheme.fontSize, inputTheme.font);
				const float padX = 2.25f;
				Vector2 inputFieldSize = unpaddedSize + new Vector2(padX, 2.25f);
				Vector2 pos = UI.Centre + Vector2.up * 5;

				InputFieldState inputFieldState = UI.InputField(ID_NameField, inputTheme, pos, inputFieldSize, wire.Label, Anchor.Centre, padX / 2, ValidateLabelInput, true);
				Bounds2D inputFieldBounds = UI.PrevBounds;
				string newLabel = inputFieldState.text;

				Vector2 buttonsTopLeft = UI.PrevBounds.BottomLeft + Vector2.down * spacing;
				int buttonIndex = UI.HorizontalButtonGroup(CancelConfirmButtonNames, ButtonGroupInteractStates, theme.ButtonTheme, buttonsTopLeft, inputFieldBounds.Width, DrawSettings.DefaultButtonSpacing, 0, Anchor.TopLeft);

				MenuHelper.DrawReservedMenuPanel(panelID, UI.GetCurrentBoundsScope());

				if (KeyboardShortcuts.CancelShortcutTriggered || buttonIndex == 0) Cancel();
				else if (KeyboardShortcuts.ConfirmShortcutTriggered || buttonIndex == 1) Confirm(newLabel);
			}
		}

		static void Confirm(string newLabel)
		{
			wire.Label = newLabel;
			UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);
		}

		static void Cancel()
		{
			UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);
		}

		static bool ValidateLabelInput(string label) => label.Length <= MaxLabelLength.Length;
	}
}
