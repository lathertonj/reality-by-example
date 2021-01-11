// Stitchscape 2.1
// ©2017 Starscene Software. All rights reserved. Redistribution without permission not allowed.

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Stitchscape {

public class StitchscapeEditor : ScriptableWizard {
	
	static int across;
	static int down;
	static int tWidth;
	static int tHeight;
	static Terrain[] terrains;
	static float stitchWidthPercent;
	static int stitchWidth;
	static string message;
	static int terrainRes;
	static Texture2D lineTex;
	static float strength;
	static bool playError = false;
	static int gridPixelHeight = 28;
	static int gridPixelWidth = 121;
	static StitchscapeEditor window;
	
	[MenuItem ("GameObject/Stitch Terrains... &#t")]
	static void CreateWizard () {
		if (lineTex == null) {	// across/down etc. defined here, so closing and re-opening wizard doesn't reset vars
			across = down = tWidth = tHeight = 2;
			stitchWidthPercent = .1f;
			strength = .5f;
			SetNumberOfTerrains();
			lineTex = EditorGUIUtility.whiteTexture;
		}
		message = "";
		playError = false;
		window = ScriptableWizard.DisplayWizard ("Stitch Terrains", typeof(StitchscapeEditor)) as StitchscapeEditor;
		window.minSize = new Vector2(270, 245);
	}
	
	void OnGUI () {
		if (Application.isPlaying) {
			playError = true;
		}
		if (playError) {	// Needs to continue showing this even if play mode is stopped
			GUI.Label (new Rect(5, 5, 250, 16), "Stitchscape can't run in play mode");
			return;
		}
		
		GUI.Label (new Rect(5, 5, 160, 16), "Number of terrains across:");
		across = Mathf.Max (EditorGUI.IntField (new Rect(170, 5, 30, 16), across), 1);
		GUI.Label (new Rect(5, 25, 160, 16), "Number of terrains down:");
		down = Mathf.Max (EditorGUI.IntField (new Rect(170, 25, 30, 16), down), 1);
		if (GUI.Button (new Rect(210, 14, 50, 18), "Apply")) {
			tWidth = across;
			tHeight = down;
			SetNumberOfTerrains();
		}
		
		if (GUI.Button (new Rect(16, 52, gridPixelWidth*tWidth + 1, 18), "Autofill from scene") ) {
			AutoFill();
		}
		
		var counter = 0;
		for (var h = 0; h < tHeight; h++) {
			for (var w = 0; w < tWidth; w++) {
				terrains[counter] = EditorGUI.ObjectField (new Rect(20 + w*gridPixelWidth, 82 + h*gridPixelHeight, 112, 16), terrains[counter++], typeof(Terrain), true) as Terrain;
			}
		}
		DrawGrid (Color.black, 1, 75);
		DrawGrid (Color.white, 0, 75);
		
		GUI.Label (new Rect(2, 71, 20, 20), "Z");
		GUI.Label (new Rect(gridPixelWidth*tWidth + 10, 77 + gridPixelHeight*tHeight, 20, 20), "X");
		GUI.color = Color.black;
		GUI.DrawTexture (new Rect(7, 87, 1, gridPixelHeight*tHeight - 2), lineTex);
		GUI.DrawTexture (new Rect(7, 85 + gridPixelHeight*tHeight, gridPixelWidth*tWidth, 1), lineTex);
		GUI.color = Color.white;
		
		GUI.Label (new Rect(5, 95 + gridPixelHeight*tHeight, 115, 16), "Stitch width %: " + (stitchWidthPercent * 100).ToString("f0"));
		stitchWidthPercent = GUI.HorizontalSlider (new Rect(120, 95 + gridPixelHeight*tHeight, window.position.width - 127, 16), stitchWidthPercent, .01f, .5f);
		
		GUI.Label (new Rect(5, 111 + gridPixelHeight*tHeight, 115, 16), "Blend strength: " + (strength * 100).ToString("f0"));
		strength = GUI.HorizontalSlider (new Rect(120, 111 + gridPixelHeight*tHeight, window.position.width - 127, 16), strength, 0.0f, 1.0f);
		
		GUI.Label (new Rect(5, 136 + gridPixelHeight*tHeight, window.position.width - 10, 20), message);
		
		var buttonWidth = window.position.width/2 - 10;
		if (GUI.Button (new Rect(7, 156 + gridPixelHeight*tHeight, buttonWidth, 18), "Clear")) {
			SetNumberOfTerrains();
		}
		if (GUI.Button (new Rect(12 + buttonWidth, 156 + gridPixelHeight*tHeight, buttonWidth, 18), "Stitch")) {
			StitchTerrains();
		}
	}
	
	static void AutoFill () {
		var sceneTerrains = FindObjectsOfType (typeof(Terrain)) as Terrain[];
		if (sceneTerrains.Length == 0) {
			message = "No terrains found";
			return;
		}
		
		var xPositions = new List<float>();
		var zPositions = new List<float>();
		var tPosition = sceneTerrains[0].transform.position;
		xPositions.Add (tPosition.x);
		zPositions.Add (tPosition.z);
		for (var i = 0; i < sceneTerrains.Length; i++) {
			tPosition = sceneTerrains[i].transform.position;
			if (!ListContains(xPositions, tPosition.x)) {
				xPositions.Add (tPosition.x);
			}
			if (!ListContains(zPositions, tPosition.z)) {
				zPositions.Add (tPosition.z);
			}
		}
		if (xPositions.Count * zPositions.Count != sceneTerrains.Length) {
			message = "Unable to autofill. Terrains should line up closely in the form of a grid.";
			return;
		}
		
		xPositions.Sort();
		zPositions.Sort();
		zPositions.Reverse();
		across = tWidth = xPositions.Count;
		down = tHeight = zPositions.Count;
		terrains = new Terrain[tWidth * tHeight];
		var count = 0;
		for (var z = 0; z < zPositions.Count; z++) {
			for (var x = 0; x < xPositions.Count; x++) {
				for (var i = 0; i < sceneTerrains.Length; i++) {
					tPosition = sceneTerrains[i].transform.position;
					if (Approx(tPosition.x, xPositions[x]) && Approx(tPosition.z, zPositions[z])) {
						terrains[count++] = sceneTerrains[i];
						break;
					}
				}
			}
		}
		message = "";
	}
	
	static bool ListContains (List<float> list, float pos) {
		for (var i = 0; i < list.Count; i++) {
			if (Approx (pos, list[i])) {
				return true;
			}
		}
		return false;
	}

	static bool Approx (float pos1, float pos2) {
		return (pos1 >= pos2-1.0f && pos1 <= pos2+1.0f);
	}
	
	static void DrawGrid (Color color, int offset, int top) {
		GUI.color = color;
		for (var i = 0; i < tHeight+1; i++) {
			GUI.DrawTexture (new Rect(15 + offset, top + offset + gridPixelHeight*i, gridPixelWidth*tWidth, 1), lineTex);
		}
		for (var i = 0; i < tWidth+1; i++) {
			GUI.DrawTexture (new Rect(15 + offset + gridPixelWidth*i, top + offset, 1, gridPixelHeight*tHeight + 1), lineTex);		
		}
	}
	
	static void SetNumberOfTerrains () {
		terrains = new Terrain[tWidth * tHeight];
		message = "";
	}
	
	static void StitchTerrains () {
		foreach (var t in terrains) {
			if (t == null) {
				message = "All terrain slots must have a terrain assigned";
				return;
			}
		}
		
		terrainRes = terrains[0].terrainData.heightmapResolution;
		if (terrains[0].terrainData.heightmapResolution != terrainRes) {
			message = "Heightmap width and height must be the same";
			return;
		}
		
		foreach (var t in terrains) {
			if (t.terrainData.heightmapResolution != terrainRes || t.terrainData.heightmapResolution != terrainRes) {
				message = "All heightmaps must be the same resolution";
				return;
			}
		}
		
		foreach (var t in terrains) {
#if UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3_OR_NEWER
			Undo.RegisterCompleteObjectUndo (t.terrainData, "Stitch Terrains");
#else
			Undo.RegisterUndo (t.terrainData, "Stitch Terrains");
#endif
		}
		
		var counter = 0;
		var total = tHeight*(tWidth-1) + (tHeight-1)*tWidth;
		
		if (tWidth == 1 && tHeight == 1) {
			Stitch.TerrainStitch (terrains[0].terrainData, terrains[0].terrainData, StitchDirection.Across, stitchWidthPercent, strength, true);
			Stitch.TerrainStitch (terrains[0].terrainData, terrains[0].terrainData, StitchDirection.Down, stitchWidthPercent, strength, true);
			message = "Terrain has been made repeatable with itself";
		}
		else {
			for (var h = 0; h < tHeight; h++) {
				for (var w = 0; w < tWidth-1; w++) {
					EditorUtility.DisplayProgressBar ("Stitching...", "", Mathf.InverseLerp (0, total, ++counter));
					Stitch.TerrainStitch (terrains[h*tWidth + w].terrainData, terrains[h*tWidth + w + 1].terrainData, StitchDirection.Across, stitchWidthPercent, strength, false);
				}
			}
			for (var h = 0; h < tHeight-1; h++) {
				for (var w = 0; w < tWidth; w++) {
					EditorUtility.DisplayProgressBar ("Stitching...", "", Mathf.InverseLerp (0, total, ++counter));
					Stitch.TerrainStitch (terrains[h*tWidth + w].terrainData, terrains[(h+1)*tWidth + w].terrainData, StitchDirection.Down, stitchWidthPercent, strength, false);
				}
			}
			message = "Terrains stitched successfully";
		}
		
		EditorUtility.ClearProgressBar();
	}
}
}