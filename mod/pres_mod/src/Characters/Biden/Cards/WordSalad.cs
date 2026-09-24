using MegaCrit.Sts2.Core.Models.Powers;
using PresMod.Characters.Biden.Mechanics;

namespace PresMod.Characters.Biden.Cards;

/// <summary>Tangent, 2 cost: (1) a big hit, (2) everything twice, (3) Block.</summary>
public sealed class WordSalad : TangentCard
{
	public override bool GainsBlock => true;

	public override int LineCount => 3;

	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
	{
		new DamageVar(14m, ValueProp.Move),
		new DamageVar("AllDamage", 6m, ValueProp.Move),
		new BlockVar(10m, ValueProp.Move)
	};

	protected override IEnumerable<IHoverTip> ExtraHoverTips => base.ExtraHoverTips.Append(HoverTipFactory.Static(StaticHoverTip.Block));

	public WordSalad()
		: base(2, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
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
					.WithHitFx("vfx/vfx_attack_slash")
					.Execute(choiceContext);
				break;
			case 3:
				await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
				break;
		}
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Damage.UpgradeValueBy(4m);
		DynamicVars["AllDamage"].UpgradeValueBy(2m);
		DynamicVars.Block.UpgradeValueBy(3m);
	}
}
