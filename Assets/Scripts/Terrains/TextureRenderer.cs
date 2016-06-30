using UnityEngine;
using System.Collections;

public class TextureRenderer : MonoBehaviour 
{
	void Start () 
	{
		//Terrain terrain = GetComponent<Terrain>();
		Material mat = GetComponent<Material>();
		mat.SetTextureScale("Tiling", new Vector2(transform.localScale.x, transform.localScale.z));
	}
}
