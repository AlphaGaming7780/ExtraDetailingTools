﻿using HarmonyLib;
using Game.SceneFlow;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using Game.UI;
using System.IO.Compression;
using Game.Common;
using Game;
using Game.Prefabs;
using System;
using Game.Net;
using System.Diagnostics;
using Colossal.OdinSerializer.Utilities;
using MonoMod.RuntimeDetour;
using Game.Tools;
using Game.UI.InGame;

namespace ExtraDetailingTools
{
	[HarmonyPatch(typeof(GameModeExtensions), "IsEditor")]
	public class GameModeExtensions_IsEditor
	{
		public static void Postfix(ref bool __result) {

			MethodBase caller = new StackFrame(2, false).GetMethod();
			if(
				(caller.DeclaringType == typeof(NetToolSystem) && caller.Name == "GetNetPrefab") ||
				(caller.DeclaringType == typeof(ObjectToolSystem) && caller.Name == "GetObjectPrefab") ||
				(caller.DeclaringType == typeof(DefaultToolSystem) && caller.Name == "InitializeRaycast")
            ) {
                    __result = true;
			}
		}
	}
}
