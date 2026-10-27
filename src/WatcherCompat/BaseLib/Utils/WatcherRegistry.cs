using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Utils;

/// <summary>
/// Answers "which cards belong to this pool" without reflection.
/// <para>
/// BaseLib found pool members by scanning every loaded assembly, which the
/// NativeAOT build cannot do.  Baked in, the membership is known at build time,
/// so <c>WatcherRegistry.Generated.cs</c> spells it out with one generic call per
/// model and this class just groups them.
/// </para>
/// </summary>
public static partial class WatcherRegistry
{
	private static readonly Dictionary<Type, List<AbstractModel>> _byPool = new Dictionary<Type, List<AbstractModel>>();

	private static bool _populated;

	public static IEnumerable<CardModel> CardsFor(Type poolType)
	{
		return ItemsFor<CardModel>(poolType);
	}

	public static IEnumerable<RelicModel> RelicsFor(Type poolType)
	{
		return ItemsFor<RelicModel>(poolType);
	}

	public static IEnumerable<PotionModel> PotionsFor(Type poolType)
	{
		return ItemsFor<PotionModel>(poolType);
	}

	private static IEnumerable<T> ItemsFor<T>(Type poolType) where T : AbstractModel
	{
		EnsurePopulated();
		if (!_byPool.TryGetValue(poolType, out List<AbstractModel>? items))
		{
			yield break;
		}
		foreach (AbstractModel item in items)
		{
			if (item is T typed)
			{
				yield return typed;
			}
		}
	}

	private static void EnsurePopulated()
	{
		if (_populated)
		{
			return;
		}
		_populated = true;
		RegisterAll();
	}

	private static void Add<TModel, TPool>() where TModel : AbstractModel
	{
		ModelDb.Inject(typeof(TModel));
		TModel model = ModelDb.GetById<TModel>(ModelDb.GetId<TModel>());
		Type poolType = typeof(TPool);
		if (!_byPool.TryGetValue(poolType, out List<AbstractModel>? items))
		{
			items = new List<AbstractModel>();
			_byPool[poolType] = items;
		}
		items.Add(model);
	}

	private static void AddCard<TModel, TPool>() where TModel : CardModel
	{
		Add<TModel, TPool>();
	}
}
