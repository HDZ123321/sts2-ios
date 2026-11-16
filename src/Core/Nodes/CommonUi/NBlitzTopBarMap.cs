using System;
using Container = Godot.Container;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Runs;

namespace MegaCrit.Sts2.Core.Nodes.CommonUi;

[ScriptPath("res://src/Core/Nodes/CommonUi/NBlitzTopBarMap.cs")]
public class NBlitzTopBarMap : Control
{
	/// <summary>
	/// Cached StringNames for the methods contained in this class, for fast lookup.
	/// </summary>
	public new class MethodName : Control.MethodName
	{
		/// <summary>
		/// Cached name for the '_Ready' method.
		/// </summary>
		public new static readonly StringName _Ready = "_Ready";

		/// <summary>
		/// Cached name for the '_Notification' method.
		/// </summary>
		public new static readonly StringName _Notification = "_Notification";

		/// <summary>
		/// Cached name for the 'OnMapChanged' method.
		/// </summary>
		public static readonly StringName OnMapChanged = "OnMapChanged";

		/// <summary>
		/// Cached name for the 'Reposition' method.
		/// </summary>
		public static readonly StringName Reposition = "Reposition";

		/// <summary>
		/// Cached name for the 'SetMapNodeAsCurrent' method.
		/// </summary>
		public static readonly StringName SetMapNodeAsCurrent = "SetMapNodeAsCurrent";
	}

	/// <summary>
	/// Cached StringNames for the properties and fields contained in this class, for fast lookup.
	/// </summary>
	public new class PropertyName : Control.PropertyName
	{
		/// <summary>
		/// Cached name for the '_marker' field.
		/// </summary>
		public static readonly StringName _marker = "_marker";

		/// <summary>
		/// Cached name for the '_container' field.
		/// </summary>
		public static readonly StringName _container = "_container";

		/// <summary>
		/// Cached name for the '_pathContainer' field.
		/// </summary>
		public static readonly StringName _pathContainer = "_pathContainer";

		/// <summary>
		/// Cached name for the '_pointContainer' field.
		/// </summary>
		public static readonly StringName _pointContainer = "_pointContainer";

		/// <summary>
		/// Cached name for the '_topBar' field.
		/// </summary>
		public static readonly StringName _topBar = "_topBar";
	}

	/// <summary>
	/// Cached StringNames for the signals contained in this class, for fast lookup.
	/// </summary>
	public new class SignalName : Control.SignalName
	{
	}

	private const float _pointSize = 35f;

	private const float _pointSpacing = 35f;

	private const float _neighborPadding = 25f;

	private readonly List<NBlitzTopBarMapPoint> _mapPoints = new List<NBlitzTopBarMapPoint>();

	private IRunState _runState;

	/// <summary>
	/// Marker which points to the current map point.
	/// </summary>
	private NMapMarker _marker;

	/// <summary>
	/// Container which is sized to exactly contain all the map points.
	/// This is repositioned to the center of the top bar, or moved around as needed to avoid running into other elements.
	/// </summary>
	private Control _container;

	/// <summary>
	/// Control which contains all the dotted paths.
	/// </summary>
	private Control _pathContainer;

	/// <summary>
	/// Control which contains all the map points.
	/// </summary>
	private Control _pointContainer;

	/// <summary>
	/// The top bar. Null until <see cref="M:MegaCrit.Sts2.Core.Nodes.CommonUi.NBlitzTopBarMap.Initialize(MegaCrit.Sts2.Core.Runs.IRunState,MegaCrit.Sts2.Core.Nodes.CommonUi.NTopBar)" />, which only Blitz runs call.
	/// </summary>
	private NTopBar? _topBar;

	public IReadOnlyList<NBlitzTopBarMapPoint> MapPoints => _mapPoints;

	public override void _Ready()
	{
		_container = GetNode<Control>("%BlitzMapContainer");
		_pathContainer = GetNode<Control>("%BlitzMapPaths");
		_pointContainer = GetNode<Control>("%BlitzMapPoints");
		_marker = GetNode<NMapMarker>("%BlitzMapMarker");
	}

	public void Initialize(IRunState runState, NTopBar topBar)
	{
		_runState = runState;
		_topBar = topBar;
		_topBar.LeftContainer.Connect(Container.SignalName.SortChildren, Callable.From(Reposition));
		_topBar.RightContainer.Connect(Container.SignalName.SortChildren, Callable.From(Reposition));
		_topBar.Timer.Connect(CanvasItem.SignalName.VisibilityChanged, Callable.From(Reposition));
		if (runState.GameMode != GameMode.Blitz)
		{
			throw new InvalidOperationException("Lightning top bar map should only used with Lightning mode!");
		}
		_marker.Initialize(LocalContext.GetMe(_runState));
		_marker.Invert();
		RunManager.Instance.MapChanged += OnMapChanged;
	}

	public override void _Notification(int what)
	{
		if ((long)what == 1)
		{
			RunManager.Instance.MapChanged -= OnMapChanged;
		}
		else if ((long)what == 40 && IsNodeReady())
		{
			TaskHelper.RunSafely(RepositionAfterOneFrame());
		}
	}

	private void OnMapChanged(bool clearDrawings)
	{
		SetMap(_runState.Map);
	}

	private void SetMap(ActMap actMap)
	{
		if (!(actMap is BlitzActMap blitzActMap))
		{
			throw new InvalidOperationException("Lightning top bar map should only used with Lightning mode!");
		}
		_mapPoints.Clear();
		foreach (Node child in _pathContainer.GetChildren())
		{
			child.QueueFreeSafely();
		}
		foreach (Node child2 in _pointContainer.GetChildren())
		{
			child2.QueueFreeSafely();
		}
		foreach (MapPoint mapPoint in blitzActMap.GetMapPoints())
		{
			NBlitzTopBarMapPoint nBlitzTopBarMapPoint = NBlitzTopBarMapPoint.Create(mapPoint, _runState);
			_pointContainer.AddChildSafely(nBlitzTopBarMapPoint);
			_mapPoints.Add(nBlitzTopBarMapPoint);
		}
		float x = (float)_mapPoints.Count * 35f + (float)(_mapPoints.Count - 1) * 35f;
		_container.Size = new Vector2(x, base.Size.Y);
		for (int i = 0; i < _mapPoints.Count; i++)
		{
			NBlitzTopBarMapPoint nBlitzTopBarMapPoint2 = _mapPoints[i];
			nBlitzTopBarMapPoint2.Position = (float)i * 70f * Vector2.Right + (base.Size.Y - nBlitzTopBarMapPoint2.Size.Y) * Vector2.Down * 0.5f;
			if (i > 0)
			{
				NBlitzTopBarMapPoint nBlitzTopBarMapPoint3 = _mapPoints[i - 1];
				NMapScreen.CreatePath(nBlitzTopBarMapPoint3.Position + nBlitzTopBarMapPoint3.Size * 0.5f, nBlitzTopBarMapPoint2.Position + nBlitzTopBarMapPoint2.Size * 0.5f, _pathContainer, StsColors.halfTransparentBlack);
			}
		}
		Reposition();
		IReadOnlyList<MapCoord> visitedMapCoords = _runState.VisitedMapCoords;
		MapCoord mapCoord;
		if (visitedMapCoords.Count <= 0)
		{
			mapCoord = blitzActMap.StartingMapPoint.coord;
		}
		else
		{
			mapCoord = visitedMapCoords[visitedMapCoords.Count - 1];
		}
		MapCoord currentCoord = mapCoord;
		NBlitzTopBarMapPoint nBlitzTopBarMapPoint4 = _mapPoints.FirstOrDefault((NBlitzTopBarMapPoint p) => p.Point.coord == currentCoord);
		if (nBlitzTopBarMapPoint4 != null)
		{
			SetMapNodeAsCurrent(nBlitzTopBarMapPoint4);
		}
	}

	public async Task RepositionAfterOneFrame()
	{
		await this.AwaitProcessFrame();
		Reposition();
	}

	private void Reposition()
	{
		if (_topBar == null)
		{
			return;
		}
		_container.Scale = Vector2.One;
		_container.Position = (base.Size - _container.Size) * 0.5f;
		float num = _topBar.LeftContainer.GlobalPosition.X + _topBar.LeftContainer.GetMinimumSize().X + 25f;
		if (!(num < _container.Position.X))
		{
			Control container = _container;
			Vector2 position = _container.Position;
			position.X = num;
			container.Position = position;
			float num2 = ((!_topBar.Timer.Visible) ? (_topBar.Deck.GlobalPosition.X - 25f) : (_topBar.Timer.GlobalPosition.X - 25f));
			if (!(_container.Position.X + _container.Size.X < num2))
			{
				_container.Scale = (num2 - num) / _container.Size.X * Vector2.One;
				Control container2 = _container;
				position = _container.Position;
				position.Y = (base.Size.Y - _container.Size.Y * _container.Scale.Y) * 0.5f;
				container2.Position = position;
			}
		}
	}

	public async Task TravelToMapCoord(MapCoord coord)
	{
		NBlitzTopBarMapPoint node = _mapPoints.FirstOrDefault((NBlitzTopBarMapPoint p) => p.Point.coord == coord);
		if (node != null)
		{
			SfxCmd.Play("event:/sfx/ui/map/map_select");
			SfxCmd.Play("event:/sfx/ui/wipe_map");
			SetMapNodeAsCurrent(node);
			node.ShowCircleVfx(playAnim: true);
		}
		await TaskHelper.RunSafely(RunManager.Instance.FadeOut());
		await RunManager.Instance.EnterMapCoord(coord);
		if (node != null && GodotObject.IsInstanceValid(node))
		{
			node.HideCircleVfx();
		}
		TaskHelper.RunSafely(RunManager.Instance.FadeIn());
	}

	private void SetMapNodeAsCurrent(NBlitzTopBarMapPoint node)
	{
		_marker.HideMapPoint();
		_marker.SetMapPoint(node);
		NMapNodeSelectVfx nMapNodeSelectVfx = NMapNodeSelectVfx.Create(1f);
		node.AddChildSafely(nMapNodeSelectVfx);
		nMapNodeSelectVfx.Position += node.PivotOffset;
	}

	/// <summary>
	/// Get the method information for all the methods declared in this class.
	/// This method is used by Godot to register the available methods in the editor.
	/// Do not call this method.
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<MethodInfo> GetGodotMethodList()
	{
		List<MethodInfo> list = new List<MethodInfo>(5);
		list.Add(new MethodInfo(MethodName._Ready, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null));
		list.Add(new MethodInfo(MethodName._Notification, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Int, "what", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		list.Add(new MethodInfo(MethodName.OnMapChanged, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Bool, "clearDrawings", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		list.Add(new MethodInfo(MethodName.Reposition, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null));
		list.Add(new MethodInfo(MethodName.SetMapNodeAsCurrent, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Object, "node", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("Control"), exported: false)
		}, null));
		return list;
	}

	/// <inheritdoc />
	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		if (method == MethodName._Ready && args.Count == 0)
		{
			_Ready();
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName._Notification && args.Count == 1)
		{
			_Notification(VariantUtils.ConvertTo<int>(in args[0]));
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.OnMapChanged && args.Count == 1)
		{
			OnMapChanged(VariantUtils.ConvertTo<bool>(in args[0]));
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.Reposition && args.Count == 0)
		{
			Reposition();
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.SetMapNodeAsCurrent && args.Count == 1)
		{
			SetMapNodeAsCurrent(VariantUtils.ConvertTo<NBlitzTopBarMapPoint>(in args[0]));
			ret = default(godot_variant);
			return true;
		}
		return base.InvokeGodotClassMethod(in method, args, out ret);
	}

	/// <inheritdoc />
	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool HasGodotClassMethod(in godot_string_name method)
	{
		if (method == MethodName._Ready)
		{
			return true;
		}
		if (method == MethodName._Notification)
		{
			return true;
		}
		if (method == MethodName.OnMapChanged)
		{
			return true;
		}
		if (method == MethodName.Reposition)
		{
			return true;
		}
		if (method == MethodName.SetMapNodeAsCurrent)
		{
			return true;
		}
		return base.HasGodotClassMethod(in method);
	}

	/// <inheritdoc />
	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
	{
		if (name == PropertyName._marker)
		{
			_marker = VariantUtils.ConvertTo<NMapMarker>(in value);
			return true;
		}
		if (name == PropertyName._container)
		{
			_container = VariantUtils.ConvertTo<Control>(in value);
			return true;
		}
		if (name == PropertyName._pathContainer)
		{
			_pathContainer = VariantUtils.ConvertTo<Control>(in value);
			return true;
		}
		if (name == PropertyName._pointContainer)
		{
			_pointContainer = VariantUtils.ConvertTo<Control>(in value);
			return true;
		}
		if (name == PropertyName._topBar)
		{
			_topBar = VariantUtils.ConvertTo<NTopBar>(in value);
			return true;
		}
		return base.SetGodotClassPropertyValue(in name, in value);
	}

	/// <inheritdoc />
	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool GetGodotClassPropertyValue(in godot_string_name name, out godot_variant value)
	{
		if (name == PropertyName._marker)
		{
			value = VariantUtils.CreateFrom(in _marker);
			return true;
		}
		if (name == PropertyName._container)
		{
			value = VariantUtils.CreateFrom(in _container);
			return true;
		}
		if (name == PropertyName._pathContainer)
		{
			value = VariantUtils.CreateFrom(in _pathContainer);
			return true;
		}
		if (name == PropertyName._pointContainer)
		{
			value = VariantUtils.CreateFrom(in _pointContainer);
			return true;
		}
		if (name == PropertyName._topBar)
		{
			value = VariantUtils.CreateFrom(in _topBar);
			return true;
		}
		return base.GetGodotClassPropertyValue(in name, out value);
	}

	/// <summary>
	/// Get the property information for all the properties declared in this class.
	/// This method is used by Godot to register the available properties in the editor.
	/// Do not call this method.
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<PropertyInfo> GetGodotPropertyList()
	{
		List<PropertyInfo> list = new List<PropertyInfo>();
		list.Add(new PropertyInfo(Variant.Type.Object, PropertyName._marker, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		list.Add(new PropertyInfo(Variant.Type.Object, PropertyName._container, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		list.Add(new PropertyInfo(Variant.Type.Object, PropertyName._pathContainer, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		list.Add(new PropertyInfo(Variant.Type.Object, PropertyName._pointContainer, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		list.Add(new PropertyInfo(Variant.Type.Object, PropertyName._topBar, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		return list;
	}

	/// <inheritdoc />
	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void SaveGodotObjectData(GodotSerializationInfo info)
	{
		base.SaveGodotObjectData(info);
		info.AddProperty(PropertyName._marker, Variant.From(in _marker));
		info.AddProperty(PropertyName._container, Variant.From(in _container));
		info.AddProperty(PropertyName._pathContainer, Variant.From(in _pathContainer));
		info.AddProperty(PropertyName._pointContainer, Variant.From(in _pointContainer));
		info.AddProperty(PropertyName._topBar, Variant.From(in _topBar));
	}

	/// <inheritdoc />
	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		base.RestoreGodotObjectData(info);
		if (info.TryGetProperty(PropertyName._marker, out var value))
		{
			_marker = value.As<NMapMarker>();
		}
		if (info.TryGetProperty(PropertyName._container, out var value2))
		{
			_container = value2.As<Control>();
		}
		if (info.TryGetProperty(PropertyName._pathContainer, out var value3))
		{
			_pathContainer = value3.As<Control>();
		}
		if (info.TryGetProperty(PropertyName._pointContainer, out var value4))
		{
			_pointContainer = value4.As<Control>();
		}
		if (info.TryGetProperty(PropertyName._topBar, out var value5))
		{
			_topBar = value5.As<NTopBar>();
		}
	}
}
