using MegaCrit.Sts2.Core.Models.Powers;
using PresMod.Characters.Biden.Mechanics;
using PresMod.Characters.Biden.Powers;

namespace PresMod.Characters.Biden.Cards;

/// <summary>Wake Up; Attacks deal double damage this turn (the game's Double Damage). Exhaust.</summary>
public sealed class Unleashed : CardModel
{
	public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { BidenHoverTips.WakeUp, HoverTipFactory.FromPower<DoubleDamagePower>() };

	public Unleashed()
		: base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await DrowsyCmd.WakeUp(choiceContext, Owner.Creature);
		await PowerCmd.Apply<DoubleDamagePower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this);
	}

	protected override void OnUpgrade()
	{
		EnergyCost.UpgradeBy(-1);
	}
}
