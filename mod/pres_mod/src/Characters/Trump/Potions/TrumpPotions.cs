using PresMod.Characters.Trump.Cards;
using PresMod.Framework.Patches;
using PresMod.Characters.Trump.Patches;

using PresMod.Characters.Trump.Mechanics;

namespace PresMod.Characters.Trump.Potions;

/// <summary>Common: Build 12.</summary>
public sealed class QuickDryConcrete : PotionModel
{
	public override PotionRarity Rarity => PotionRarity.Common;

	public override PotionUsage Usage => PotionUsage.CombatOnly;

	public override TargetType TargetType => TargetType.AnyPlayer;

	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DynamicVar(CardVars.Build, 12m) };

	public override IEnumerable<IHoverTip> ExtraHoverTips => new[] { TrumpHoverTips.Build, TrumpHoverTips.Wall };

	protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
	{
		PotionModel.AssertValidForTargetedPotion(target);
		await WallCmd.Build(choiceContext, target!, DynamicVars[CardVars.Build].BaseValue);
	}
}

/// <summary>Uncommon: add 3 Upgraded Tweets into your Hand.</summary>
public sealed class CovfefePotion : PotionModel
{
	public override PotionRarity Rarity => PotionRarity.Uncommon;

	public override PotionUsage Usage => PotionUsage.CombatOnly;

	public override TargetType TargetType => TargetType.Self;

	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new CardsVar(3) };

	public override IEnumerable<IHoverTip> ExtraHoverTips => new[] { TrumpHoverTips.Tweet };

	protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
	{
		ICombatState? combat = Owner.Creature.CombatState;
		if (combat != null)
		{
			await Tweet.CreateInHand(Owner, DynamicVars.Cards.IntValue, combat, upgraded: true);
		}
	}
}

/// <summary>Rare: Deport an enemy, whatever its HP. Immune bosses can't be targeted (TargetFilterPatch).</summary>
public sealed class DeportationDraught : PotionModel, ITargetFilter
{
	public override PotionRarity Rarity => PotionRarity.Rare;

	public override PotionUsage Usage => PotionUsage.CombatOnly;

	public override TargetType TargetType => TargetType.AnyEnemy;

	public override IEnumerable<IHoverTip> ExtraHoverTips => new[] { TrumpHoverTips.Deport };

	public bool CanTarget(Creature target) => !DeportCmd.IsImmune(target);

	protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
	{
		PotionModel.AssertValidForTargetedPotion(target);
		await DeportCmd.ForceDeport(choiceContext, target!, Owner);
	}
}
