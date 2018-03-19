using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using System;
using System.IO;
using System.Runtime.CompilerServices;

public class STEPImporter : MonoBehaviour {

	public string stepFilePath;
	public bool debugStatements;
	public Material defaultMaterial;
	IntPtr vertexBuffer = IntPtr.Zero;
	IntPtr indexBuffer = IntPtr.Zero;
    Int32 vertexEntryCount = 0;
	Int32 indexCount = 0;
	
	[DllImport("STEPImport")]
	private static extern int ImportSTEPFile(IntPtr stepFilePath, ref IntPtr vertexBuffer, ref Int32 vertexEntryCount, ref IntPtr indexBuffer, ref Int32 indexCount);

	void Start () {
		
		System.Diagnostics.Stopwatch timer = (debugStatements) ? System.Diagnostics.Stopwatch.StartNew() : null;
		
		string formattedFilePath = stepFilePath.Replace("\\", "\\\\");
		
		// Call the C++ dll with the file name
		int returnCode = ImportSTEPFile(Marshal.StringToHGlobalAnsi(stepFilePath), ref vertexBuffer, ref vertexEntryCount, ref indexBuffer, ref indexCount);
		
		// TODO: Check the return code

		if (debugStatements) {
			Debug.Log("Time spent on Open Cascade geometry generation: " + timer.Elapsed.TotalSeconds + " seconds.");
			timer.Reset();
		}
		if (debugStatements) {
			Debug.Log("Time spent on transfering the data to managed C# float arrays: " + timer.Elapsed.TotalSeconds + " seconds.");
			Debug.Log("Number of vertices: " + vertexEntryCount / 3);
			Debug.Log("Number of triangles: " + indexCount / 3); 
			timer.Reset();
		}
			
		float[] managedVertexBuffer = new float[vertexEntryCount];
		int[] managedIndexBuffer = new int[indexCount];
		
		try {
			Marshal.Copy(vertexBuffer, managedVertexBuffer, 0, vertexEntryCount); 
			Marshal.Copy(indexBuffer, managedIndexBuffer, 0, indexCount); 
		}
		finally {
			Marshal.FreeHGlobal(vertexBuffer);
			Marshal.FreeHGlobal(indexBuffer);
		}        
		
		Vector3[] vertices = new Vector3[vertexEntryCount / 3];
		for (int i = 0, j = 0; j < vertexEntryCount; i++, j += 3)
			vertices[i] = new Vector3(managedVertexBuffer[j], managedVertexBuffer[j + 1], managedVertexBuffer[j + 2]);

		GameObject model = new GameObject(Path.GetFileName(stepFilePath));
		
		MeshFilter meshFilter = model.AddComponent<MeshFilter>();
		meshFilter.mesh.vertices = vertices;
		meshFilter.mesh.triangles = managedIndexBuffer;
		meshFilter.mesh.RecalculateNormals();

		MeshRenderer meshRenderer = model.AddComponent<MeshRenderer>();	
		meshRenderer.material = defaultMaterial;	
	}
}
