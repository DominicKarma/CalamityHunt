﻿sampler2D uImage0 : register(s0);
texture uTextureClose;
sampler tex0 = sampler_state
{
    texture = <uTextureClose>;
    magfilter = LINEAR;
    minfilter = LINEAR;
    mipfilter = LINEAR;
    AddressU = wrap;
    AddressV = wrap;
};
texture uTextureFar;
sampler tex1 = sampler_state
{
    texture = <uTextureFar>;
    magfilter = LINEAR;
    minfilter = LINEAR;
    mipfilter = LINEAR;
    AddressU = wrap;
    AddressV = wrap;
};
float2 uPosition;
float2 uParallax;
float2 uScrollClose;
float2 uScrollFar;
float4 uCloseColor;
float4 uFarColor;
float4 uOutlineColor;
float2 uImageRatio;

float4 PixelShaderFunction(float4 baseColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float4 screen = tex2D(uImage0, coords);
    float4 spaceClose = tex2D(tex0, frac(coords * uImageRatio + (uPosition) * uParallax.x) + uScrollClose + sin(coords.y) * 0.01);
    float4 spaceFar = tex2D(tex1, frac(coords * uImageRatio + (uPosition) * uParallax.y) + uScrollFar);
    
    float4 backColor = lerp(uFarColor, float4(uFarColor.r * 0.7, uFarColor.g * 0.95, uFarColor.b * 1.1, uFarColor.a), sin(coords.x * 10 + uScrollClose.x * 50 + uPosition.x));
    float4 final = pow(spaceClose, 1.33) * uCloseColor + pow(spaceClose * 1.4, 4) + spaceFar * 0.85 * backColor + pow(spaceFar * 1.1, 4);
    //float outline = screen.g - screen.r - screen.b;
    //if (outline > 0.4)
    //    return (final * screen.a + outline * 3 * uOutlineColor);
    
    return final * screen.a;
}

technique Technique1
{
    pass GelPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}