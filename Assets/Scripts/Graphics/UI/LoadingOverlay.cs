using Seb.Helpers;
using Seb.Vis;
using Seb.Vis.UI;
using UnityEngine;

namespace DLS.Graphics
{
	/// <summary>
	/// Overlay modal de "carregando" para operações longas (login, signup, sync, etc).
	/// Consumidor chama Show(mensagem) antes da operação e Hide() quando termina (sucesso ou erro).
	/// Draw() deve ser chamado uma vez por frame pelo menu que está visível, após todo o resto do UI.
	/// </summary>
	public static class LoadingOverlay
	{
		static string message = string.Empty;
		static bool visible;

		public static bool IsVisible => visible;

		public static void Show(string msg)
		{
			message = msg ?? string.Empty;
			visible = true;
		}

		public static void Hide()
		{
			visible = false;
			message = string.Empty;
		}

		public static void Draw()
		{
			if (!visible) return;

			DrawSettings.UIThemeDLS theme = DrawSettings.ActiveUITheme;

			UI.StartNewLayer();
			UI.DrawFullscreenPanel(new Color(0f, 0f, 0f, 0.72f));

			Vector2 panelSize = new(44f, 12f);
			UI.DrawPanel(UI.Centre, panelSize, ColHelper.MakeCol255(37, 37, 43), Anchor.Centre);

			Vector2 titlePos = UI.Centre + Vector2.up * 1.1f;
			UI.DrawText(message, theme.FontBold, theme.FontSizeRegular * 1.05f, titlePos, Anchor.Centre, Color.white);

			DrawSpinnerDots(UI.Centre + Vector2.down * 2f, theme);
		}

		static void DrawSpinnerDots(Vector2 centre, DrawSettings.UIThemeDLS theme)
		{
			const int dotCount = 3;
			const float dotRadius = 0.45f;
			const float dotSpacing = 1.8f;
			const float cycleSeconds = 1.2f;

			float totalWidth = dotSpacing * (dotCount - 1);
			float t = (Time.unscaledTime % cycleSeconds) / cycleSeconds;

			for (int i = 0; i < dotCount; i++)
			{
				float phase = i / (float)dotCount;
				float wave = Mathf.Abs(Mathf.Sin(Mathf.PI * (t + phase)));
				float alpha = Mathf.Lerp(0.25f, 1f, wave);
				Color dotCol = new(1f, 1f, 1f, alpha);

				Vector2 pos = centre + Vector2.right * (i * dotSpacing - totalWidth * 0.5f);
				UI.DrawPanel(pos, Vector2.one * dotRadius * 2f, dotCol, Anchor.Centre);
			}
		}
	}
}
