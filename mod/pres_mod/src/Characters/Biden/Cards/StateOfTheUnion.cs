using MegaCrit.Sts2.Core.Models.Powers;
using PresMod.Characters.Biden.Mechanics;
using PresMod.Characters.Biden.Powers;

namespace PresMod.Characters.Biden.Cards;

/// <summary>Ancient: Wake Up, then hit everything hard.</summary>
public sealed class StateOfTheUnion : CardModel
{
	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DamageVar(14m, ValueProp.Move) };

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { BidenHoverTips.WakeUp, BidenHoverTips.LaserEyes };

	public StateOfTheUnion()
		: base(1, CardType.Attack, CardRarity.Ancient, TargetType.AllEnemies)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await DrowsyCmd.WakeUp(choiceContext, Owner.Creature);
		if (CombatState!.HittableEnemies.Count == 0)
		{
			return;
		}
		await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).TargetingAllOpponents(CombatState)
			.WithHitFx("vfx/vfx_giant_horizontal_slash")
			.Execute(choiceContext);
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Damage.UpgradeValueBy(6m);
	}
}
