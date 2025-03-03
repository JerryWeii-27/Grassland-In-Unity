Shader "Custom/GrassHighLod"
{
    Properties
    {
        _Albedo1 ("Albedo 1", Color) = (1, 1, 1)
        _WindAngle ("Wind Angle (Degree)", float) = 0
        _WindSpeed ("Wind Speed", float) = 1
        _WindStrMultiplier ("Wind Strength Multiplier", float) = 1
        _WindMaxStrHeight ("Wind Max Strength Height", float) = 1
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

            // Declare the buffer for position data.
            RWStructuredBuffer<float3> _positionBuffer;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            // Output structure
            struct v2f
            {
                float4 pos : POSITION;
            };

            // Random number [0, 1].
            float Random(float2 seed)
            {
                return frac(sin(dot(seed, float2(12.9898, 78.233))) * 43758.5453);
            }

            // Rotate a point around the Y-axis.
            float3 RotateY(float3 position, float radians)
            {
                float sina, cosa;
                sincos(radians, sina, cosa);

                float2 rotatedXZ = float2(cosa * position.x - sina * position.z, sina * position.x + cosa * position.z);

                return float3(rotatedXZ.x, position.y, rotatedXZ.y);
            }

            // Properties.
            float _WindSpeed;
            float _WindAngle;
            float _WindMaxStrHeight;
            float _WindStrMultiplier;

            v2f vert(appdata v, uint instanceID : SV_INSTANCEID)
            {
                v2f o;

                // Fetch position of grass.
                float4 instancePos = float4(_positionBuffer[instanceID], 1.0f);
                
                // Apply random rotation around y-axis.
                float2 xzPos = float2(instancePos.x, instancePos.z);

                float rotationAngle = Random(xzPos) * 2.0f * UNITY_PI; // Convert to radians.
                float3 rotatedVertex = RotateY(v.vertex.xyz, rotationAngle);
                
                // Wind.
                float timeFactor = _Time.y * _WindSpeed;
                float cosAngle = cos(_WindAngle * UNITY_PI / 180.0f);
                float sinAngle = sin(_WindAngle * UNITY_PI / 180.0f);

                // Wind base str is between [0, 1].
                float windBaseStr = sin(cosAngle * (xzPos.x + timeFactor) + sinAngle * (xzPos.y+timeFactor));
                windBaseStr = windBaseStr * 0.5f + 0.5f; // Change range to [0, 1];
                
                // Direction of grass displacement. The magnitude is always 1.
                float3 windDirection = float3(cosAngle * cosAngle, 0, sinAngle * sinAngle);

                // Wind true str is not between [0, 1]. It is influenced by height of vertex.
                float windTrueStr = _WindStrMultiplier * windBaseStr * (rotatedVertex.y / _WindMaxStrHeight);
                
                float3 windDisplacement = windDirection * windTrueStr;

                // Decrease height according to horizontal displacement so grass does not appear stretched.
                windDisplacement.y -= _WindMaxStrHeight * (1- sqrt(1 - (windTrueStr / _WindMaxStrHeight) * (windTrueStr / _WindMaxStrHeight)));

                rotatedVertex += windDisplacement;

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
