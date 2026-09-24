using MegaCrit.Sts2.Core.Models.Powers;
using PresMod.Characters.Biden.Mechanics;
using PresMod.Characters.Biden.Powers;

namespace PresMod.Characters.Biden.Cards;

/// <summary>Tangent, 2 cost: (1) a huge hit, (2) everything twice, (3) big Block.</summary>
public sealed class RambleOn : TangentCard
{
	public override bool GainsBlock => true;

	public override int LineCount => 3;

	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
	{
		new DamageVar(20m, ValueProp.Move),
		new DamageVar("AllDamage", 8m, ValueProp.Move),
		new BlockVar(15m, ValueProp.Move)
	};

	protected override IEnumerable<IHoverTip> ExtraHoverTips => base.ExtraHoverTips.Append(HoverTipFactory.Static(StaticHoverTip.Block));

	public RambleOn()
		: base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
	{
	}

	protected override async Task PlayLine(int line, PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		switch (line)
		{
			case 1:
				ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
				await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
					.WithHitFx("vfx/vfx_heavy_blunt", null, "heavy_attack.mp3")
					.Execute(choiceContext);
				break;
			case 2:
				await DamageCmd.Attack(DynamicVars["AllDamage"].BaseValue).WithHitCount(2).FromCard(this).TargetingAllOpponents(CombatState!)
					.WithHitFx("vfx/vfx_giant_horizontal_slash")
					.Execute(choiceContext);
				break;
			case 3:
				await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
				break;
		}
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Damage.UpgradeValueBy(6m);
		DynamicVars["AllDamage"].UpgradeValueBy(2m);
		DynamicVars.Block.UpgradeValueBy(5m);
	}
}
