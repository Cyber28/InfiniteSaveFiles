using BepInEx;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace InfiniteSaveFilesNS
{
	[BepInPlugin("InfiniteSaveFiles", "InfiniteSaveFiles", "1.0.1")]
	public class InfiniteSaveFiles : BaseUnityPlugin
	{
		public static BepInEx.Logging.ManualLogSource L;
		private void Awake()
		{
			L = Logger;
			Harmony.CreateAndPatchAll(typeof(Patches));
		}
	}

	public class Patches
	{
		[HarmonyPatch(typeof(WorldManager), "GetAllSaves")]
		[HarmonyPrefix]
		public static bool WMGAS(ref List<SaveGame> __result)
		{
			var allSaves = new List<SaveGame>();
			foreach (var file in new DirectoryInfo(Application.persistentDataPath).GetFiles())
			{
				if (file.Extension == ".sav")
				{
					InfiniteSaveFiles.L.LogInfo($"found save file: {file.Name} | id: {file.Name.Split('.')[0].Split('_')[1]}");
					var json = File.ReadAllText(file.FullName);
					var saveId = file.Name.Split('.')[0].Split('_')[1]; // please dont castigate me for this
					if (!string.IsNullOrEmpty(json))
						allSaves.Add(SaveGame.LoadFromString(json, saveId));
				}
			}
			if (allSaves.Count == 0)
				allSaves.Add(new SaveGame() { SaveId = "0" });
			__result = allSaves;
			return false;
		}

		[HarmonyPatch(typeof(SelectSaveScreen), "Start")]
		[HarmonyPrefix]
		public static bool SSSStart(SelectSaveScreen __instance)
		{
			__instance.BackButton.Clicked += (Action)(() => GameCanvas.instance.SetScreen(GameCanvas.instance.OptionsScreen));
			List<SaveGame> allSaves = WorldManager.instance.GetAllSaves();
			foreach (var save in allSaves)
			{
				CustomButton customButton = UnityEngine.Object.Instantiate<CustomButton>(PrefabManager.instance.ButtonPrefab);
				customButton.transform.SetParent(__instance.ButtonsParent);
				customButton.transform.localScale = Vector3.one;
				customButton.transform.localPosition = Vector3.zero;
				customButton.transform.localRotation = Quaternion.identity;
				customButton.TextMeshPro.text = WorldManager.instance.GetSaveSummary(save);
				customButton.Clicked += (() =>
				{
					WorldManager.instance.Save(save);
					TransitionScreen.instance.StartTransition((Action)(() => WorldManager.RestartGame()));
				});
			}
			CustomButton newSave = UnityEngine.Object.Instantiate<CustomButton>(PrefabManager.instance.ButtonPrefab);
			newSave.transform.SetParent(__instance.ButtonsParent);
			//allSaves.Sort((SaveGame x, SaveGame y) => Int32.Parse(x.SaveId) - Int32.Parse(x.SaveId)); // WHY CANT YOU CHAIN THESEEE
			// ^ you forgor to compare x to y and not to y, lol
			// Also, wdym "chain these"?
			var saveIndex = 0;
			var tempIndex;
			allSaves.ForEach(allSave =>
				{
					if (Int32.TryParse(allSave.SaveId, out int tempIndex) && tempIndex > saveIndex) saveIndex = tempIndex;
				});
			saveIndex++;// 0-indexed, so has to be incremented again for displaying
			InfiniteSaveFiles.L.LogInfo($"last save id: {saveIndex}");
			newSave.TextMeshPro.text = SokLoc.Translate("label_start_new_save", LocParam.Create("save_index", (saveIndex + 1).ToString()));
			newSave.transform.localScale = Vector3.one;
			newSave.transform.localPosition = Vector3.zero;
			newSave.transform.localRotation = Quaternion.identity;
			newSave.Clicked += (() =>
			{
				WorldManager.instance.Save(new SaveGame() { SaveId = saveIndex.ToString() });
				TransitionScreen.instance.StartTransition((Action)(() => WorldManager.RestartGame()));
			});
			return false;
		}
	}
}
