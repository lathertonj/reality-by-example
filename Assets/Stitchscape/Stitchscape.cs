// Stitchscape 2.1
// ©2017 Starscene Software. All rights reserved. Redistribution without permission not allowed.

using UnityEngine;

namespace Stitchscape {

public enum StitchDirection {Across, Down}

public class Stitch {
		
	public static void TerrainStitch (TerrainData terrain1, TerrainData terrain2, StitchDirection thisDirection, float stitchWidthPercent, float blendStrength, bool singleTerrain) {
		int terrainRes = terrain1.heightmapWidth;
		int terrainRes2 = terrain2.heightmapWidth;
		if (terrainRes != terrainRes2) {
			Debug.LogError ("TerrainStitch: terrain heightmap resolution must be the same for both terrains (terrain1 is " + terrainRes + " and terrain2 is " + terrainRes2 + ")");
			return;
		}
		if (terrain1.heightmapHeight != terrainRes || terrain2.heightmapHeight != terrainRes) {
			Debug.LogError ("TerrainStitch: heightmap width and height must be the same");
			return;
		}
		
		var heightmapData = terrain1.GetHeights (0, 0, terrainRes, terrainRes);
		var heightmapData2 = terrain2.GetHeights (0, 0, terrainRes, terrainRes);
		int width = terrainRes-1;
		
		int stitchWidth = (int)Mathf.Clamp (terrainRes * Mathf.Clamp01 (stitchWidthPercent), 2, (terrainRes-1)/2);
		blendStrength = Mathf.Clamp01 (blendStrength);
		
		if (thisDirection == StitchDirection.Across) {
			for (int i = 0; i < terrainRes; i++) {
				var midpoint = (heightmapData[i, width] + heightmapData2[i, 0]) * .5f;
				for (int j = 1; j < stitchWidth; j++) {
					var mix = Mathf.Lerp (heightmapData[i, width-j], heightmapData2[i, j], .5f);
					if (j == 1) {
						heightmapData[i, width] = Mathf.Lerp (mix, midpoint, blendStrength);
						heightmapData2[i, 0] = Mathf.Lerp (mix, midpoint, blendStrength);
					}
					var t = Mathf.SmoothStep (0.0f, 1.0f, Mathf.InverseLerp (1, stitchWidth-1, j));
					var mixdata = Mathf.Lerp (mix, heightmapData[i, width-j], t);
					heightmapData[i, width-j] = Mathf.Lerp (mixdata, Mathf.Lerp (midpoint, heightmapData[i, width-j], t), blendStrength);
										
					mixdata = Mathf.Lerp (mix, heightmapData2[i, j], t);
					var blend = Mathf.Lerp (mixdata, Mathf.Lerp (midpoint, heightmapData2[i, j], t), blendStrength);
					if (!singleTerrain) {
						heightmapData2[i, j] = blend;
					}
					else {
						heightmapData[i, j] = blend;
					}
				}
			}
			if (singleTerrain) {
				for (int i = 0; i < terrainRes; i++) {
					heightmapData[i, 0] = heightmapData[i, width];
				}
			}
		}
		else {
			for (int i = 0; i < terrainRes; i++) {
				var midpoint = (heightmapData2[width, i] + heightmapData[0, i]) * .5f;
				for (int j = 1; j < stitchWidth; j++) {
					var mix = Mathf.Lerp (heightmapData2[width-j, i], heightmapData[j, i], .5f);
					if (j == 1) {
						heightmapData2[width, i] = Mathf.Lerp (mix, midpoint, blendStrength);
						heightmapData[0, i] = Mathf.Lerp (mix, midpoint, blendStrength);
					}
					var t = Mathf.SmoothStep (0.0f, 1.0f, Mathf.InverseLerp (1, stitchWidth-1, j));
					var mixdata = Mathf.Lerp (mix, heightmapData[j, i], t);
					heightmapData[j, i] = Mathf.Lerp (mixdata, Mathf.Lerp (midpoint, heightmapData[j, i], t), blendStrength);
					
					mixdata = Mathf.Lerp (mix, heightmapData2[width-j, i], t);
					var blend = Mathf.Lerp (mixdata, Mathf.Lerp (midpoint, heightmapData2[width-j, i], t), blendStrength);
					if (!singleTerrain) {
						heightmapData2[width-j, i] = blend;
					}
					else {
						heightmapData[width-j, i] = blend;
					}
				}
			}
			if (singleTerrain) {
				for (int i = 0; i < terrainRes; i++) {
					heightmapData[width, i] = heightmapData[0, i];
				}
			}
		}
		
		terrain1.SetHeights (0, 0, heightmapData);
		if (!singleTerrain) {
			terrain2.SetHeights (0, 0, heightmapData2);
		}
	}
}
}