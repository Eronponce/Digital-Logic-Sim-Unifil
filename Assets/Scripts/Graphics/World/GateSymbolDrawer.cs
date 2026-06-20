using DLS.Description;
using DLS.Game;
using Seb.Vis;
using UnityEngine;
using static DLS.Graphics.DrawSettings;

namespace DLS.Graphics
{
	public static class GateSymbolDrawer
	{
		const float StrokeT = 0.045f;
		const float BubbleR = 0.09f;
		const int   Segs    = 10;

		// Reusable boundary point buffer (3 * Segs max)
		static readonly Vector2[] pts = new Vector2[Segs * 3];

		// Longer names first: XNOR before XOR/NOR, NAND before AND, NOR before OR
		static readonly string[] GateNames = { "XNOR", "NOR", "NAND", "XOR", "NOT", "AND", "OR" };

		public static bool IsGateChip(SubChipInstance chip)
		{
			if (chip.ChipType == ChipType.Nand) return true;
			string n = chip.Description.Name;
			if (string.IsNullOrEmpty(n)) return false;
			n = n.Trim().ToUpperInvariant();
			foreach (string g in GateNames)
				if (MatchesGateName(n, g)) return true;
			return false;
		}

		public static void Draw(SubChipInstance chip, Color fill, Color outline)
		{
			string name = chip.ChipType == ChipType.Nand
				? "NAND"
				: chip.Description.Name.Trim().ToUpperInvariant();

			string baseName = GetBaseGateName(name);
			switch (baseName)
			{
				case "AND":  DrawAND(chip, fill, outline, false); break;
				case "NAND": DrawAND(chip, fill, outline, true);  break;
				case "OR":   DrawOR(chip, fill, outline, false, false); break;
				case "NOR":  DrawOR(chip, fill, outline, true,  false); break;
				case "XOR":  DrawOR(chip, fill, outline, false, true);  break;
				case "XNOR": DrawOR(chip, fill, outline, true,  true);  break;
				case "NOT":  DrawNOT(chip, fill, outline); break;
			}
		}

		// Returns the canonical gate name that this chip name starts with, or the name itself
		static string GetBaseGateName(string name)
		{
			foreach (string g in GateNames)
				if (MatchesGateName(name, g)) return g;
			return name;
		}

		// Gate name must be at string edge or bordered by digit, '_', or '-'
		// "3AND", "AND-3", "AND_8bit", "MY-OR" → match. "COMMAND", "STORAGE" → no match.
		static bool MatchesGateName(string name, string gate)
		{
			int idx = 0;
			while (true)
			{
				idx = name.IndexOf(gate, idx, System.StringComparison.Ordinal);
				if (idx == -1) return false;
				bool beforeOk = idx == 0 || IsBoundary(name[idx - 1]);
				bool afterOk  = idx + gate.Length == name.Length || IsBoundary(name[idx + gate.Length]);
				if (beforeOk && afterOk) return true;
				idx++;
			}
		}

		static bool IsBoundary(char c) => char.IsDigit(c) || c == '_' || c == '-';

		// ── AND / NAND ─────────────────────────────────────────────────────────────

		static void DrawAND(SubChipInstance chip, Color fill, Color outline, bool bubble)
		{
			Vector2 pos = chip.Position;
			float hw = chip.Size.x * 0.5f;
			float hh = chip.Size.y * 0.5f;

			var TL = new Vector2(pos.x - hw, pos.y + hh);
			var BL = new Vector2(pos.x - hw, pos.y - hh);
			var RT = new Vector2(pos.x + hw, pos.y);

			FillAND(pos, hw, hh, fill);

			Seb.Vis.Draw.Line(TL, BL, StrokeT, outline);
			Seb.Vis.Draw.QuadraticBezier(TL, new Vector2(pos.x + hw, pos.y + hh), RT, StrokeT, outline, Segs);
			Seb.Vis.Draw.QuadraticBezier(RT, new Vector2(pos.x + hw, pos.y - hh), BL, StrokeT, outline, Segs);

			if (bubble)
				foreach (PinInstance pin in chip.OutputPins)
					DrawBubble(pin.GetWorldPos(), outline, fill);
		}

		static void FillAND(Vector2 pos, float hw, float hh, Color col)
		{
			var C  = new Vector2(pos.x - hw, pos.y);
			float rx = 2f * hw;
			float ry = hh;
			int N = Segs * 2;
			var prev = new Vector2(C.x, C.y - ry);
			for (int i = 1; i <= N; i++)
			{
				float t = -Mathf.PI * 0.5f + Mathf.PI * i / N;
				var next = new Vector2(C.x + rx * Mathf.Cos(t), C.y + ry * Mathf.Sin(t));
				Seb.Vis.Draw.Triangle(C, prev, next, col);
				prev = next;
			}
		}

		// ── OR / NOR / XOR / XNOR ─────────────────────────────────────────────────

		static void DrawOR(SubChipInstance chip, Color fill, Color outline, bool bubble, bool xor)
		{
			Vector2 pos = chip.Position;
			float hw = chip.Size.x * 0.5f;
			float hh = chip.Size.y * 0.5f;

			// XOR/XNOR: indent the body rightward so the extra arc sits right at the input pins
			float ind = xor ? hw * 0.40f : 0f;

			// Body corners (shifted right for XOR)
			var TLb = new Vector2(pos.x - hw + ind, pos.y + hh);
			var BLb = new Vector2(pos.x - hw + ind, pos.y - hh);
			var RT  = new Vector2(pos.x + hw, pos.y);

			var leftCtrl = new Vector2(pos.x - hw + ind + hw * 0.5f, pos.y);
			var topCtrl  = new Vector2(pos.x + hw * 0.2f,            pos.y + hh);
			var botCtrl  = new Vector2(pos.x + hw * 0.2f,            pos.y - hh);

			// Fill: triangle fan from chip center through body boundary
			int n = SampleORBoundary(BLb, leftCtrl, TLb, topCtrl, botCtrl, RT);
			for (int i = 0; i < n - 1; i++)
				Seb.Vis.Draw.Triangle(pos, pts[i], pts[i + 1], fill);
			Seb.Vis.Draw.Triangle(pos, pts[n - 1], pts[0], fill);

			// Outline: body strokes
			Seb.Vis.Draw.QuadraticBezier(BLb, leftCtrl, TLb, StrokeT, outline, Segs);
			Seb.Vis.Draw.QuadraticBezier(TLb, topCtrl,  RT,  StrokeT, outline, Segs);
			Seb.Vis.Draw.QuadraticBezier(RT,  botCtrl,  BLb, StrokeT, outline, Segs);

			// XOR extra arc at the original left edge — exactly where input pins are
			if (xor)
			{
				var TL       = new Vector2(pos.x - hw, pos.y + hh);
				var BL       = new Vector2(pos.x - hw, pos.y - hh);
				var origCtrl = new Vector2(pos.x - hw + hw * 0.5f, pos.y);
				Seb.Vis.Draw.QuadraticBezier(BL, origCtrl, TL, StrokeT, outline, Segs);
			}

			if (bubble)
				foreach (PinInstance pin in chip.OutputPins)
					DrawBubble(pin.GetWorldPos(), outline, fill);
		}

		// Fills pts[] with boundary samples (3*Segs points, no duplicate endpoints)
		static int SampleORBoundary(Vector2 BL, Vector2 leftCtrl, Vector2 TL,
		                             Vector2 topCtrl, Vector2 botCtrl, Vector2 RT)
		{
			int n = 0;
			for (int i = 0; i < Segs; i++) pts[n++] = QuadBez(BL, leftCtrl, TL, (float)i / Segs);
			for (int i = 0; i < Segs; i++) pts[n++] = QuadBez(TL, topCtrl,  RT, (float)i / Segs);
			for (int i = 0; i < Segs; i++) pts[n++] = QuadBez(RT, botCtrl,  BL, (float)i / Segs);
			return n;
		}

		// ── NOT ───────────────────────────────────────────────────────────────────

		static void DrawNOT(SubChipInstance chip, Color fill, Color outline)
		{
			Vector2 pos = chip.Position;
			float hw = chip.Size.x * 0.5f;
			float hh = chip.Size.y * 0.5f;

			float tipX = pos.x + hw - BubbleR * 2f;
			var TL = new Vector2(pos.x - hw, pos.y + hh);
			var BL = new Vector2(pos.x - hw, pos.y - hh);
			var RT = new Vector2(tipX, pos.y);

			Seb.Vis.Draw.Triangle(TL, BL, RT, fill);
			Seb.Vis.Draw.Line(TL, BL, StrokeT, outline);
			Seb.Vis.Draw.Line(BL, RT, StrokeT, outline);
			Seb.Vis.Draw.Line(RT, TL, StrokeT, outline);

			foreach (PinInstance pin in chip.OutputPins)
				DrawBubble(pin.GetWorldPos(), outline, fill);
		}

		// ── Helpers ───────────────────────────────────────────────────────────────

		static void DrawBubble(Vector2 centre, Color outline, Color fill)
		{
			Seb.Vis.Draw.Point(centre, BubbleR, fill);
			Seb.Vis.Draw.PointOutline(centre, BubbleR, StrokeT, outline);
		}

		static Vector2 QuadBez(Vector2 p0, Vector2 p1, Vector2 p2, float t)
		{
			float u = 1f - t;
			return u * u * p0 + 2f * u * t * p1 + t * t * p2;
		}
	}
}
