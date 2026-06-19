using System.Collections.Generic;
using System.Globalization;
using DLS.Game;
using Seb.Helpers;
using Seb.Vis;
using UnityEngine;

namespace DLS.Graphics
{
	public static class AnnotationEditMenu
	{
		public static AnnotationInstance Target => target;
		public static int CursorIndex => cursorIndex;
		public static bool CaretVisible => caretVisible;

		static AnnotationInstance target;
		static string textAtOpen;
		static int cursorIndex;
		static bool caretVisible;
		static float lastBlinkToggle;
		static float backspaceHeldSince = -1f;
		static float lastBackspaceRepeat;

		static readonly List<(string text, int cursor)> undoStack = new();
		static readonly List<(string text, int cursor)> redoStack = new();

		const float BackspaceStartDelay = 0.5f;
		const float BackspaceRepeatInterval = 0.04f;

		public static void Open(AnnotationInstance annotation)
		{
			target = annotation;
			UIDrawer.SetActiveMenu(UIDrawer.MenuType.AnnotationEdit);
		}

		public static void OnMenuOpened()
		{
			if (target == null && ContextMenu.interactionContext is AnnotationInstance ann)
				target = ann;
			if (target == null) return;

			textAtOpen = target.Text;
			string initText = target.Text == " " ? "" : target.Text;
			cursorIndex = initText.Length;
			caretVisible = true;
			lastBlinkToggle = Time.realtimeSinceStartup;
			backspaceHeldSince = -1f;
			undoStack.Clear();
			redoStack.Clear();
		}

		public static void DrawMenu()
		{
			if (target == null)
			{
				UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);
				return;
			}

			HandleKeyboardInput();

			float now = Time.realtimeSinceStartup;
			if (now - lastBlinkToggle >= 0.5f)
			{
				caretVisible = !caretVisible;
				lastBlinkToggle = now;
			}

			if (InputHelper.IsMouseDownThisFrame(MouseButton.Left) && !InteractionState.MouseIsOverUI)
			{
				if (!InputHelper.MouseInsideBounds_World(target.Position, target.ComputedSize))
					Confirm();
			}
			else if (KeyboardShortcuts.ConfirmShortcutTriggered) Confirm();
			else if (KeyboardShortcuts.CancelShortcutTriggered) Cancel();
		}

		static void HandleKeyboardInput()
		{
			string raw = target.Text == " " ? "" : target.Text;
			bool changed = false;
			bool undoPushedThisFrame = false;

			void Snapshot()
			{
				if (undoPushedThisFrame) return;
				undoStack.Add((raw, cursorIndex));
				redoStack.Clear();
				undoPushedThisFrame = true;
			}

			// Undo / Redo
			if (InputHelper.CtrlIsHeld && InputHelper.IsKeyDownThisFrame(KeyCode.Z))
			{
				if (undoStack.Count > 0)
				{
					redoStack.Add((raw, cursorIndex));
					(raw, cursorIndex) = undoStack[^1];
					undoStack.RemoveAt(undoStack.Count - 1);
					changed = true;
				}
			}
			else if (InputHelper.CtrlIsHeld && InputHelper.IsKeyDownThisFrame(KeyCode.Y))
			{
				if (redoStack.Count > 0)
				{
					undoStack.Add((raw, cursorIndex));
					(raw, cursorIndex) = redoStack[^1];
					redoStack.RemoveAt(redoStack.Count - 1);
					changed = true;
				}
			}
			else
			{
				foreach (char c in InputHelper.InputStringThisFrame)
				{
					bool invalid = char.IsControl(c) || char.IsSurrogate(c) ||
					               CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.Format ||
					               CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.PrivateUse;
					if (invalid) continue;
					Snapshot();
					raw = raw.Insert(cursorIndex, c.ToString());
					cursorIndex++;
					changed = true;
				}

				float t = Time.time;
				bool bsDown = InputHelper.IsKeyDownThisFrame(KeyCode.Backspace);
				bool bsHeld = InputHelper.IsKeyHeld(KeyCode.Backspace);
				if (bsDown) { backspaceHeldSince = t; lastBackspaceRepeat = t; }
				bool bsRepeat = bsHeld && backspaceHeldSince > 0 &&
				                t - backspaceHeldSince > BackspaceStartDelay &&
				                t - lastBackspaceRepeat >= BackspaceRepeatInterval;
				if (bsRepeat) lastBackspaceRepeat = t;

				if ((bsDown || bsRepeat) && cursorIndex > 0 && raw.Length > 0)
				{
					Snapshot();
					raw = raw.Remove(cursorIndex - 1, 1);
					cursorIndex--;
					changed = true;
				}
				if (!bsHeld) backspaceHeldSince = -1f;

				if (InputHelper.IsKeyDownThisFrame(KeyCode.Delete) && cursorIndex < raw.Length)
				{
					Snapshot();
					raw = raw.Remove(cursorIndex, 1);
					changed = true;
				}

				if (InputHelper.IsKeyDownThisFrame(KeyCode.LeftArrow)) { cursorIndex = Mathf.Max(0, cursorIndex - 1); ResetBlink(); }
				if (InputHelper.IsKeyDownThisFrame(KeyCode.RightArrow)) { cursorIndex = Mathf.Min(raw.Length, cursorIndex + 1); ResetBlink(); }
				if (InputHelper.IsKeyDownThisFrame(KeyCode.Home)) { cursorIndex = 0; ResetBlink(); }
				if (InputHelper.IsKeyDownThisFrame(KeyCode.End)) { cursorIndex = raw.Length; ResetBlink(); }
			}

			if (changed)
			{
				target.Text = string.IsNullOrEmpty(raw) ? " " : raw;
				cursorIndex = Mathf.Clamp(cursorIndex, 0, raw.Length);
				ResetBlink();
			}
		}

		static void ResetBlink()
		{
			caretVisible = true;
			lastBlinkToggle = Time.realtimeSinceStartup;
		}

		static void Confirm()
		{
			if (target != null)
			{
				string t = target.Text == " " ? "" : target.Text;
				target.Text = string.IsNullOrWhiteSpace(t) ? "Note" : t.Trim();
			}
			target = null;
			UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);
		}

		static void Cancel()
		{
			if (target != null)
				target.Text = textAtOpen;
			target = null;
			UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);
		}
	}
}
