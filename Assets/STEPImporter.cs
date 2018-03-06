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
    Int32 vertexElementCount = 0;
	Int32 normalElementCount = 0;
	Int32 uv2ElementCount = 0;
	Int32 triangleElementCount = 0;
	
	[DllImport("STEPImporter")]
	private static extern int Get3DGeometry(string filepath, ref IntPtr dataBufferPtr, ref Int32 vertexCount, ref Int32 normalCount, ref Int32 uv2Count, ref Int32 triangleCount);


	void Start () {
		System.Diagnostics.Stopwatch timer = (debugStatements) ? System.Diagnostics.Stopwatch.StartNew() : null;
		
		// Call the C++ dll with the file name
		int returnCode = Get3DGeometry(stepFilePath, ref dataBufferPtr, ref vertexElementCount, ref normalElementCount, ref uv2ElementCount, ref triangleElementCount);
		
		// TODO: Check the return code

		if (debugStatements) {
			Debug.Log("Time spent on Open Cascade geometry generation: " + timer.Elapsed.TotalSeconds + " seconds.");
			timer.Reset();
		}
			
		float[] vertexBuffer = new float[vertexElementCount];
		float[] normalBuffer = new float[normalElementCount];
		float[] uv2Buffer = new float[uv2ElementCount];
		float[] triangleBuffer = new float[triangleElementCount];
		
		// Possible might want: (int)Math.Min(dataBufLength, (long)Int32.MaxValue)
		Int32[] stopIndices = new Int32[] {vertexElementCount, (vertexElementCount + normalElementCount), (vertexElementCount + normalElementCount + uv2ElementCount), (vertexElementCount + normalElementCount + uv2ElementCount + triangleElementCount)};
		Marshal.Copy(dataBufferPtr, vertexBuffer, 0, stopIndices[0]); 
		Marshal.Copy(dataBufferPtr, normalBuffer, stopIndices[0], stopIndices[1]); 
		Marshal.Copy(dataBufferPtr, uv2Buffer, stopIndices[1], stopIndices[2]); 
		Marshal.Copy(dataBufferPtr, triangleBuffer, stopIndices[2], stopIndices[3]); 
        Marshal.FreeHGlobal(dataBufferPtr);

		if (debugStatements) {
			Debug.Log("Time spent on transfering the data to managed C# float arrays: " + timer.Elapsed.TotalSeconds + " seconds.");
			Debug.Log("Number of vertices: " + vertexElementCount / 3);
			Debug.Log("Number of normals: " + normalElementCount / 3); 
			Debug.Log("Number of uv2s: " + uv2ElementCount / 2); 
			Debug.Log("Number of triangles: " + triangleElementCount); 
			timer.Reset();
		}

		Vector3[] vertices = new Vector3[vertexElementCount / 3];
		for (int i = 0, j = 0; j < stopIndices[0]; i++, j += 3)
			vertices[i] = new Vector3(vertexBuffer[j], vertexBuffer[j + 1], vertexBuffer[j + 2]);

		Vector3[] normals = new Vector3[normalElementCount / 3];
		for (int i = 0, j = stopIndices[0]; j < stopIndices[1]; i++, j += 3)
			normals[i] = new Vector3(normalBuffer[j], normalBuffer[j + 1], normalBuffer[j + 2]);

		Vector2[] uv2s = new Vector2[uv2ElementCount / 2];
		for (int i = 0, j = stopIndices[1]; j < stopIndices[2]; i++, j += 2)
			uv2s[i] = new Vector3(uv2Buffer[j], uv2Buffer[j + 1]);

		Mesh mesh = new Mesh();
		mesh.vertices = vertices;
		mesh.normals = normals;
		mesh.uv2 = uv2s;
		mesh.triangles = Array.ConvertAll(triangleBuffer, x => (int)x);

		GameObject model = new GameObject(Path.GetFileName(stepFilePath));
		MeshFilter meshFilter = model.AddComponent<MeshFilter>();
		meshFilter.mesh = mesh;
		MeshRenderer meshRenderer = model.AddComponent<MeshRenderer>();

		Instantiate(model, Vector3.zero, Quaternion.identity);

		if (debugStatements) {
			Debug.Log("Time spent generating Unity meshes: " + timer.Elapsed.TotalSeconds + " seconds.");
			timer.Reset();
		}
	}
}
