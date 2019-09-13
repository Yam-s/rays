#version 450 core

layout (location=0) in vec4 vPosition;

out vec4 position;

uniform mat4 MVP;

void main()
{
	gl_Position = vPosition;
	position = gl_Position;
}