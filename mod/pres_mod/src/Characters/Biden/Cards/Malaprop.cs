using MegaCrit.Sts2.Core.Models.Powers;
using PresMod.Characters.Biden.Mechanics;
using PresMod.Characters.Biden.Powers;

namespace PresMod.Characters.Biden.Cards;

/// <summary>Tangent: (1) everything, (2) big Block.</summary>
public sealed class Malaprop : TangentCard
{
	public override bool GainsBlock => true;

	public override int LineCount => 2;

	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DamageVar(10m, ValueProp.Move), new BlockVar(14m, ValueProp.Move) };

	protected override IEnumerable<IHoverTip> ExtraHoverTips => base.ExtraHoverTips.Append(HoverTipFactory.Static(StaticHoverTip.Block));

	public Malaprop()
		: base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
	{
	}

	protected override async Task PlayLine(int line, PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (line == 1)
		{
			await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).TargetingAllOpponents(CombatState!)
				.WithHitFx("vfx/vfx_giant_horizontal_slash")
				.Execute(choiceContext);
			return;
		}
		await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Damage.UpgradeValueBy(3m);
		DynamicVars.Block.UpgradeValueBy(4m);
	}
}
