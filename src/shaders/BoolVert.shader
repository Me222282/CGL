#version 330 core

layout(location = locations.vertex) in vec3 vPosition;
layout(location = locations.texture) in vec2 texCoord;

out vec2 tex_Coords;

uniform mat4 matrix;

void main()
{
	tex_Coords = texCoord;

	gl_Position = matrix * vec4(vPosition, 1);
}