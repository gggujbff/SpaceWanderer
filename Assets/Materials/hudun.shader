Shader "2D/CircularShield"
{
    Properties
    {
        _ShieldColor ("护盾颜色", Color) = (0.3, 0.6, 1.0, 1)
        _FresnelColor ("边缘颜色", Color) = (0.2, 0.8, 1.0, 1)
        _FresnelPower ("边缘强度", Range(0.1, 10)) = 3.0
        _FresnelBias ("边缘偏移", Range(0, 1)) = 0.2
        _RippleSpeed ("波纹速度", Float) = 1.0
        _RippleScale ("波纹缩放", Float) = 2.0
        _RippleIntensity ("波纹强度", Range(0, 1)) = 0.3
        _ShieldIntensity ("护盾强度", Range(0, 1)) = 0.7
        _EdgeThickness ("边缘厚度", Range(0.01, 0.5)) = 0.1
        _PulseSpeed ("脉冲速度", Float) = 1.0
        _Radius ("护盾半径", Range(0.1, 1)) = 0.5
        _Feather ("羽化程度", Range(0.001, 0.1)) = 0.02
        
        _ShieldAngle ("护盾角度", Range(0, 360)) = 180
        _AngleOffset ("角度偏移", Range(0, 360)) = 0
        _AngleFeather ("角度边缘羽化", Range(0.001, 0.1)) = 0.05
    }
    SubShader
    {
        Tags 
        { 
            "Queue" = "Transparent" 
            "RenderType" = "Transparent" 
            "IgnoreProjector" = "True" 
            "PreviewType" = "Plane"
        }
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 center : TEXCOORD1;
                float2 worldPos : TEXCOORD2;
                float2 localPos : TEXCOORD3;
            };

            fixed4 _ShieldColor;
            fixed4 _FresnelColor;
            float _FresnelPower;
            float _FresnelBias;
            float _RippleSpeed;
            float _RippleScale;
            float _RippleIntensity;
            float _ShieldIntensity;
            float _EdgeThickness;
            float _PulseSpeed;
            float _Radius;
            float _Feather;
            float _ShieldAngle;
            float _AngleOffset;
            float _AngleFeather;

            // 生成2D噪声函数
            float noise(float2 p)
            {
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }

            // 简单2D值噪声
            float simpleNoise(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);
                
                // 四个角的值
                float a = noise(i);
                float b = noise(i + float2(1.0, 0.0));
                float c = noise(i + float2(0.0, 1.0));
                float d = noise(i + float2(1.0, 1.0));
                
                // 双线性插值
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(a, b, u.x) + 
                       (c - a) * u.y * (1.0 - u.x) + 
                       (d - b) * u.x * u.y;
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv; // 直接使用UV坐标
                
                // 计算中心点（UV中心）
                o.center = float2(0.5, 0.5);
                
                // 世界坐标用于噪声计算
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xy;
                
                // 本地坐标（相对于护盾中心）
                o.localPos = v.uv - o.center;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 计算从中心到当前点的方向和距离
                float2 dir = i.uv - i.center;
                float distance = length(dir);
                
                // 1. 创建圆形遮罩 - 确保只有圆形区域内显示
                float mask = smoothstep(_Radius + _Feather, _Radius - _Feather, distance);
                
                // 如果完全在圆形外，直接返回透明
                if (mask <= 0.0) {
                    return fixed4(0, 0, 0, 0);
                }
                
                // 2. 角度遮罩计算
                float angleMask = 1.0;
                if (_ShieldAngle < 360.0) // 只有当角度小于360时才计算角度遮罩
                {
                    // 计算当前点相对于中心的角度 (atan2返回的是弧度，转换为角度)
                    float currentAngle = atan2(i.localPos.y, i.localPos.x) * 180.0 / UNITY_PI;
                    
                    // 应用角度偏移
                    currentAngle += _AngleOffset;
                    
                    // 将角度标准化到 -180 到 180 度范围
                    if (currentAngle > 180.0) currentAngle -= 360.0;
                    if (currentAngle < -180.0) currentAngle += 360.0;
                    
                    // 计算允许的半角
                    float halfAngle = _ShieldAngle * 0.5;
                    
                    // 计算角度差的绝对值
                    float angleDiff = abs(currentAngle);
                    
                    // 创建角度遮罩，使用smoothstep实现羽化边缘
                    angleMask = smoothstep(halfAngle + _AngleFeather, halfAngle - _AngleFeather, angleDiff);
                }
                
                // 如果完全在角度范围外，直接返回透明
                if (angleMask <= 0.0) {
                    return fixed4(0, 0, 0, 0);
                }
                
                // 3. 2D菲涅尔效应 - 基于到边缘的距离
                // 将距离映射到0-1范围（中心为0，边缘为1）
                float normalizedDistance = distance * 2.0; // 因为UV范围是0-1，半径最大0.5
                float edgeFactor = smoothstep(0.0, _EdgeThickness, 1.0 - normalizedDistance);
                
                float fresnel = _FresnelBias + (1.0 - _FresnelBias) * pow(edgeFactor, _FresnelPower);
                
                // 4. 生成动态波纹
                float ripple = simpleNoise(i.worldPos * _RippleScale + _Time.y * _RippleSpeed);
                ripple = ripple * 2.0 - 1.0; // 转换到[-1,1]范围
                
                // 应用波纹到菲涅尔
                fresnel = saturate(fresnel + ripple * _RippleIntensity);
                
                // 5. 添加脉冲效果
                float pulse = (sin(_Time.y * _PulseSpeed) + 1.0) * 0.5;
                fresnel += pulse * 0.1;
                
                // 6. 护盾效果颜色
                fixed4 shieldColor = lerp(_ShieldColor, _FresnelColor, fresnel);
                
                // 7. 透明度控制
                shieldColor.a = saturate(mask * fresnel * _ShieldIntensity * 2.0 * angleMask);
                
                // 8. 边缘增强
                shieldColor.rgb *= (1.0 + fresnel * 0.8);
                
                return shieldColor;
            }
            ENDCG
        }
    }
    FallBack "Sprites/Default"
}