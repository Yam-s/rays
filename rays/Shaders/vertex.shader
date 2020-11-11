#version 430 core

layout (location=0) in vec4 vPosition;

out vec4 position;

void main()
{
	gl_Position = vPosition;
	position = gl_Position;
}