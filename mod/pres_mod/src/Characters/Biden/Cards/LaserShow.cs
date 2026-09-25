using MegaCrit.Sts2.Core.Models.Powers;
using PresMod.Characters.Biden.Mechanics;
using PresMod.Characters.Biden.Powers;

namespace PresMod.Characters.Biden.Cards;

/// <summary>X cost: Wake Up, then hit everything X times.</summary>
public sealed class LaserShow : CardModel
{
	protected override bool HasEnergyCostX => true;

	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DamageVar(7m, ValueProp.Move) };

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { BidenHoverTips.WakeUp, BidenHoverTips.LaserEyes };

	public LaserShow()
		: base(0, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		int hits = ResolveEnergyXValue();
		await DrowsyCmd.WakeUp(choiceContext, Owner.Creature);
		if (hits <= 0 || CombatState!.HittableEnemies.Count == 0)
		{
			return;
		}
		await BidenVfx.LaserShow(Owner.Creature, CombatState.HittableEnemies.ToList());
		await DamageCmd.Attack(DynamicVars.Damage.BaseValue).WithHitCount(hits).FromCard(this).TargetingAllOpponents(CombatState)
			.WithHitFx("vfx/vfx_attack_slash")
			.Execute(choiceContext);
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Damage.UpgradeValueBy(2m);
	}
}
