
/**************************************************************************************/
/* Variables comunes */
/**************************************************************************************/

//Matrices de transformacion
float4x4 matWorld; //Matriz de transformacion World
float4x4 matWorldView; //Matriz World * View
float4x4 matWorldViewProj; //Matriz World * View * Projection
float4x4 matInverseTransposeWorld; //Matriz Transpose(Invert(World))

float screen_dx = 1024;
float screen_dy = 768;

//Textura para DiffuseMap
texture texDiffuseMap;
sampler2D diffuseMap = sampler_state
{
    Texture = (texDiffuseMap);
    ADDRESSU = WRAP;
    ADDRESSV = WRAP;
    MINFILTER = LINEAR;
    MAGFILTER = LINEAR;
    MIPFILTER = LINEAR;
};


float4 ColorSuperficie;
float time = 0;

/**************************************************************************************/
/* RenderScene */
/**************************************************************************************/

//Input del Vertex Shader
struct VS_INPUT
{
    float4 Position : POSITION0;
    float4 Color : COLOR0;
    float2 Texcoord : TEXCOORD0;
};

//Output del Vertex Shader
struct VS_OUTPUT
{
    float4 Position : POSITION0;
    float2 Texcoord : TEXCOORD0;
    float2 RealPos : TEXCOORD1;
    float4 Color : COLOR0;
};


//Vertex Shader
VS_OUTPUT vs_main(VS_INPUT Input)
{
    VS_OUTPUT Output;
    Output.RealPos = Input.Position;
		
	//------------
	float frecuencia=2;
	Input.Position.y=20*cos(frecuencia*(Input.Position.x+time))+20*sin(frecuencia*(Input.Position.z+time));
    Input.Position.y+=cos(time);
	//-------------
	

	//Input.Position.x=10*cos(time);
	//Input.Position.y=10*sin(time);
		
	//Proyectar posicion
    Output.Position = mul(Input.Position, matWorldViewProj);
   
	//Propago las coordenadas de textura
    Output.Texcoord = Input.Texcoord;

	 //Input.Color.b = 100;


	//Propago el color x vertice
    Output.Color = Input.Color;

    return (Output);
}

//Pixel Shader
float4 ps_main(float2 Texcoord : TEXCOORD0, float4 Color : COLOR0) : COLOR0
{
// Obtener el texel de textura
	// diffuseMap es el sampler, Texcoord son las coordenadas interpoladas
    float4 fvBaseColor = tex2D(diffuseMap, Texcoord);
	// combino color y textura
	// en este ejemplo combino un 80% el color de la textura y un 20%el del vertice

	fvBaseColor.a=0.7;

    return fvBaseColor*0.4 + 0.6*ColorSuperficie; //0.8 * fvBaseColor + 0.2 * Color;
}

// ------------------------------------------------------------------
technique OleajeNormal
{
    pass Pass_0
    {	
		AlphaBlendEnable = TRUE;

        VertexShader = compile vs_3_0 vs_main();
        PixelShader = compile ps_3_0 ps_main();
    }
}