using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes;

namespace MegaCrit.Sts2.Core.Nodes.GodotExtensions;

/// <summary>
/// Nudges controls that were authored flush against a screen edge back inside the device safe area.
/// <para>
/// The scenes place corner buttons a handful of pixels from the edge, which is correct on a desktop
/// window and wrong on a phone. The iPhone 17 Pro Max reports a 62pt inset on both short edges in
/// landscape, so a button authored at <c>offset_right = -6</c> ends up roughly 300 canvas pixels
/// under the rounded corner and cannot be tapped. The project stretches with <c>canvas_items</c>, so
/// the inset has to be applied in canvas space, which is exactly what <c>NGame.SafeAreaRect</c>
/// already holds.
/// </para>
/// <para>
/// Only controls that never move themselves may register. Buttons that animate to a parked position
/// off screen - the back button, the proceed button, the end turn button - would be dragged back
/// into view, so they keep their own safe area aware placement instead.
/// </para>
/// </summary>
public static class NSafeAreaFit
{
	/// <summary>
	/// Controls waiting to be fitted. Entries are pruned as they leave the tree, so a screen that
	/// opens and closes a hundred times does not grow this list.
	/// </summary>
	private static readonly List<Control> _tracked = new List<Control>();

	/// <summary>
	/// Registers a control and fits it once immediately. <c>NGame</c> refits the whole registry
	/// every time it recomputes the safe area, so no per-control signal connection is needed and
	/// nothing has to be disconnected when the control dies.
	/// </summary>
	/// <param name="control">Control to keep inside the safe area.</param>
	public static void Watch(Control control)
	{
		if (!MobileDevice.IsMobile || control == null)
		{
			return;
		}
		_tracked.Add(control);
		Fit(control);
		// A control that has just entered the tree has not been laid out yet - its anchors are
		// resolved during the parent's next sort, so GetGlobalRect still reports the scene's
		// authored offsets. Fitting again once the frame settles catches the real rect.
		Callable.From(() => Fit(control)).CallDeferred();
	}

	/// <summary>
	/// Fits every registered control and drops the ones that have left the tree.
	/// </summary>
	public static void FitAll()
	{
		if (_tracked.Count == 0)
		{
			return;
		}
		for (int num = _tracked.Count - 1; num >= 0; num--)
		{
			Control control = _tracked[num];
			if (!GodotObject.IsInstanceValid(control) || !control.IsInsideTree())
			{
				_tracked.RemoveAt(num);
			}
			else
			{
				Fit(control);
			}
		}
	}

	/// <summary>
	/// Shifts the control the smallest amount that puts its rect inside the safe area. Idempotent:
	/// once the control is inside, the delta is zero and nothing moves.
	/// </summary>
	/// <param name="control">Control to fit. Ignored when it has already left the tree.</param>
	public static void Fit(Control control)
	{
		if (!GodotObject.IsInstanceValid(control) || !control.IsInsideTree())
		{
			return;
		}
		NGame game = NGame.Instance;
		if (game == null || !GodotObject.IsInstanceValid(game))
		{
			return;
		}
		Rect2 safe = game.SafeAreaRect;
		if (safe.Size.X <= 0f || safe.Size.Y <= 0f)
		{
			return;
		}
		Rect2 rect = control.GetGlobalRect();
		Vector2 delta = Vector2.Zero;
		if (rect.Size.X <= safe.Size.X)
		{
			if (rect.Position.X < safe.Position.X)
			{
				delta.X = safe.Position.X - rect.Position.X;
			}
			else if (rect.End.X > safe.End.X)
			{
				delta.X = safe.End.X - rect.End.X;
			}
		}
		else if (rect.Position.X > safe.Position.X)
		{
			delta.X = safe.Position.X - rect.Position.X;
		}
		if (rect.Size.Y <= safe.Size.Y)
		{
			if (rect.Position.Y < safe.Position.Y)
			{
				delta.Y = safe.Position.Y - rect.Position.Y;
			}
			else if (rect.End.Y > safe.End.Y)
			{
				delta.Y = safe.End.Y - rect.End.Y;
			}
		}
		else if (rect.Position.Y > safe.Position.Y)
		{
			delta.Y = safe.Position.Y - rect.Position.Y;
		}
		if (delta == Vector2.Zero)
		{
			return;
		}
		// GlobalPosition, not Position: GetGlobalRect is in canvas space and SafeAreaRect is too, so
		// the delta has to be applied in that same space. A screen that scales or offsets its subtree
		// would otherwise take the shift and apply it in the wrong units.
		control.GlobalPosition += delta;
	}
}
