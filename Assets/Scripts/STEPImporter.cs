using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using System;
using System.IO;
using System.Threading;
using System.Runtime.CompilerServices;
using System.Linq;

public class STEPImporter : MonoBehaviour {
	
	[DllImport("STEPImport")]
	private static extern int ImportSTEPFile(IntPtr stepFilePath, ref Int32 numberOfSubShapes);
	[DllImport("STEPImport")]
	private static extern int ProcessSubShape(Int32 subshapeIndex, ref IntPtr vertexBuffer, ref Int32 vertexEntryCount, ref IntPtr indexBuffer, ref Int32 indexCount);
	[DllImport("STEPImport")]
	private static extern int ClearStepModelData();

	/* Unity arguments */
	public string stepFilePath;
	public bool debugStatements;
	public Material defaultMaterial;
	private GameObject rootObject;

	/* Subshape variables */
	private Int32 totalNumberOfSubShapes = 0;
	
	/* Color pallette variables */
	private Color[] defaultColorPalette;
	private const float MAX_RGB_VALUE = 255f;

	/* Debug variables */
	private uint totalVertexCount = 0;
	private uint totalTriangleCount = 0;

	private void GetSubShape(object subShapesToProcess) {

		int[] subShapeIndexes = subShapesToProcess as int[];
		IntPtr vertexBuffer = IntPtr.Zero, indexBuffer = IntPtr.Zero;
		Int32 vertexEntryCount = 0, indexCount = 0;

		foreach (int index in subShapeIndexes) {
			ProcessSubShape(index, ref vertexBuffer, ref vertexEntryCount, ref indexBuffer, ref indexCount);

			float[] managedVertexBuffer = new float[vertexEntryCount];
			int[] managedIndexBuffer = new int[indexCount];
			
			Marshal.Copy(vertexBuffer, managedVertexBuffer, 0, vertexEntryCount); 
			Marshal.Copy(indexBuffer, managedIndexBuffer, 0, indexCount); 
			Marshal.FreeHGlobal(vertexBuffer);
			Marshal.FreeHGlobal(indexBuffer);  
			
			Vector3[] vertices = new Vector3[vertexEntryCount / 3];
			for (int i = 0, j = 0; j < vertexEntryCount; i++, j += 3)
				vertices[i] = new Vector3(managedVertexBuffer[j], managedVertexBuffer[j + 1], managedVertexBuffer[j + 2]);

			GameObject subobject = new GameObject(Path.GetFileName(stepFilePath) + index);
			subobject.transform.parent = rootObject.transform;
			subobject.transform.localScale = new Vector3(0.001f, 0.001f, 0.001f); //Default unit for Open Cascade is millimeters, while it is meters for Unity.
			
			MeshFilter meshFilter = subobject.AddComponent<MeshFilter>();
			meshFilter.mesh.vertices = vertices;
			meshFilter.mesh.triangles = managedIndexBuffer;
			meshFilter.mesh.RecalculateNormals();

			MeshRenderer meshRenderer = subobject.AddComponent<MeshRenderer>();	
			meshRenderer.material = defaultMaterial;	
			meshRenderer.material.color = defaultColorPalette[index % defaultColorPalette.Length];

			totalVertexCount += (uint) vertices.Length;
			totalTriangleCount += (uint) (managedIndexBuffer.Length / 3);
		}
	}

	void Start () {
		
		PopulateColorPalettes();
		System.Diagnostics.Stopwatch timer = (debugStatements) ? System.Diagnostics.Stopwatch.StartNew() : null;	
		string formattedFilePath = stepFilePath.Replace("\\", "\\\\");
		
		int returnCode = ImportSTEPFile(Marshal.StringToHGlobalAnsi(stepFilePath), ref totalNumberOfSubShapes);

		if (totalNumberOfSubShapes <= 0)
			return;

		rootObject = new GameObject(Path.GetFileName(stepFilePath));
		
		int nextShapeIndexToProcess = 0;
		int threadCount = Environment.ProcessorCount;
		int shapesPerThread = (totalNumberOfSubShapes / threadCount);
		int leftOverShapes = (totalNumberOfSubShapes % threadCount);

		Thread[] availableThreads = new Thread[Environment.ProcessorCount];
		availableThreads[0] = new Thread(GetSubShape);
		availableThreads[0].Start(Enumerable.Range(nextShapeIndexToProcess, (shapesPerThread + leftOverShapes)).ToArray());
		nextShapeIndexToProcess += (shapesPerThread + leftOverShapes);

		for (int i = 1; i < availableThreads.Length; i++) {
			//ThreadStart threadDelegate = new ThreadStart(GetSubShape);
			availableThreads[i] = new Thread(GetSubShape);
			availableThreads[i].Start(Enumerable.Range(nextShapeIndexToProcess, shapesPerThread).ToArray());
			nextShapeIndexToProcess += shapesPerThread;
		}

		for (int i = 0; i < availableThreads.Length; i++)
			availableThreads[i].Join();

		ClearStepModelData();
		
		if (debugStatements) {
			Debug.Log(
				"Number of failed translations: " + returnCode +
				"\nTotal time spent on Open Cascade geometry generation: " + timer.Elapsed.ToString() +
				"\nTotal number of vertices: " + totalVertexCount.ToString() +
				"\nTotal number of triangles: " + totalTriangleCount.ToString()
			); 
			// Debug.Log("Number of failed translations: " + returnCode);
			// Debug.Log("Total time spent on Open Cascade geometry generation: " + timer.Elapsed.ToString());
			// Debug.Log("Total number of vertices: " + totalVertexCount.ToString());
			// Debug.Log("Total number of triangles: " + totalTriangleCount.ToString()); 
		}		
	} 

	private void PopulateColorPalettes()
    {

        defaultColorPalette = new Color[17];
        int i = 0;
        // "50 shades of grey"
		defaultColorPalette[i++] = new Color(146f / MAX_RGB_VALUE, 158f / MAX_RGB_VALUE, 148f / MAX_RGB_VALUE, 1f); // Middle grey (#929E94)
        defaultColorPalette[i++] = new Color(130f / MAX_RGB_VALUE, 147f / MAX_RGB_VALUE, 153f / MAX_RGB_VALUE, 1f); // dark grey (#829399)
        defaultColorPalette[i++] = new Color(88f / MAX_RGB_VALUE, 88f / MAX_RGB_VALUE, 88f / MAX_RGB_VALUE, 1f); // dark-grey (#585858)
		defaultColorPalette[i++] = new Color(66f / MAX_RGB_VALUE, 75f / MAX_RGB_VALUE, 84f / MAX_RGB_VALUE, 1f); // black-grey (#424B54)
		
        // Blues 
        defaultColorPalette[i++] = new Color(157f / MAX_RGB_VALUE, 217f / MAX_RGB_VALUE, 210f / MAX_RGB_VALUE, 1f); // Pale blue (#9DD9D2)
        defaultColorPalette[i++] = new Color(64f / MAX_RGB_VALUE, 121f / MAX_RGB_VALUE, 140f / MAX_RGB_VALUE, 1f); // Sea blue (#40798C)
		defaultColorPalette[i++] = new Color(111f / MAX_RGB_VALUE, 145f / MAX_RGB_VALUE, 159f / MAX_RGB_VALUE, 1f); // Mist blue (#6F919F)

        // Greens
        defaultColorPalette[i++] = new Color(214f / MAX_RGB_VALUE, 246f / MAX_RGB_VALUE, 221f / MAX_RGB_VALUE, 1f); // Pistachio green (#D6F6DD)
        defaultColorPalette[i++] = new Color(182 / MAX_RGB_VALUE, 215f / MAX_RGB_VALUE, 185f / MAX_RGB_VALUE, 1f); // Light eagreen (#B6D7B9) 
		defaultColorPalette[i++] = new Color(85 / MAX_RGB_VALUE, 144f / MAX_RGB_VALUE, 146f / MAX_RGB_VALUE, 1f); // Seagreen (#559092)
        defaultColorPalette[i++] = new Color(192f / MAX_RGB_VALUE, 214f / MAX_RGB_VALUE, 132f / MAX_RGB_VALUE, 1f); // Spring green (#C0D684)
        defaultColorPalette[i++] = new Color(125f / MAX_RGB_VALUE, 207f / MAX_RGB_VALUE, 182f / MAX_RGB_VALUE, 1f); // green (#7DCFB6)
		defaultColorPalette[i++] = new Color(212f / MAX_RGB_VALUE, 218f / MAX_RGB_VALUE, 179f / MAX_RGB_VALUE, 1f); // Beige green (#D4DAB3)
		defaultColorPalette[i++] = new Color(136f / MAX_RGB_VALUE, 159f / MAX_RGB_VALUE, 137f / MAX_RGB_VALUE, 1f); // Military green (#889F89)
		defaultColorPalette[i++] = new Color(78f / MAX_RGB_VALUE, 107f / MAX_RGB_VALUE, 83f / MAX_RGB_VALUE, 1f); // Dark green (#4E6B53)
		
        // Yellows
        defaultColorPalette[i++] = new Color(255f / MAX_RGB_VALUE, 244f / MAX_RGB_VALUE, 145f / MAX_RGB_VALUE, 1f); // Pale yellow (#FFF491)
		defaultColorPalette[i++] = new Color(202f / MAX_RGB_VALUE, 193f / MAX_RGB_VALUE, 110f / MAX_RGB_VALUE, 1f); // Dark yellow (#CAC16E)
    }
}
