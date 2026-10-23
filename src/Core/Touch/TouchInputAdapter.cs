using System;
using System.Collections.Generic;
using Godot;

namespace MegaCrit.Sts2.Core.Touch;

/// <summary>
/// 触屏输入适配层。Godot 自带的 emulate_mouse_from_touch 已经把单指触摸映射成鼠标左键，
/// 但游戏依赖「右键查看卡牌/遗物详情」和「ESC 返回」，这两类在 iOS 上无处触发。
/// 本类只补这两条，其余手势一律放行给引擎默认行为，避免破坏拖牌手感。
///
/// 手势映射：
///   单指长按 550ms（移动 &lt; 14px）→ 右键按下 + 抬起（打开详情面板）
///   双指轻点（两只手指都在 260ms 内抬起）→ ui_cancel（关闭面板 / 返回）
///   其余 → 不拦截，交给 emulate_mouse_from_touch
/// </summary>
[ScriptPath("res://src/Core/Touch/TouchInputAdapter.cs")]
public partial class TouchInputAdapter : Node
{
	private const float LongPressSeconds = 0.55f;
	private const float LongPressMoveTolerance = 14.0f;
	private const float TwoFingerTapSeconds = 0.26f;

	private readonly Dictionary<int, TouchPoint> _points = new Dictionary<int, TouchPoint>();

	private bool _longPressFired;
	private double _firstTouchAt;
	private int _maxConcurrentTouches;
	private bool _twoFingerTapCandidate;

	private readonly struct TouchPoint
	{
		public readonly Vector2 StartPosition;
		public readonly Vector2 Position;
		public readonly double StartTime;
		public readonly bool Moved;

		public TouchPoint(Vector2 startPosition, Vector2 position, double startTime, bool moved)
		{
			StartPosition = startPosition;
			Position = position;
			StartTime = startTime;
			Moved = moved;
		}
	}

	public override void _Ready()
	{
		// 触摸事件必须能在 _Input 里看到，即使游戏暂停或界面吞事件。
		ProcessMode = ProcessModeEnum.Always;
	}

	public override void _Input(InputEvent inputEvent)
	{
		switch (inputEvent)
		{
			case InputEventScreenTouch touch:
				HandleTouch(touch);
				break;
			case InputEventScreenDrag drag:
				HandleDrag(drag);
				break;
		}
	}

	public override void _Process(double delta)
	{
		if (_points.Count != 1 || _longPressFired)
		{
			return;
		}

		foreach (KeyValuePair<int, TouchPoint> pair in _points)
		{
			TouchPoint point = pair.Value;
			if (point.Moved)
			{
				return;
			}
			if (Time.GetTicksMsec() / 1000.0 - point.StartTime < LongPressSeconds)
			{
				return;
			}

			_longPressFired = true;
			EmitRightClick(point.Position, pressed: true);
			EmitRightClick(point.Position, pressed: false);
			return;
		}
	}

	private void HandleTouch(InputEventScreenTouch touch)
	{
		double now = Time.GetTicksMsec() / 1000.0;

		if (touch.Pressed)
		{
			if (_points.Count == 0)
			{
				_firstTouchAt = now;
				_maxConcurrentTouches = 1;
				_longPressFired = false;
				_twoFingerTapCandidate = false;
			}
			else
			{
				_maxConcurrentTouches = Math.Max(_maxConcurrentTouches, _points.Count + 1);
				if (_points.Count + 1 >= 2)
				{
					_twoFingerTapCandidate = true;
				}
			}

			_points[touch.Index] = new TouchPoint(touch.Position, touch.Position, now, moved: false);
			return;
		}

		bool hadPoint = _points.Remove(touch.Index);

		// 长按已经触发过右键，抬起时不再做别的判断。
		if (_longPressFired)
		{
			if (_points.Count == 0)
			{
				ResetGestureState();
			}
			return;
		}

		if (hadPoint && _points.Count == 0)
		{
			bool twoFingerTap = _twoFingerTapCandidate
				&& _maxConcurrentTouches >= 2
				&& now - _firstTouchAt <= TwoFingerTapSeconds;

			if (twoFingerTap)
			{
				EmitAction("ui_cancel");
			}

			ResetGestureState();
		}
	}

	private void HandleDrag(InputEventScreenDrag drag)
	{
		if (!_points.TryGetValue(drag.Index, out TouchPoint point))
		{
			return;
		}

		bool moved = point.Moved
			|| point.StartPosition.DistanceTo(drag.Position) > LongPressMoveTolerance;

		_points[drag.Index] = new TouchPoint(point.StartPosition, drag.Position, point.StartTime, moved);
	}

	private void ResetGestureState()
	{
		_maxConcurrentTouches = 0;
		_longPressFired = false;
		_twoFingerTapCandidate = false;
	}

	private static void EmitRightClick(Vector2 position, bool pressed)
	{
		InputEventMouseButton mouseButton = new InputEventMouseButton
		{
			ButtonIndex = MouseButton.Right,
			Pressed = pressed,
			Position = position,
			GlobalPosition = position,
			ButtonMask = pressed ? MouseButtonMask.Right : 0
		};
		Input.ParseInputEvent(mouseButton);
	}

	private static void EmitAction(string action)
	{
		InputEventAction press = new InputEventAction
		{
			Action = action,
			Pressed = true
		};
		Input.ParseInputEvent(press);

		InputEventAction release = new InputEventAction
		{
			Action = action,
			Pressed = false
		};
		Input.ParseInputEvent(release);
	}
}
