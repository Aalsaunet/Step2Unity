using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using System;
using System.IO;
using System.Threading;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Text;

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
	public Material[] defaultMaterials;
	private GameObject rootObject;

	/* Subshape variables */
	private Int32 totalNumberOfSubShapes = 0;

	/* Debug variables */
	private uint totalVertexCount = 0;
	private uint totalTriangleCount = 0;

	void Start () {	
		System.Diagnostics.Stopwatch timer = (debugStatements) ? System.Diagnostics.Stopwatch.StartNew() : null;	
		int returnCode = ImportSTEPFile(Marshal.StringToHGlobalAnsi(stepFilePath.Replace("\\", "\\\\")), ref totalNumberOfSubShapes);

		if (debugStatements) Debug.Log("Finding all sub shapes took: " + timer.Elapsed.ToString());
		if (totalNumberOfSubShapes <= 0) return;

		rootObject = new GameObject(Path.GetFileName(stepFilePath));
		for (int i = 0; i < totalNumberOfSubShapes; i++)
			GetSubShape(i);
			
		ClearStepModelData();
		
		if (debugStatements) {
			Debug.Log(
				"Number of failed translations: " + returnCode +
				"\nTotal time spent on Open Cascade geometry generation: " + timer.Elapsed.ToString() +
				"\nTotal number of vertices: " + totalVertexCount.ToString() +
				"\nTotal number of triangles: " + totalTriangleCount.ToString()
			); 
		}		
	} 

	private void GetSubShape(int subShapeIndex) {

		IntPtr vertexBuffer = IntPtr.Zero, indexBuffer = IntPtr.Zero;
		Int32 vertexEntryCount = 0, indexCount = 0;

		int returncode = ProcessSubShape(subShapeIndex, ref vertexBuffer, ref vertexEntryCount, ref indexBuffer, ref indexCount);
		if (returncode != 0)
			return;

		float[] managedVertexBuffer = new float[vertexEntryCount];
		int[] managedIndexBuffer = new int[indexCount];

		Marshal.Copy(vertexBuffer, managedVertexBuffer, 0, vertexEntryCount);
		Marshal.Copy(indexBuffer, managedIndexBuffer, 0, indexCount);
		Marshal.FreeHGlobal(vertexBuffer);
		Marshal.FreeHGlobal(indexBuffer);

		CreateGameObject(vertexEntryCount, subShapeIndex, managedVertexBuffer, managedIndexBuffer);
    }

    private void CreateGameObject(int vertexEntryCount, int index, float[] managedVertexBuffer, int[] managedIndexBuffer)
    {
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
        meshRenderer.sharedMaterial = defaultMaterials[index % defaultMaterials.Length];
        totalVertexCount += (uint)vertices.Length;
        totalTriangleCount += (uint)(managedIndexBuffer.Length / 3);
    }
}
