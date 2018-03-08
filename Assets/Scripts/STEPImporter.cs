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
	IntPtr geometricalBufferPtr = IntPtr.Zero;
	IntPtr indexBufferPtr = IntPtr.Zero;
    Int32 vertexElementCount = 0;
	Int32 normalElementCount = 0;
	Int32 uv2ElementCount = 0;
	Int32 triangleElementCount = 0;
	
	[DllImport("STEPImport")]
	private static extern int Get3DGeometry(string filepath, ref IntPtr geometricalBufferPtr, ref IntPtr indexBufferPtr, ref Int32 vertexCount, ref Int32 normalCount, ref Int32 uv2Count, ref Int32 triangleCount);

	void Start () {
		
		System.Diagnostics.Stopwatch timer = (debugStatements) ? System.Diagnostics.Stopwatch.StartNew() : null;
		
		// Call the C++ dll with the file name
		int returnCode = Get3DGeometry(stepFilePath, ref geometricalBufferPtr, ref indexBufferPtr, ref vertexElementCount, ref normalElementCount, ref uv2ElementCount, ref triangleElementCount);
		
		Debug.Log("Number of unique indices: " + returnCode);
		// TODO: Check the return code

		if (debugStatements) {
			Debug.Log("Time spent on Open Cascade geometry generation: " + timer.Elapsed.TotalSeconds + " seconds.");
			timer.Reset();
		}
		if (debugStatements) {
			Debug.Log("Time spent on transfering the data to managed C# float arrays: " + timer.Elapsed.TotalSeconds + " seconds.");
			Debug.Log("Number of vertices: " + vertexElementCount / 3);
			Debug.Log("Number of normals: " + normalElementCount / 3); 
			Debug.Log("Number of uv2s: " + uv2ElementCount / 2); 
			Debug.Log("Number of triangles: " + triangleElementCount / 3); 
			timer.Reset();
		}
			
		float[] vertexBuffer = new float[vertexElementCount];
		float[] normalBuffer = new float[normalElementCount];
		float[] uv2Buffer = new float[uv2ElementCount];
		int[] triangleBuffer = new int[triangleElementCount];
		
		// Possible might want: (int)Math.Min(dataBufLength, (long)Int32.MaxValue)
		//Int32[] stopIndices = new Int32[] {vertexElementCount, (vertexElementCount + normalElementCount), (vertexElementCount + normalElementCount + uv2ElementCount), (vertexElementCount + normalElementCount + uv2ElementCount + triangleElementCount)};
		Int32[] startIndices = new Int32[] {0, vertexElementCount, (vertexElementCount + normalElementCount)};
		try {
			Marshal.Copy(new IntPtr(geometricalBufferPtr.ToInt32() + startIndices[0]), vertexBuffer, 0, vertexElementCount); 
			Marshal.Copy(new IntPtr(geometricalBufferPtr.ToInt32() + startIndices[1]), normalBuffer, 0, normalElementCount); 
			Marshal.Copy(new IntPtr(geometricalBufferPtr.ToInt32() + startIndices[2]), uv2Buffer, 0, uv2ElementCount); 
			Marshal.Copy(indexBufferPtr, triangleBuffer, 0, triangleElementCount); 
		}
		finally {
			Marshal.FreeHGlobal(geometricalBufferPtr);
			Marshal.FreeHGlobal(indexBufferPtr);
		}        

		foreach (var value in triangleBuffer)
			Debug.Log(value);

		
		Vector3[] vertices = new Vector3[vertexElementCount / 3];
		for (int i = 0, j = 0; j < vertexElementCount; i++, j += 3)
			vertices[i] = new Vector3(vertexBuffer[j], vertexBuffer[j + 1], vertexBuffer[j + 2]);

		Vector3[] normals = new Vector3[normalElementCount / 3];
		for (int i = 0, j = 0; j < normalElementCount; i++, j += 3)
			normals[i] = new Vector3(normalBuffer[j], normalBuffer[j + 1], normalBuffer[j + 2]);

		Vector2[] uv2s = new Vector2[uv2ElementCount / 2];
		for (int i = 0, j = 0; j < uv2ElementCount; i++, j += 2)
			uv2s[i] = new Vector2(uv2Buffer[j], uv2Buffer[j + 1]);

		Mesh mesh = new Mesh();
		mesh.vertices = vertices;
		mesh.normals = normals;
		mesh.uv2 = uv2s;
		mesh.triangles = triangleBuffer;
		
		//TODO Remove
		// int[] triangles2 = new int[2736];
		// for (int i = 0; i < triangles2.Length; i++)
		// 	triangles2[i] = triangleBuffer[i];
		// mesh.triangles = triangles2;

		GameObject model = new GameObject(Path.GetFileName(stepFilePath));
		MeshFilter meshFilter = model.AddComponent<MeshFilter>();
		meshFilter.mesh = mesh;
		
		MeshRenderer meshRenderer = model.AddComponent<MeshRenderer>();	
		meshRenderer.material = defaultMaterial;
		
		Instantiate(model, Vector3.zero, Quaternion.identity);	

		Debug.Log("Triangles: " + meshFilter.mesh.triangles.Length);
		if (debugStatements) {
			Debug.Log("Time spent generating Unity meshes: " + timer.Elapsed.TotalSeconds + " seconds.");
			timer.Reset();
		}

		// Int32[] triangleBuffer2 = new Int32[triangleBuffer.Length];
		// for (int i = 0; i < triangleBuffer.Length; i++) {	
		// 	Debug.Log("Index float value: " + triangleBuffer[i]);
		// 	triangleBuffer2[i] = (Int32)triangleBuffer[i];
		// 	Debug.Log("Index int value: " + triangleBuffer2[i]);
		// }
		// mesh.triangles = triangleBuffer2;
		// //mesh.triangles = Array.ConvertAll(triangleBuffer, x => (int)x);
	}
}
