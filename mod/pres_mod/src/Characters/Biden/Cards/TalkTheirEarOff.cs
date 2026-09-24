using MegaCrit.Sts2.Core.Models.Powers;
using PresMod.Characters.Biden.Mechanics;
using PresMod.Characters.Biden.Powers;

namespace PresMod.Characters.Biden.Cards;

/// <summary>4 damage per Tangent card in hand.</summary>
public sealed class TalkTheirEarOff : CardModel
{
	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
	{
		new CalculationBaseVar(0m),
		new ExtraDamageVar(4m),
		new CalculatedDamageVar(ValueProp.Move).WithMultiplier((CardModel card, Creature? _) => PileType.Hand.GetPile(card.Owner).Cards.Count(c => c is TangentCard))
	};

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { BidenHoverTips.Tangent };

	public TalkTheirEarOff()
		: base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
		await DamageCmd.Attack(DynamicVars.CalculatedDamage).FromCard(this).Targeting(cardPlay.Target)
			.WithHitFx("vfx/vfx_attack_blunt")
			.Execute(choiceContext);
	}

	protected override void OnUpgrade()
	{
		DynamicVars.ExtraDamage.UpgradeValueBy(1m);
	}
}
