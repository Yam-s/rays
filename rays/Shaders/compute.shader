#version 430 core

layout(local_size_x = 8, local_size_y = 8) in;
layout(rgba32f, binding = 0) uniform image2D img_output;

uniform vec3 camera;
uniform vec3 rayBottomLeft;
uniform vec3 rayBottomRight;
uniform vec3 rayTopLeft;
uniform vec3 rayTopRight;

#define NUM_SPHERES 2
uniform vec4[NUM_SPHERES] spheres;

uniform sampler2D skybox;

uniform float _randseed;
float seed = 0;
vec2 _pixel = gl_GlobalInvocationID.xy;

const float PI = 3.14159265f;
const float infinity = 1. / 0.;

// Ray Types
const uint LightRay = 0x100;
const uint ShadowRay = 0x101;

float rand(){
    float result = fract(sin(seed / 100.0f * dot(_pixel, vec2(12.9898,78.233))) * 43758.5453123);
	seed += 1.0f;
	return result;
}

struct Ray
{
	vec3 position;
	vec3 direction;
	vec3 energy;
	uint type;
};

Ray CreateRay(vec3 position, vec3 direction, uint type)
{
	Ray ray;
	ray.position = position;
	ray.direction = direction;
	ray.energy = vec3(1.0f, 1.0f, 1.0f);
	ray.type = type;
	return ray;
};

struct RayHit
{
	vec3 position;
	float distance;
	vec3 normal;
	vec3 albedo;
	vec3 specular;
	vec2 tex;
};

RayHit CreateRayHit()
{
	RayHit hit;
    hit.position = vec3(0.0f, 0.0f, 0.0f);
    hit.distance = infinity;
    hit.normal = vec3(0.0f, 0.0f, 0.0f);
	hit.albedo = vec3(0.0f, 0.0f, 0.0f);
	hit.specular = vec3(0.0f, 0.0f, 0.0f);
	hit.tex = vec2(0.0f, 0.0f);
    return hit;
};

struct Sphere
{
	vec3 position;
	float radius;
	vec3 albedo;
	vec3 specular;
};

Sphere CreateSphere(float x, float y, float z, float radius)
{
	Sphere sphere;
	sphere.position = vec3(x, y, z);
	sphere.radius = radius;
	sphere.albedo = vec3(x, 1.0f, z);
	sphere.specular = vec3(0.6f, 0.6f, 0.6f);
	return sphere;
}

struct Light
{
	vec3 color;
	float intensity;
	vec3 direction;
};

Light CreateLight(vec3 color, float intensity, vec3 direction)
{
	Light light;
	light.color = color;
	light.intensity = intensity;
	light.direction = direction;
	return light;
}

float RaySphereIntersect(Ray ray, inout Sphere sphere) {
    vec3 oc = ray.position - sphere.position;
	float a = dot(ray.direction, ray.direction);
	float b = 2.0 * dot(oc, ray.direction);
	float c = dot(oc, oc) - (sphere.radius * sphere.radius);

	float discriminant = b*b - 4.0*a*c;

	if (discriminant < 0.0)
		return infinity;

	float denom = (2.0*a);
	float t1 = (-b - sqrt(discriminant));
	float t2 = (-b + sqrt(discriminant));

	if (t1 > 0.0)
	{
		if (t2 > 0.0)
			return min(t1 / denom, t2 / denom);
		return t1;
	}
		
	return infinity;
}

void IntersectSphere(inout Ray ray, inout RayHit hit, Sphere sphere)
{
	float t = RaySphereIntersect(ray, sphere);

	if (t > 0 && t < hit.distance)
	{
		hit.distance = t;
		hit.position = ray.position + t * ray.direction;
		hit.normal = normalize(hit.position - sphere.position);
		hit.albedo = sphere.albedo;
		hit.specular = sphere.specular;

		// Texture mapping
		//float texScale = 8;
		//hit.tex.x = ((0.5 + atan(hit.normal.x, hit.normal.z) / PI) * 0.5) * texScale;
		//hit.tex.y =  (0.5 - acos(hit.normal.y) / PI) * texScale;
	}
}

// Copy-pasted from stack overflow
// Calculates 
vec3 computePrimaryTexDir(vec3 normal)
{
    vec3 a = cross(normal, vec3(1, 0, 0));
    vec3 b = cross(normal, vec3(0, 1, 0));

    vec3 max_ab = dot(a, a) < dot(b, b) ? b : a;

    vec3 c = cross(normal, vec3(0, 0, 1));

    return normalize(dot(max_ab, max_ab) < dot(c, c) ? c : max_ab);
}

void IntersectGroundPlane(Ray ray, inout RayHit hit, vec3 origin, vec3 normal)
{
	float denom = dot(normal, ray.direction);
	if (abs(denom) > 0.0001f)
	{
		float t = dot(origin - ray.position, normal) / denom;
		if (t >= 0 && t < hit.distance)
		{
			hit.distance = t;
			hit.position = ray.position + t * ray.direction;
			hit.normal = normal;
			hit.albedo = vec3(1f, 1f, 1f);
			hit.specular = vec3(0.6f, 0.6f, 0.6f);

			// Plane textures
			vec3 test = computePrimaryTexDir(hit.normal);
			float texScale = 2;
			hit.tex.x = dot(hit.position, test) * texScale;
			hit.tex.y = dot(hit.position, cross(hit.normal, test)) * texScale;
		}
	}
}



RayHit Trace(Ray ray)
{
	RayHit hit = CreateRayHit();


	for (int i = 0; i <= NUM_SPHERES; i++)
	{
		if (ray.type == ShadowRay && hit.distance != infinity)
			break;
		Sphere sphere = CreateSphere(spheres[i].x, spheres[i].y, spheres[i].z, spheres[i].w);
		IntersectSphere(ray, hit, sphere);

	}
	// Ground
	IntersectGroundPlane(ray, hit, vec3(0.0f, 0.0f, 0.0f), vec3(0.0f, 1.0f, 0.0f));
	//IntersectGroundPlane(ray, hit, vec3(0.0f, 0.0f, -22.0f), vec3(0.0f, 0.0f, 1.0f)); // Back wall
	//IntersectGroundPlane(ray, hit, vec3(22.0f, 0.0f, 0.0f), vec3(-1.0f, 0.0f, 0.0f)); // Right wall

	return hit;
}

vec3 Shade(inout Ray ray, RayHit hit, Light light)
{
	if (hit.distance < infinity)
	{
		// Reflection
		ray.position = hit.position + hit.normal * 0.001f;
		ray.direction = reflect(ray.direction, hit.normal);
		ray.energy *=  hit.specular;

		// Shadow
		Ray shadowRay = CreateRay(ray.position, -light.direction, ShadowRay);
		RayHit shadowHit = Trace(shadowRay);
		if (shadowHit.distance != infinity)
		{
			return vec3(0,0,0);
		}

		// Light
		vec3 L = -light.direction;

		// Map texture coordinates into a checkerboard pattern
		float x = mod(hit.tex.x, 1) > 0.5 ? 1 : 0;
		float y = mod(hit.tex.y, 1) > 0.5 ? 1 : 0;
		float pattern = x == y ? 1 : 0;

		float angle = dot(ray.direction, hit.normal) / length(ray.direction) * length(hit.normal);
		vec3 color = clamp(dot(hit.normal, L), 0.0f, 1.0f) * light.intensity * light.color * angle * hit.albedo;
		//vec3 color = hit.albedo * light.intensity * light.color * clamp(dot(hit.normal, L), 0.0f, 1.0f);

		vec3 black = clamp(dot(hit.normal, L), 0.0f, 1.0f) * light.intensity * vec3(0f, 0f, 0f) * angle * hit.albedo;
		return mix(color, black, pattern);
	}
	else
	{
		ray.energy = vec3(0,0,0);
		return vec3(0,0,0);
	}
}

void main()
{
	seed = _randseed;
	ivec2 pixel_coords = ivec2(gl_GlobalInvocationID.xy);
	ivec2 size = imageSize(img_output);

	if (pixel_coords.x >= size.x || pixel_coords.y >= size.y)
	{
		return;
	}

	// Create Ray
	// Relative position along viewbuffer (eg: 640th x pixel == 0.5)
	vec2 position = vec2(pixel_coords) / vec2(size.x, size.y);
	// interpolate between frustrum corners to get the direction of the pixel we want to trace through
	// This is for proper perspective
	vec3 dir = mix(mix(rayBottomLeft, rayTopLeft, position.y), mix(rayBottomRight, rayTopRight, position.y), position.x);
	
	Ray ray = CreateRay(camera, dir, LightRay);
	Light light = CreateLight(vec3(0.8f, 0.8f, 0.8f), 1.8f, vec3(1.0f, -1.0f, -1.0f));

	vec3 final_color = vec3(0,0,0);
	for (int i = 0; i < 8; i++)
	{
		RayHit hit = Trace(ray);
		final_color += ray.energy * Shade(ray,hit, light);

		if (!any(greaterThan(ray.energy, vec3(0,0,0))))
			break;
	}

	// output to a specific pixel in the image
	imageStore(img_output, pixel_coords, vec4(final_color, 1.0f));
}