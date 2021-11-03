using UnityEngine;

[SelectionBase]
public class ChunkGameObject : MonoBehaviour
{
	[System.NonSerialized]
	public Chunk data;

	public MeshFilter filter;
}
