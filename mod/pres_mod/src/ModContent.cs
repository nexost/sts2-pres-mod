using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Models;

namespace PresMod;

/// <summary>Lookups for the models this mod adds.</summary>
public static class ModContent
{
	private static CharacterModel[]? _characters;

	/// <summary>Our playable characters as registered in ModelDb (empty until ModelDb.Init has run).</summary>
	public static IReadOnlyList<CharacterModel> Characters
	{
		get
		{
			if (_characters != null)
			{
				return _characters;
			}
			CharacterModel[] found = typeof(ModContent).Assembly.GetTypes()
				.Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(CharacterModel)))
				.Select(t => ModelDb.GetByIdOrNull<CharacterModel>(ModelDb.GetId(t)))
				.OfType<CharacterModel>()
				.ToArray();
			// Don't cache an empty result: ModelDb may simply not be initialized yet.
			if (found.Length > 0)
			{
				_characters = found;
			}
			return found;
		}
	}

	public static bool IsModCharacter(CharacterModel? character)
	{
		return character != null && character.GetType().Assembly == typeof(ModContent).Assembly;
	}
}
