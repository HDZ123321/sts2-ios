using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using Watcher.Code.Stances.Vfx;
using Watcher.Code.Vfx;

namespace Watcher.Code.Stances;

public class StanceVfxController(StanceVfxConfig cfg)
{
	private const float AmbienceFadeTime = 0.6f;

	private const float AmbienceVolume = -6f;

	private static Color? _originalModulate;

	private static AudioStreamPlayer? _ambiencePlayer;

	private Node2D? _vfxInstance;

	public async Task OnEnter(Creature owner)
	{
		await CreateAura(owner);
		ApplyBodyTint(owner);
		PlayEnterSfx();
		StartAmbience();
		if (LocalContext.IsMe(owner))
		{
			PlayScreenFlash();
			PlayScreenShake();
		}
	}

	public async Task OnExit(Creature owner)
	{
		RemoveAura();
		ResetBodyTint(owner);
		StopAmbience();
		await Task.CompletedTask;
	}

	private Task CreateAura(Creature owner)
	{
		if (cfg.AuraScenePath == null)
		{
			return Task.CompletedTask;
		}
		NCreatureVisuals nCreatureVisuals = NCombatRoom.Instance?.GetCreatureNode(owner)?.Visuals;
		if (nCreatureVisuals == null)
		{
			return Task.CompletedTask;
		}
		Node2D obj = nCreatureVisuals.GetNodeOrNull<Node2D>("StanceVfxContainer") ?? CreateContainer(nCreatureVisuals);
		if (_vfxInstance != null && GodotObject.IsInstanceValid(_vfxInstance))
		{
			_vfxInstance.QueueFree();
		}
		_vfxInstance = PreloadManager.Cache.GetScene(cfg.AuraScenePath).Instantiate<Node2D>(PackedScene.GenEditState.Disabled);
		_vfxInstance.Position = Vector2.Zero;
		_vfxInstance.Scale = Vector2.One;
		obj.AddChild(_vfxInstance, forceReadableName: false, Node.InternalMode.Disabled);
		foreach (Node2D item in (from c in _vfxInstance.GetChildren()
			where c.Name.ToString().Contains("Burst")
			select c).Cast<Node2D>())
		{
			Vector2 globalPosition = item.GlobalPosition;
			item.Reparent(nCreatureVisuals);
			item.GlobalPosition = globalPosition;
			nCreatureVisuals.MoveChild(item, 0);
		}
		return Task.CompletedTask;
	}

	private static Node2D CreateContainer(Node visuals)
	{
		Node2D node2D = new Node2D
		{
			Name = "StanceVfxContainer",
			Position = Vector2.Zero
		};
		visuals.AddChild(node2D, forceReadableName: false, Node.InternalMode.Disabled);
		return node2D;
	}

	private void RemoveAura()
	{
		if (_vfxInstance == null || !GodotObject.IsInstanceValid(_vfxInstance))
		{
			return;
		}
		foreach (Node child in _vfxInstance.GetChildren())
		{
			if (!(child is WrathGlowSparkSpawner wrathGlowSparkSpawner))
			{
				if (!(child is CalmFrostStreakSpawner calmFrostStreakSpawner))
				{
					if (!(child is DivinityEyeSpawner divinityEyeSpawner))
					{
						AuraBlobEmitter auraBlobEmitter = child as AuraBlobEmitter;
						if (auraBlobEmitter == null)
						{
							continue;
						}
						foreach (CpuParticles2D item in auraBlobEmitter.GetChildren().OfType<CpuParticles2D>())
						{
							item.Emitting = false;
						}
						SceneTree? tree = auraBlobEmitter.GetTree();
						if (tree == null)
						{
							continue;
						}
						tree.CreateTimer(2.5).Timeout += delegate
						{
							if (GodotObject.IsInstanceValid(auraBlobEmitter))
							{
								auraBlobEmitter.QueueFree();
							}
						};
					}
					else
					{
						divinityEyeSpawner.StopSpawning();
					}
				}
				else
				{
					calmFrostStreakSpawner.StopSpawning();
				}
			}
			else
			{
				wrathGlowSparkSpawner.StopSpawning();
			}
		}
		_vfxInstance = null;
	}

	private void ApplyBodyTint(Creature owner)
	{
		if (!cfg.BodyTint.HasValue)
		{
			return;
		}
		Node2D node2D = NCombatRoom.Instance?.GetCreatureNode(owner)?.Body;
		if (node2D != null)
		{
			Color valueOrDefault = _originalModulate.GetValueOrDefault();
			if (!_originalModulate.HasValue)
			{
				valueOrDefault = node2D.Modulate;
				_originalModulate = valueOrDefault;
			}
			node2D.Modulate = cfg.BodyTint.Value;
		}
	}

	private void ResetBodyTint(Creature owner)
	{
		if (_originalModulate.HasValue)
		{
			Node2D node2D = NCombatRoom.Instance?.GetCreatureNode(owner)?.Body;
			if (node2D != null)
			{
				node2D.Modulate = _originalModulate.Value;
				_originalModulate = null;
			}
		}
	}

	private void PlayEnterSfx()
	{
		try
		{
			if (cfg.EnterSfxPath != null)
			{
				SfxCmd.Play(cfg.EnterSfxPath);
			}
		}
		catch (Exception ex)
		{
			WatcherMainFile.Logger.Warn(ex.ToString());
		}
	}

	private void PlayScreenFlash()
	{
		if (cfg.ScreenFlashColor.HasValue)
		{
			ScreenFlashEffect.Play(cfg.ScreenFlashColor.Value);
		}
	}

	private void PlayScreenShake()
	{
		if (cfg.ScreenShakeStrength != ShakeStrength.None)
		{
			NGame.Instance?.ScreenShake(cfg.ScreenShakeStrength, ShakeDuration.Short);
		}
	}

	private void StartAmbience()
	{
		if (cfg.AmbienceLoopPath != null)
		{
			if (_ambiencePlayer != null && GodotObject.IsInstanceValid(_ambiencePlayer))
			{
				_ambiencePlayer.QueueFree();
			}
			NCombatRoom instance = NCombatRoom.Instance;
			if (instance != null)
			{
				_ambiencePlayer = new AudioStreamPlayer
				{
					Stream = PreloadManager.Cache.GetAsset<AudioStream>(cfg.AmbienceLoopPath),
					Bus = "SFX",
					VolumeDb = -80f
				};
				instance.AddChild(_ambiencePlayer, forceReadableName: false, Node.InternalMode.Disabled);
				_ambiencePlayer.Play();
				_ambiencePlayer.CreateTween().TweenProperty(_ambiencePlayer, "volume_db", -6f, 0.6000000238418579);
			}
		}
	}

	private static void StopAmbience()
	{
		if (_ambiencePlayer == null || !GodotObject.IsInstanceValid(_ambiencePlayer))
		{
			return;
		}
		AudioStreamPlayer player = _ambiencePlayer;
		_ambiencePlayer = null;
		Tween tween = player.CreateTween();
		tween.TweenProperty(player, "volume_db", -80f, 0.6000000238418579);
		tween.TweenCallback(Callable.From(delegate
		{
			if (GodotObject.IsInstanceValid(player))
			{
				player.QueueFree();
			}
		}));
	}
}
