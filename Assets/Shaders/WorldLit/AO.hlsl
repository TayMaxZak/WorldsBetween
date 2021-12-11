#ifndef CUSTOM_AMBIENT_OCCLUSION_INCLUDED
#define CUSTOM_AMBIENT_OCCLUSION_INCLUDED
 
void AmbientOcclusion_float(float2 NormalizedScreenSpaceUV, out float Direct, out float Indirect)
{
    #ifdef SHADERGRAPH_PREVIEW
        Direct = 1;
        Indirect = 1;
    #else
        AmbientOcclusionFactor factor = GetScreenSpaceAmbientOcclusion(NormalizedScreenSpaceUV);
        Direct = factor.directAmbientOcclusion;
        Indirect = factor.indirectAmbientOcclusion;
    #endif
}
 
#endif //CUSTOM_AMBIENT_OCCLUSION_INCLUDED