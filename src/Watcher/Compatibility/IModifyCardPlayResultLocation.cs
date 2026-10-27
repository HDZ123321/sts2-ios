using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace Watcher.Code.Compatibility;

public interface IModifyCardPlayResultLocation
{
	CardLocationCompatiblity ModifyCardPlayResultLocationCompability(CardModel card, bool isAutoPlay, ResourceInfo resources, CardLocationCompatiblity cardLocation)
	{
		return cardLocation;
	}

	Task AfterModifyingCardPlayResultLocationCompability(CardModel card, CardLocationCompatiblity cardLocation)
	{
		return Task.CompletedTask;
	}
}
