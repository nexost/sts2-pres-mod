using MegaCrit.Sts2.Core.Models.Powers;
using PresMod.Characters.Biden.Mechanics;
using PresMod.Characters.Biden.Powers;

namespace PresMod.Characters.Biden.Cards;

/// <summary>A big hit that costs 1 less for each other card played this turn.</summary>
public sealed class ZeroToSixty : CardModel
{
	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DamageVar(20m, ValueProp.Move) };

	public ZeroToSixty()
		: base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
	{
	}

	public override bool TryModifyEnergyCostInCombatLate(CardModel card, decimal originalCost, out decimal modifiedCost)
	{
		modifiedCost = originalCost;
		if (card != this || CombatState == null)
		{
			return false;
		}
		int played = this.CardsPlayedThisTurn();
		if (played <= 0)
		{
			return false;
		}
		modifiedCost = Math.Max(0m, originalCost - played);
		return true;
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
		await BidenVfx.Vehicle(Owner.Creature, "corvette", cardPlay.Target, size: 0.5f, seconds: 0.55f, fromHim: true);
		await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
			.WithHitFx("vfx/vfx_heavy_blunt", null, "heavy_attack.mp3")
			.Execute(choiceContext);
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Damage.UpgradeValueBy(6m);
	}
}
