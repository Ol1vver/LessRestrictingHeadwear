﻿using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;

namespace LessRestrictingHeadwear;

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 1)]
public class LessRestrictingHeadwear : IOnLoad
{
	public readonly LessRestrictingHeadwearConfig Config;
	private readonly ISptLogger<LessRestrictingHeadwear> Logger;
	private readonly DatabaseService DatabaseService;
	private readonly ItemHelper ItemHelper;

	public LessRestrictingHeadwear(ISptLogger<LessRestrictingHeadwear> logger, ModHelper modHelper, JsonUtil jsonUtil, DatabaseService databaseService, ItemHelper itemHelper)
	{
		string modPath = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
		string configPath = System.IO.Path.Combine(modPath, "config.jsonc");
		if (!File.Exists(configPath))
		{
			logger.Info($"[LessRestrictingHeadwear] Config file not found, creating");
			using FileStream file = new(configPath, FileMode.Create, FileAccess.Write);
			file.Write(Encoding.ASCII.GetBytes(jsonUtil.Serialize(new LessRestrictingHeadwearConfig()
			{
				Debug = false,
				FaceShieldItemIDs = [
					"5c0e842486f77443a74d2976",
					"5f60c85b58eff926626a60f7",
					"5b46238386f7741a693bcf9c",
					"5ca2113f86f7740b2547e1d2",
					"5d6d3829a4b9361bc8618943",
					"5aa7e3abe5b5b000171d064d",
					"65719f9ef392ad76c50a2ec8",
					"5a16b7e1fcdbcb00165aa6c9",
					"658188edf026a90c1708c827",
					"65818e4e566d2de69901b1b1",
					"5e00cdd986f7747473332240",
					"5ac4c50d5acfc40019262e87",
					"5e01f37686f774773c6f6c15",
					"5c0919b50db834001b7ce3b9",
					"5aa7e373e5b5b000137b76f0",
					"5a16ba61fcdbcb098008728a",
					"5f60c076f2bcbb675b00dac2",
					"6570a88c8f221f3b210353b7"
				],
				FaceShieldItemSettings = [false, true, true, true],
				ItemSettings = new()
				{
					/* 
					 * Structure:
					 * Item Type ->	HEADWEAR	(Headwear)
					 *				HEADPHONES	(Earpiece)
					 *				FACE_COVER	(FaceCover)
					 *				VISORS		(Eyewear)
					 * A "true" value will remove the block. 
					 * A "false" value will keep value.
					 * 
					 * Config contains the MongoId of the class instead of a human-readable name.
					*/
					{ BaseClasses.HEADWEAR,     [ false, true, true, true ] },
					{ BaseClasses.HEADPHONES,   [ true, false, true, true ] },
					{ BaseClasses.FACE_COVER,   [ true, true, false, true ] },
					{ BaseClasses.VISORS,       [ true, true, true, false ] }
				}
			}, true) ?? ""));
		}

		Config = modHelper.GetJsonDataFromFile<LessRestrictingHeadwearConfig>(modPath, "config.jsonc");
		Logger = logger;
		DatabaseService = databaseService;
		ItemHelper = itemHelper;
	}

	Task IOnLoad.OnLoad()
	{
		int patchedCount = 0;
		foreach (KeyValuePair<MongoId, TemplateItem> item in DatabaseService.GetItems())
		{
			bool applyPatch = Config.FaceShieldItemIDs.Contains(item.Key);
			bool[] settings = Config.FaceShieldItemSettings;
			if (!applyPatch)
				foreach (MongoId baseClass in Config.ItemSettings.Keys)
					if (ItemHelper.IsOfBaseclass(item.Key, baseClass))
					{
						applyPatch = true;
						settings = Config.ItemSettings[baseClass];
						break;
					}

			if (applyPatch)
			{
				TemplateItemProperties? properties = item.Value.Properties;
				if (properties == null)
				{
					if (Config.Debug) Logger.Warning($"[LessRestrictingHeadwear] Properties of item {item.Key} are missing");
					continue;
				}

				properties.BlocksHeadwear = !settings[0] && properties.BlocksHeadwear.GetValueOrDefault(false);
				properties.BlocksEarpiece = !settings[1] && properties.BlocksEarpiece.GetValueOrDefault(false);
				properties.BlocksFaceCover = !settings[2] && properties.BlocksFaceCover.GetValueOrDefault(false);
				properties.BlocksEyewear = !settings[3] && properties.BlocksEyewear.GetValueOrDefault(false);
				properties.ConflictingItems = [];
				patchedCount++;
				if (Config.Debug) Logger.Info($"[LessRestrictingHeadwear] Patched item #{patchedCount} {ItemHelper.GetItemName(item.Key)} ({item.Key})");
			}
		}

		Logger.Success($"[LessRestrictingHeadwear] Patched {patchedCount} items!");
		return Task.CompletedTask;
	}
}

public record LessRestrictingHeadwearConfig
{
	[JsonPropertyName("debug")]
	public required bool Debug { get; set; }

	[JsonPropertyName("faceShieldItemIDs")]
	public required MongoId[] FaceShieldItemIDs { get; set; }

	[JsonPropertyName("faceShieldItemSettings")]
	public required bool[] FaceShieldItemSettings { get; set; }

	[JsonPropertyName("itemSettings")]
	public required Dictionary<MongoId, bool[]> ItemSettings { get; set; }
}

public record LessRestrictingHeadwearModMetadata : AbstractModMetadata
{
	public override string ModGuid { get; init; } = "com.musicmaniac.lessrestrictingheadwear";
	public override string Name { get; init; } = "LessRestrictingHeadwear";
	public override string Author { get; init; } = "MusicManiac";
	public override List<string>? Contributors { get; init; } = ["olv"];
	public override SemanticVersioning.Version Version { get; init; } = new("2.4.0");
	public override SemanticVersioning.Range SptVersion { get; init; } = new("~4.0.0");


	public override List<string>? Incompatibilities { get; init; } = [];
	public override Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; } = [];
	public override string? Url { get; init; } = "https://forge.sp-tarkov.com/mod/922/less-restricting-headwear";
	public override bool? IsBundleMod { get; init; } = false;
	public override string License { get; init; } = "MIT";
}
