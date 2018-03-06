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
	IntPtr dataBufferPtr = IntPtr.Zero;
    Int32 vertixCount = 0;
	Int32 normalCount = 0;
	Int32 uv2Count = 0;
	Int32 triangleCount = 0;
	
	[DllImport("STEPImporter")]
	private static extern void Get3DGeometry(string filepath, ref IntPtr dataBufferPtr, ref Int32 vertixCount, ref Int32 normalCount, ref Int32 uv2Count, ref Int32 triangleCount);


	void Start () {
		System.Diagnostics.Stopwatch timer = (debugStatements) ? System.Diagnostics.Stopwatch.StartNew() : null;
		
		// Call the C++ dll with the file name
		Get3DGeometry(stepFilePath, ref dataBufferPtr, ref vertixCount, ref normalCount, ref uv2Count, ref triangleCount);
		
		if (debugStatements) {
			Debug.Log("Time spent on Open Cascade geometry generation: " + timer.Elapsed.TotalSeconds + " seconds.");
			timer.Reset();
		}
			
		float[] vertices = new float[vertixCount];
		float[] normals = new float[normalCount];
		float[] uv2s = new float[uv2Count];
		float[] triangles = new float[triangleCount];
		
		// Possible might want: (int)Math.Min(dataBufLength, (long)Int32.MaxValue)
		Int32[] stopIndices = new Int32[] {vertixCount, (vertixCount + normalCount), (vertixCount + normalCount + uv2Count), (vertixCount + normalCount + uv2Count + triangleCount)};
		Marshal.Copy(dataBufferPtr, vertices, 0, stopIndices[0]); 
		Marshal.Copy(dataBufferPtr, normals, stopIndices[0], stopIndices[1]); 
		Marshal.Copy(dataBufferPtr, uv2s, stopIndices[1], stopIndices[2]); 
		Marshal.Copy(dataBufferPtr, triangles, stopIndices[2], stopIndices[3]); 
        Marshal.FreeHGlobal(dataBufferPtr);

		if (debugStatements) {
			Debug.Log("Time spent on transfering the data to managed C# float arrays: " + timer.Elapsed.TotalSeconds + " seconds.");
			Debug.Log("Number of vertices: " + vertixCount);
			Debug.Log("Number of normals: " + normalCount); 
			Debug.Log("Number of uv2s: " + uv2Count); 
			Debug.Log("Number of triangles: " + triangleCount); 
			timer.Reset();
		}


		GameObject model = new GameObject(Path.GetFileName(stepFilePath));
		MeshFilter meshFilter = model.AddComponent<MeshFilter>();
		MeshRenderer meshRenderer = model.AddComponent<MeshRenderer>();

		//meshFilter.mesh.vertices = 
		//meshFilter.mesh.triangles =
		//meshFilter.mesh.normals = 
		//meshFilter.mesh.uv2 =  

		if (debugStatements) {
			Debug.Log("Time spent generating Unity meshes: " + timer.Elapsed.TotalSeconds + " seconds.");
			timer.Reset();
		}
	}
}
