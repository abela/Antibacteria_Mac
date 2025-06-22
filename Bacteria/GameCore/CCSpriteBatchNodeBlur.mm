//
//  CCSpriteBatchNodeBlur.m
//  Bacterium
//
//  Created by Giorgi Abelashvili on 2/18/13.
//
//

#import "CCSpriteBatchNodeBlur.h"
#import "cocos2d.h"
//
@implementation CCSpriteBatchNodeBlur
//
@synthesize blendIsDisabled;
//
-(id)initWithTexture:(CCTexture2D *)tex capacity:(NSUInteger)capacity
{
	if( (self=[super initWithTexture:tex capacity:capacity]) ) {
		
		CGSize s = [self.texture contentSizeInPixels];
		
		blur_ = ccp(1/s.width, 1/s.height);
		sub_[0] = sub_[1] = sub_[2] = sub_[3] = 0;
		
		GLchar * fragSource = (GLchar*) [[NSString stringWithContentsOfFile:[[CCFileUtils sharedFileUtils] fullPathFromRelativePath:@"example_Blur.fsh"] encoding:NSUTF8StringEncoding error:nil] UTF8String];
		shaderProgram_ = [[CCGLProgram alloc] initWithVertexShaderByteArray:ccPositionTextureColor_vert fragmentShaderByteArray:fragSource];
		
		
		CHECK_GL_ERROR_DEBUG();
		
		[shaderProgram_ addAttribute:kCCAttributeNamePosition index:kCCVertexAttrib_Position];
		[shaderProgram_ addAttribute:kCCAttributeNameColor index:kCCVertexAttrib_Color];
		[shaderProgram_ addAttribute:kCCAttributeNameTexCoord index:kCCVertexAttrib_TexCoords];
		
		CHECK_GL_ERROR_DEBUG();
		
		[shaderProgram_ link];
		
		CHECK_GL_ERROR_DEBUG();
		
		[shaderProgram_ updateUniforms];
		
		CHECK_GL_ERROR_DEBUG();
		
		subLocation = glGetUniformLocation( shaderProgram_->program_, "substract");
		blurLocation = glGetUniformLocation( shaderProgram_->program_, "blurSize");
		
		CHECK_GL_ERROR_DEBUG();
	}
	
	return self;
}

-(void) draw
{
    if(blendIsDisabled == YES)
    {
        blendFunc_.src = 1;
        blendFunc_.dst = 771;
    }
    [super draw];
	CC_PROFILER_START(@"CCSpriteBatchNode - draw");
	
	// Optimization: Fast Dispatch
	if( textureAtlas_.totalQuads == 0 )
		return;
	
	[shaderProgram_ use];
	[shaderProgram_ setUniformForModelViewProjectionMatrix];
	[shaderProgram_ setUniformLocation:blurLocation withF1:blur_.x f2:blur_.y];
	[shaderProgram_ setUniformLocation:subLocation with4fv:sub_ count:1];
	
	
	[children_ makeObjectsPerformSelector:@selector(updateTransform)];
	
	ccGLBlendFunc( blendFunc_.src, blendFunc_.dst );
	
	[textureAtlas_ drawQuads];
	
	CC_PROFILER_STOP(@"CCSpriteBatchNode - draw");
}

-(void) setBlurSize:(CGFloat)f
{
	CGSize s = [self.texture contentSizeInPixels];
	
	blur_ = ccp(1/s.width, 1/s.height);
	blur_ = ccpMult(blur_,f);
}


@end
