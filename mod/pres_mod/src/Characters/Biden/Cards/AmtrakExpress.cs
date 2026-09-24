using MegaCrit.Sts2.Core.Models.Powers;
using PresMod.Characters.Biden.Mechanics;
using PresMod.Characters.Biden.Powers;

namespace PresMod.Characters.Biden.Cards;

/// <summary>3 damage for each card played this turn, this one included.</summary>
public sealed class AmtrakExpress : CardModel
{
	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
	{
		new CalculationBaseVar(0m),
		new ExtraDamageVar(3m),
		new CalculatedDamageVar(ValueProp.Move).WithMultiplier((CardModel card, Creature? _) => card.CardsPlayedThisTurn() + 1)
	};

	public AmtrakExpress()
		: base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
		await DamageCmd.Attack(DynamicVars.CalculatedDamage).FromCard(this).Targeting(cardPlay.Target)
			.WithHitFx("vfx/vfx_heavy_blunt", null, "heavy_attack.mp3")
			.Execute(choiceContext);
	}

	protected override void OnUpgrade()
	{
		DynamicVars.ExtraDamage.UpgradeValueBy(1m);
	}
}
