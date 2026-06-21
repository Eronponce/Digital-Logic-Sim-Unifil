using Seb.Helpers;
using Seb.Vis;
using UnityEngine;

namespace DLS.Graphics
{
	public static class WireDrawer
	{
		public static float DrawWireStraight(Vector2[] points, float thickness, Color col, Vector2 interactPos)
		{
			float interactSqrDst = float.MaxValue;
			Vector2 inA = points[0];

			for (int i = 1; i < points.Length; i++)
			{
				Vector2 inB = points[i];
				WireSegmentDraw(inA, inB, thickness, col, interactPos, ref interactSqrDst);
				inA = inB;
			}

			return interactSqrDst;
		}

		static void WireSegmentDraw(Vector2 start, Vector2 end, float thickness, Color col, Vector2 interactPos, ref float minSqrDst)
		{
			Draw.Line(start, end, thickness, col);
			float sqrDst = Maths.SqrDistanceToLineSegment(interactPos, start, end);
			if (sqrDst < minSqrDst) minSqrDst = sqrDst;
		}

		public static float DrawWireDashed(Vector2[] points, float thickness, Color col, Vector2 interactPos)
		{
			const float dashLen = 0.18f;
			const float gapLen = 0.12f;
			const float cycleLen = dashLen + gapLen;
			const float minStep = 0.0005f;
			float interactSqrDst = float.MaxValue;
			float distAlongWire = 0;

			for (int i = 1; i < points.Length; i++)
			{
				Vector2 a = points[i - 1];
				Vector2 b = points[i];
				float segLen = Vector2.Distance(a, b);
				if (segLen < minStep) continue;

				float t = 0;
				int safetyLimit = Mathf.CeilToInt(segLen / minStep) + 10;
				while (t < segLen - minStep && safetyLimit-- > 0)
				{
					float posInCycle = distAlongWire % cycleLen;
					bool inDash = posInCycle < dashLen;
					float remainInPhase = inDash ? dashLen - posInCycle : cycleLen - posInCycle;
					float step = Mathf.Max(Mathf.Min(remainInPhase, segLen - t), minStep);

					if (inDash)
					{
						float tEnd = Mathf.Min(t + step, segLen);
						Vector2 segA = Vector2.Lerp(a, b, t / segLen);
						Vector2 segB = Vector2.Lerp(a, b, tEnd / segLen);
						WireSegmentDraw(segA, segB, thickness, col, interactPos, ref interactSqrDst);
					}

					t += step;
					distAlongWire += step;
				}
			}

			return interactSqrDst;
		}

		public static float DrawWireDouble(Vector2[] points, float thickness, Color col, Vector2 interactPos)
		{
			float offset = thickness * 1.6f;
			float thinThickness = thickness * 0.55f;
			float interactSqrDst = float.MaxValue;

			for (int i = 1; i < points.Length; i++)
			{
				Vector2 a = points[i - 1];
				Vector2 b = points[i];
				Vector2 dir = (b - a);
				if (dir.sqrMagnitude < 0.0001f) continue;
				dir.Normalize();
				Vector2 perp = new Vector2(-dir.y, dir.x) * offset;

				WireSegmentDraw(a + perp, b + perp, thinThickness, col, interactPos, ref interactSqrDst);
				WireSegmentDraw(a - perp, b - perp, thinThickness, col, interactPos, ref interactSqrDst);
			}

			return interactSqrDst;
		}

		public static float DrawWireCurved(Vector2[] points, float thickness, Color col, Vector2 interactPos)
		{
			float interactSqrDst = float.MaxValue;
			Vector2 inA = points[0];

			float curveSize = 0.12f;
			int resolution = 20;

			for (int i = 1; i < points.Length - 1; i++)
			{
				Vector2 inB = points[i];
				Vector2 inC = points[i + 1];
				Vector2 targetPoint = inB;
				Vector2 targetDir = (inB - inA).normalized;
				float dstToTarget = (inB - inA).magnitude;
				float dstToCurveStart = Mathf.Max(dstToTarget - curveSize, dstToTarget / 2);

				Vector2 nextTargetDir = (inC - inB).normalized;
				float nextLineLength = (inC - inB).magnitude;

				Vector2 curveStartPoint = inA + targetDir * dstToCurveStart;
				Vector2 curveEndPoint = targetPoint + nextTargetDir * Mathf.Min(curveSize, nextLineLength / 2);

				// Bezier
				for (int j = 0; j < resolution; j++)
				{
					float t = j / (resolution - 1f);
					Vector2 a = Vector2.Lerp(curveStartPoint, targetPoint, t);
					Vector2 b = Vector2.Lerp(targetPoint, curveEndPoint, t);
					Vector2 p = Vector2.Lerp(a, b, t);

					WireSegmentDraw(inA, p, thickness, col, interactPos, ref interactSqrDst);
					inA = p;
				}
			}

			WireSegmentDraw(inA, points[^1], thickness, col, interactPos, ref interactSqrDst);
			return interactSqrDst;
		}
	}
}