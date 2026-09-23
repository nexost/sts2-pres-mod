using Godot;
using MegaCrit.Sts2.Core.Models;
using TrumpMod.Models.Cards;
using TrumpMod.Models.Cards.Placeholders;

namespace TrumpMod.Models.CardPools;

public sealed class TrumpCardPool : CardPoolModel
{
	/// <summary>Also the portrait folder: images/packed/card_portraits/trump/&lt;card&gt;.png</summary>
	public override string Title => "trump";

	public override string EnergyColorName => Characters.Trump.energyColorName;

	/// <summary>res://materials/cards/frames/card_frame_trump_mat.tres (a hue shift of the shared frame).</summary>
	public override string CardFrameMaterialPath => "card_frame_trump";

	public override Color DeckEntryCardColor => new Color("D9A21B");

	public override Color EnergyOutlineColor => new Color("7A5200");

	public override bool IsColorless => false;

	protected override CardModel[] GenerateAllCards()
	{
		return new CardModel[]
		{
			ModelDb.Card<StrikeTrump>(),
			ModelDb.Card<DefendTrump>(),
			ModelDb.Card<BuildTheWall>(),
			ModelDb.Card<Deport>(),
			ModelDb.Card<SlapATariff>(),
			ModelDb.Card<MeanTweet>(),
			ModelDb.Card<SmallLoan>(),
			ModelDb.Card<PlaceholderCommonAttackA>(),
			ModelDb.Card<PlaceholderCommonAttackB>(),
			ModelDb.Card<PlaceholderCommonSkillA>(),
			ModelDb.Card<PlaceholderCommonSkillB>(),
			ModelDb.Card<PlaceholderCommonPower>(),
			ModelDb.Card<PlaceholderUncommonAttackA>(),
			ModelDb.Card<PlaceholderUncommonAttackB>(),
			ModelDb.Card<PlaceholderUncommonSkillA>(),
			ModelDb.Card<PlaceholderUncommonSkillB>(),
			ModelDb.Card<PlaceholderUncommonPower>(),
			ModelDb.Card<PlaceholderRareAttackA>(),
			ModelDb.Card<PlaceholderRareAttackB>(),
			ModelDb.Card<PlaceholderRareSkillA>(),
			ModelDb.Card<PlaceholderRareSkillB>(),
			ModelDb.Card<PlaceholderRarePower>()
		};
	}
}
