using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.ValueProps;
using PresMod.Framework.Patches;

namespace PresMod.Characters.{{Class}}.Relics;

/// <summary>Starter (stub from scripts/new_character.py): start each combat with 6 Block. Replace with the designed starter.</summary>
public sealed class {{Starter}} : RelicModel, IUpgradableStarterRelic
{
	public override RelicRarity Rarity => RelicRarity.Starter;

	/// <summary>What Touch of Orobas turns it into. Circlet until the character has its own Ancient version.</summary>
	public RelicModel AncientUpgrade => ModelDb.Relic<Circlet>();

	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new BlockVar(6m, ValueProp.Unpowered) };

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { HoverTipFactory.Static(StaticHoverTip.Block) };

	public override async Task BeforeCombatStart()
	{
		Flash();
		await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, null);
	}
}
