using UnityEngine;
using UnityEngine.Rendering;

public class FullscreenQuad : MonoBehaviour
{
	public Material mat;

	void Start()
	{
		RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
	}

	void Point(float x, float y)
	{
		GL.TexCoord2(x, y);
		GL.Vertex3(x, y, -1);
	}

	void OnEndCameraRendering(ScriptableRenderContext context, Camera camera)
	{
		mat.SetPass(0);
		GL.PushMatrix();
		GL.LoadIdentity();
		GL.LoadProjectionMatrix(Matrix4x4.Ortho(0, 1, 0, 1, 0, 1));
		GL.Begin(GL.QUADS);
		Point(0, 0);
		Point(0, 1);
		Point(1, 1);
		Point(1, 0);
		GL.End();
		GL.PopMatrix();
	}

	void OnDestroy()
	{
		RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
	}
}