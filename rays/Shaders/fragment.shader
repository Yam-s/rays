#version 430 core

in vec4 position;
out vec4 fColor;

uniform sampler2D atexture;
uniform float width;
uniform float height;

void main()
{
	fColor = texture(atexture, gl_FragCoord.xy / vec2(width, height));
}