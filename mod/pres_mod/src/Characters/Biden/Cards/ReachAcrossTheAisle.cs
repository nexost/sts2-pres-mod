using MegaCrit.Sts2.Core.Models.Powers;
using PresMod.Characters.Biden.Mechanics;
using PresMod.Characters.Biden.Powers;

namespace PresMod.Characters.Biden.Cards;

/// <summary>Co-op only: every player gains Energy and draws 1.</summary>
public sealed class ReachAcrossTheAisle : CardModel
{
	public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

	public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };

	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new EnergyVar(1), new CardsVar(1) };

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { HoverTipFactory.ForEnergy(this) };

	public ReachAcrossTheAisle()
		: base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AllAllies)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		BidenVfx.ReachAcross(Owner.Creature, CombatState!.Players.Select(p => p.Creature));
		foreach (Player player in CombatState!.Players)
		{
			await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, player);
			await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, player);
		}
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Energy.UpgradeValueBy(1m);
	}
}
