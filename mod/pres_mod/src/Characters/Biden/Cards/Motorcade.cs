using MegaCrit.Sts2.Core.Models.Powers;
using PresMod.Characters.Biden.Mechanics;
using PresMod.Characters.Biden.Powers;

namespace PresMod.Characters.Biden.Cards;

/// <summary>3 cost: everything, and Block.</summary>
public sealed class Motorcade : CardModel
{
	public override bool GainsBlock => true;

	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DamageVar(14m, ValueProp.Move), new BlockVar(10m, ValueProp.Move) };

	public Motorcade()
		: base(3, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await BidenVfx.Vehicle(Owner.Creature, "limo", CombatState!.HittableEnemies.LastOrDefault(), size: 0.55f, seconds: 1.1f, copies: 3);
		await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).TargetingAllOpponents(CombatState!)
			.WithHitFx("vfx/vfx_giant_horizontal_slash")
			.Execute(choiceContext);
		await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Damage.UpgradeValueBy(4m);
		DynamicVars.Block.UpgradeValueBy(4m);
	}
}
