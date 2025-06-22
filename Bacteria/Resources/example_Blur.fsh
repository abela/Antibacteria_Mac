// Shader taken from: http://webglsamples.googlecode.com/hg/electricflower/electricflower.html

#ifdef GL_ES
precision mediump float;
#endif

varying vec4 v_fragmentColor;
varying vec2 v_texCoord;

uniform sampler2D CC_Texture0;

uniform vec2 blurSize;
uniform vec4 substract;

void main() {
	vec4 sum = vec4(0.0);
	sum += texture2D(CC_Texture0, (v_texCoord - 3.4 * blurSize)) * 0.016216216;
	sum += texture2D(CC_Texture0, (v_texCoord - 2.4 * blurSize)) * 0.054054054;
	sum += texture2D(CC_Texture0, (v_texCoord - 1.4 * blurSize)) * 0.126126126;
	sum += texture2D(CC_Texture0, (v_texCoord - 0.5 * blurSize)) * 0.194594594;
	sum += texture2D(CC_Texture0, (v_texCoord                 )) * 0.227022270;
	sum += texture2D(CC_Texture0, (v_texCoord + 0.5 * blurSize)) * 0.194594594;
	sum += texture2D(CC_Texture0, (v_texCoord + 1.4 * blurSize)) * 0.126126126;
	sum += texture2D(CC_Texture0, (v_texCoord + 2.4 * blurSize)) * 0.054054054;
	sum += texture2D(CC_Texture0, (v_texCoord + 3.4 * blurSize)) * 0.016216216;

	gl_FragColor = (sum - substract) * v_fragmentColor;
}

