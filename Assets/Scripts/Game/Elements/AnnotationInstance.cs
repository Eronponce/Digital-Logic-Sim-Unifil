using DLS.Description;
using Seb.Helpers;
using Seb.Types;
using UnityEngine;

namespace DLS.Game
{
	public class AnnotationInstance : IMoveable
	{
		public const float MaxWidth = 5f;

		public string Text;
		// ComputedSize is updated each frame by DevSceneDrawer based on wrapped text bounds
		public Vector2 ComputedSize = new(1f, 0.5f);

		public int ID { get; }
		public Vector2 Position { get; set; }
		public Vector2 MoveStartPosition { get; set; }
		public Vector2 StraightLineReferencePoint { get; set; }
		public bool IsSelected { get; set; }
		public bool IsValidMovePos { get; set; }
		public bool HasReferencePointForStraightLineMovement { get; set; }

		public Vector2 SnapPoint => Position;
		public Bounds2D BoundingBox => Bounds2D.CreateFromCentreAndSize(Position, ComputedSize);
		public Bounds2D SelectionBoundingBox => BoundingBox;

		public AnnotationInstance(AnnotationDescription desc)
		{
			ID = desc.ID;
			Text = string.IsNullOrEmpty(desc.Text) ? "Note" : desc.Text;
			Position = desc.Position;
		}

		public AnnotationDescription ToDescription() => new()
		{
			ID = ID,
			Text = Text,
			Position = Position,
			Size = ComputedSize,
			Colour = Color.clear
		};

		public bool ShouldBeIncludedInSelectionBox(Vector2 selectionCentre, Vector2 selectionSize) =>
			Maths.PointInBox2D(Position, selectionCentre, selectionSize);
	}
}
