#ifndef FRACTALS_CGINC
#define FRACTALS_CGINC

#include "./Utils.cginc"

inline float Mandelbulb(float3 p, int loop, float power)
{
    float3 z = p;
	float dr = 1.0;
	float r = 0.0;
	for (int i = 0; i < loop ; i++) {
        r = length(z);
        if(r > 10)
            break;

        // convert to polar coordinates
		float theta = acos(z.y/r);
		float phi = atan2(z.z, z.x) + PI/4;

		dr =  (pow(r, power-1.0)*power*dr) + 1.0;
		
		// scale and rotate the point
		float zr = pow(r, power);
		theta *= power;
		phi *= power;
		
		// convert back to cartesian coordinates
		z = zr*float3(sin(theta)*cos(phi), cos(theta), sin(theta)*sin(phi));
		z += p;
	}
	return 0.5*log(r)*r/dr;
}

inline float KochTetrahedron(float3 p, int loop)
{
	float3 z = p;
	float s = 1/3.0;
	float r = Tetrahedron(z, 1);
	for (int n = 0; n < loop; n++) {
		z = Mirror(z, float3(.36, 1, -.61), 0);
		z = Mirror(z, float3(.36, 1, .61), 0);
		z = Mirror(z, float3(-.7,1,0), 0);
		z = Mirror(z, float3(0,-1,0), s);
		z = Rotate(float4(0, 0.5, 0, 0.8660254), z*2.0 + float3(0,s,0));
		float i = pow(2,-n-1) * Tetrahedron(z, 1);
		r = min(r,i);
    }
    return r;
}

inline float SierpinskiTetrahedron(float3 pos, int loop)
{
	float3 z = pos;
	const float Scale = 2.0;
	const float Offset = 3.0;
    for (int n = 0; n < loop; n++) {
       if(z.x+z.y<0) z.xy = -z.yx;
       if(z.x+z.z<0) z.xz = -z.zx;
       if(z.y+z.z<0) z.zy = -z.yz;
       z = z*Scale - Offset*(Scale-1.0);
    }
    return (length(z) ) * pow(Scale, -float(loop));
}



#endif
