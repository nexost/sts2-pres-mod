using MegaCrit.Sts2.Core.Models.Powers;
using PresMod.Characters.Biden.Cards;
using PresMod.Characters.Biden.Mechanics;

namespace PresMod.Characters.Biden.Potions;

/// <summary>Common: Doze 8.</summary>
public sealed class WarmMilk : PotionModel
{
	public override PotionRarity Rarity => PotionRarity.Common;

	public override PotionUsage Usage => PotionUsage.CombatOnly;

	public override TargetType TargetType => TargetType.Self;

	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DynamicVar(CardVars.Doze, 8m) };

	public override IEnumerable<IHoverTip> ExtraHoverTips => new[] { BidenHoverTips.Doze, BidenHoverTips.Drowsy };

	protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
	{
		BidenVfx.Steam(Owner.Creature);
		await DrowsyCmd.Doze(choiceContext, Owner.Creature, DynamicVars[CardVars.Doze].BaseValue);
	}
}

/// <summary>Uncommon: Wake Up and draw 2.</summary>
public sealed class EspressoShot : PotionModel
{
	public override PotionRarity Rarity => PotionRarity.Uncommon;

	public override PotionUsage Usage => PotionUsage.CombatOnly;

	public override TargetType TargetType => TargetType.Self;

	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new CardsVar(2) };

	public override IEnumerable<IHoverTip> ExtraHoverTips => new[] { BidenHoverTips.WakeUp, BidenHoverTips.DarkBrandon };

	protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
	{
		await BidenVfx.Jitter(Owner.Creature);
		await DrowsyCmd.WakeUp(choiceContext, Owner.Creature);
		await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
	}
}

/// <summary>Rare: Wake Up; this turn, Attacks deal double damage.</summary>
public sealed class DarkRoast : PotionModel
{
	public override PotionRarity Rarity => PotionRarity.Rare;

	public override PotionUsage Usage => PotionUsage.CombatOnly;

	public override TargetType TargetType => TargetType.Self;

	public override IEnumerable<IHoverTip> ExtraHoverTips => new[] { BidenHoverTips.WakeUp, HoverTipFactory.FromPower<DoubleDamagePower>() };

	protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
	{
		BidenVfx.RedFlash();
		await DrowsyCmd.WakeUp(choiceContext, Owner.Creature);
		await PowerCmd.Apply<DoubleDamagePower>(choiceContext, Owner.Creature, 1m, Owner.Creature, null);
	}
}
