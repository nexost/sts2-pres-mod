using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using PresMod.Characters.{{Class}}.Cards;
using PresMod.Characters.{{Class}}.Pools;
using PresMod.Characters.{{Class}}.Relics;
using PresMod.Framework;

namespace PresMod.Characters.{{Class}};

/// <summary>
/// The playable character. Id entry is {{ID}}, so the game looks for assets named "{{id}}"
/// (scenes/creature_visuals/{{id}}.tscn, images/ui/top_panel/character_icon_{{id}}.png, ...).
/// Stats and deck from the design (characters/{{id}}/design/design.md §1).
/// </summary>
public sealed class {{Class}} : CharacterModel, IModCharacter
{
	public const string energyColorName = "{{id}}";

	/// <summary>Borrow a base character's FMOD events until we have our own sounds.</summary>
	public string SfxProxyEntry => "{{sfx}}";

	public override CharacterGender Gender => CharacterGender.{{Gender}};

	protected override CharacterModel? UnlocksAfterRunAs => null;

	public override Color NameColor => new Color("{{primary}}");

	public override int StartingHp => {{hp}};

	public override int StartingGold => {{gold}};

	public override CardPoolModel CardPool => ModelDb.CardPool<{{Class}}CardPool>();

	public override PotionPoolModel PotionPool => ModelDb.PotionPool<{{Class}}PotionPool>();

	public override RelicPoolModel RelicPool => ModelDb.RelicPool<{{Class}}RelicPool>();

	public override IEnumerable<CardModel> StartingDeck => new CardModel[]
	{
		ModelDb.Card<Strike{{Class}}>(),
		ModelDb.Card<Strike{{Class}}>(),
		ModelDb.Card<Strike{{Class}}>(),
		ModelDb.Card<Strike{{Class}}>(),
		ModelDb.Card<Strike{{Class}}>(),
		ModelDb.Card<Defend{{Class}}>(),
		ModelDb.Card<Defend{{Class}}>(),
		ModelDb.Card<Defend{{Class}}>(),
		ModelDb.Card<Defend{{Class}}>(),
		ModelDb.Card<Defend{{Class}}>()
	};

	public override IReadOnlyList<RelicModel> StartingRelics => new RelicModel[] { ModelDb.Relic<{{Starter}}>() };

	public override float AttackAnimDelay => 0.15f;

	public override float CastAnimDelay => 0.25f;

	public override Color EnergyLabelOutlineColor => new Color("{{dark}}FF");

	public override Color DialogueColor => new Color("{{dialogue}}");

	public override VfxColor SpeechBubbleColor => VfxColor.{{VfxColor}};

	public override Color MapDrawingColor => new Color("{{map}}");

	public override Color RemoteTargetingLineColor => new Color("{{primary}}FF");

	public override Color RemoteTargetingLineOutline => new Color("{{dark}}FF");

	public override string CharacterSelectSfx => "event:/sfx/characters/{{sfx}}/{{sfx}}_select";

	public override string CharacterTransitionSfx => "event:/sfx/ui/wipe_ironclad";

	public override List<string> GetArchitectAttackVfx()
	{
		return new List<string> { "vfx/vfx_attack_blunt", "vfx/vfx_heavy_blunt", "vfx/vfx_attack_slash" };
	}
}
