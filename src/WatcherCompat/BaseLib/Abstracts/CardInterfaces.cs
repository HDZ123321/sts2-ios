using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Abstracts;

/// <summary>Marks a basic card that a higher rarity card transforms into.</summary>
public interface ITranscendenceCard
{
	CardModel GetTranscendenceTransformedCard();
}
