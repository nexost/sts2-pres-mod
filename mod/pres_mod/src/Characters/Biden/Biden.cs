using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using PresMod.Characters.Biden.Cards;
using PresMod.Characters.Biden.Pools;
using PresMod.Characters.Biden.Relics;
using PresMod.Framework;

namespace PresMod.Characters.Biden;

/// <summary>
/// The playable character. Id entry is BIDEN, so the game looks for assets named "biden"
/// (scenes/creature_visuals/biden.tscn, images/ui/top_panel/character_icon_biden.png, ...).
/// Stats and deck from the design (characters/biden/design/design.md §1): 70 HP, 99 Gold, Aviator Shades.
/// </summary>
public sealed class Biden : CharacterModel, IModCharacter
{
	public const string energyColorName = "biden";

	/// <summary>Borrow a base character's FMOD events until we have our own sounds.</summary>
	public string SfxProxyEntry => "ironclad";

	public override CharacterGender Gender => CharacterGender.Masculine;

	protected override CharacterModel? UnlocksAfterRunAs => null;

	public override Color NameColor => new Color("4A55E6");

	public override int StartingHp => 70;

	public override int StartingGold => 99;

	public override CardPoolModel CardPool => ModelDb.CardPool<BidenCardPool>();

	public override PotionPoolModel PotionPool => ModelDb.PotionPool<BidenPotionPool>();

	public override RelicPoolModel RelicPool => ModelDb.RelicPool<BidenRelicPool>();

	public override IEnumerable<CardModel> StartingDeck => new CardModel[]
	{
		ModelDb.Card<StrikeBiden>(),
		ModelDb.Card<StrikeBiden>(),
		ModelDb.Card<StrikeBiden>(),
		ModelDb.Card<StrikeBiden>(),
		ModelDb.Card<DefendBiden>(),
		ModelDb.Card<DefendBiden>(),
		ModelDb.Card<DefendBiden>(),
		ModelDb.Card<DefendBiden>(),
		ModelDb.Card<Catnap>(),
		ModelDb.Card<HeresTheDeal>()
	};

	public override IReadOnlyList<RelicModel> StartingRelics => new RelicModel[] { ModelDb.Relic<AviatorShades>() };

	public override float AttackAnimDelay => 0.15f;

	public override float CastAnimDelay => 0.25f;

	public override Color EnergyLabelOutlineColor => new Color("252A73FF");

	public override Color DialogueColor => new Color("121437");

	public override VfxColor SpeechBubbleColor => VfxColor.Blue;

	public override Color MapDrawingColor => new Color("3B44B8");

	public override Color RemoteTargetingLineColor => new Color("4A55E6FF");

	public override Color RemoteTargetingLineOutline => new Color("252A73FF");

	public override string CharacterSelectSfx => "event:/sfx/characters/ironclad/ironclad_select";

	public override string CharacterTransitionSfx => "event:/sfx/ui/wipe_ironclad";

	public override List<string> GetArchitectAttackVfx()
	{
		return new List<string> { "vfx/vfx_attack_blunt", "vfx/vfx_heavy_blunt", "vfx/vfx_attack_slash" };
	}
}
