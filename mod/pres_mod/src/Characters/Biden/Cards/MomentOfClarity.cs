using MegaCrit.Sts2.Core.Models.Powers;
using PresMod.Characters.Biden.Mechanics;
using PresMod.Characters.Biden.Powers;

namespace PresMod.Characters.Biden.Cards;

/// <summary>This turn, Tangent cards do all their lines. Exhausts until upgraded.</summary>
public sealed class MomentOfClarity : CardModel
{
	public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { BidenHoverTips.Tangent };

	public MomentOfClarity()
		: base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await PowerCmd.Apply<MomentOfClarityPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this);
	}

	protected override void OnUpgrade()
	{
		RemoveKeyword(CardKeyword.Exhaust);
	}
}
