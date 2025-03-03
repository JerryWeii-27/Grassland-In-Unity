Shader "Custom/GrassLowLod"
{
    Properties
    {
        _Albedo1 ("Albedo 1", Color) = (1, 1, 1)
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            RWStructuredBuffer<float3> _positionBuffer;

            // Input structure for the vertex data
            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : POSITION;
            };

            float Random(float2 seed)
            {
                return frac(sin(dot(seed, float2(12.9898, 78.233))) * 43758.5453);
            }

            float3 RotateY(float3 position, float radians)
            {
                float sina, cosa;
                sincos(radians, sina, cosa);

                float2 rotatedXZ = float2(cosa * position.x - sina * position.z, sina * position.x + cosa * position.z);

                return float3(rotatedXZ.x, position.y, rotatedXZ.y);
            }

            v2f vert(appdata v, uint instanceID : SV_INSTANCEID)
            {
                v2f o;

                // Fetch position of grass.
                float4 instancePos = float4(_positionBuffer[instanceID], 1.0f);
                
                // Apply random rotation around y-axis.
                float2 xzPos = float2(instancePos.x, instancePos.z);

                float rotationAngle = Random(xzPos) * 2.0f * UNITY_PI; // Convert to radians.
                float3 rotatedVertex = RotateY(v.vertex.xyz, rotationAngle);

                o.pos = UnityObjectToClipPos(float4(rotatedVertex + instancePos.xyz, 1.0f));
                return o;
            }
        
            half4 _Albedo1;

            half4 frag(v2f i) : SV_Target
            {
                return _Albedo1;
            }
            ENDCG
        }
    }
}
