using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FogManager : MonoBehaviour {

	public static FogManager singleton;

    [SerializeField] private Vector2 FogOrigin = Vector2.zero;
    [SerializeField] private Mesh fogObjMesh;
    [SerializeField] private Material fogMaterial;
    [SerializeField] private Color FogColor = Color.gray;
    [SerializeField] private int fogInstancesCount = 10;
    [SerializeField] private float FogPosY = 1f;
    [SerializeField] private float FogScaleY = 1f;
    [SerializeField] private float FogStartScale = 1f;
    [SerializeField] private float FogAddScalePerStep = 1f;

    [SerializeField] private Mesh fogUnderwaterObjMesh;
    [SerializeField] private Material fogUnderwaterMaterial;
    [SerializeField] private Color FogUnderwaterColor = Color.green;
    [SerializeField] private int fogUnderwaterInstancesCount = 10;
    [SerializeField] private float FogUnderwaterPosYStart = 1f;
    [SerializeField] private float FogUnderwaterScaleXZ = 1f;
    [SerializeField] private float FogUnderwaterHeightPerStep = 1f;

    [SerializeField] private bool m_underwaterFogActive = true;
    [SerializeField] private bool m_heightFogActive = true;
    [SerializeField] private bool m_heightFogOnlyLast = false;

    private Matrix4x4[] matricesFogInstances;
	private Matrix4x4[] matrixLastFogInstance = new Matrix4x4[1];
	private Matrix4x4[] matricesUnderwaterFogInstances;
	private Matrix4x4[] matrixLastUnderwaterFogInstance = new Matrix4x4[1];
	private Material firstInstancedMat;
	private Material lastInstanceMat;
	private Material firstIUnterwaterInstancedMat;
	private Material lastUnterwaterInstanceMat;

	//[SerializeField]
	//private Vector3[] scales;

	void Awake()
	{
		singleton = this;
	}

	void Start()
	{
		recalculateFogInstances();
		recalculateFogUnderwaterInstances();
	}

	void FixedUpdate()
	{
		recalculateFogInstances();
		recalculateFogUnderwaterInstances();
	}

	void Update()
	{
        // normal fog
        if (m_heightFogActive)
        {
            if (!m_heightFogOnlyLast)
            {
                Graphics.DrawMeshInstanced(fogObjMesh, 0, firstInstancedMat, matricesFogInstances, matricesFogInstances.Length, null, UnityEngine.Rendering.ShadowCastingMode.Off, false);
            }
            Graphics.DrawMeshInstanced(fogObjMesh, 0, lastInstanceMat, matrixLastFogInstance, 1, null, UnityEngine.Rendering.ShadowCastingMode.Off, false);
        }
        // underwater fog
        if (m_underwaterFogActive)
        {
            Graphics.DrawMeshInstanced(fogUnderwaterObjMesh, 0, firstIUnterwaterInstancedMat, matricesUnderwaterFogInstances, matricesUnderwaterFogInstances.Length, null, UnityEngine.Rendering.ShadowCastingMode.Off, false);
            Graphics.DrawMeshInstanced(fogUnderwaterObjMesh, 0, lastUnterwaterInstanceMat, matrixLastUnderwaterFogInstance, 1, null, UnityEngine.Rendering.ShadowCastingMode.Off, false);
        }
	}

	private void recalculateFogInstances()
	{
		firstInstancedMat = new Material(fogMaterial);
		firstInstancedMat.SetColor("_Color", FogColor);

		Color lastInstanceColor = new Color(FogColor.r,FogColor.g,FogColor.b,1f);
		lastInstanceMat = new Material(fogMaterial);
		lastInstanceMat.SetColor("_Color", lastInstanceColor);

		matricesFogInstances = new Matrix4x4[fogInstancesCount];
		Vector3 position = new Vector3(FogOrigin.x, FogPosY,FogOrigin.y);

		//scales = new Vector3[fogInstancesCount];

		for(int i = 0; i < matricesFogInstances.Length;i++)
		{
			//scales[i] = new Vector3(FogStartScale+FogAddScalePerStep*i,FogScaleY,FogStartScale+FogAddScalePerStep*i);
			matricesFogInstances[i] = Matrix4x4.TRS(position,Quaternion.identity,new Vector3(FogStartScale+FogAddScalePerStep*i,FogScaleY,FogStartScale+FogAddScalePerStep*i));
		}
		matrixLastFogInstance[0] = Matrix4x4.TRS(position,Quaternion.identity,new Vector3(FogStartScale+FogAddScalePerStep*matricesFogInstances.Length,FogScaleY,FogStartScale+FogAddScalePerStep*matricesFogInstances.Length));
	}

	private void recalculateFogUnderwaterInstances()
	{
		firstIUnterwaterInstancedMat = new Material(fogUnderwaterMaterial);
		firstIUnterwaterInstancedMat.SetColor("_Color", FogUnderwaterColor);

		Color lastInstanceColor = new Color(FogUnderwaterColor.r,FogUnderwaterColor.g,FogUnderwaterColor.b,1f);
		lastUnterwaterInstanceMat = new Material(fogUnderwaterMaterial);
		lastUnterwaterInstanceMat.SetColor("_Color", lastInstanceColor);

		matricesUnderwaterFogInstances = new Matrix4x4[fogUnderwaterInstancesCount];
		Vector3 scale = new Vector3(FogUnderwaterScaleXZ,1,FogUnderwaterScaleXZ);

		for(int i = 0; i < matricesUnderwaterFogInstances.Length;i++)
		{
			matricesUnderwaterFogInstances[i] = Matrix4x4.TRS(new Vector3(FogOrigin.x,FogUnderwaterPosYStart - FogUnderwaterHeightPerStep * i,FogOrigin.y),Quaternion.identity,scale);
		}
		matrixLastUnderwaterFogInstance[0] = Matrix4x4.TRS(new Vector3					(FogOrigin.x,FogUnderwaterPosYStart - FogUnderwaterHeightPerStep * matricesUnderwaterFogInstances.Length,FogOrigin.y),Quaternion.identity,scale);
	}

	public void setFogOrigin(Vector2 newPos)
	{
		FogOrigin = newPos;
	}
	public void setFogOrigin(Vector3 newPos)
	{
		FogOrigin = new Vector2(newPos.x,newPos.z);
	}
}
