//
//  CCSpriteBatchNodeBlur.h
//  Bacterium
//
//  Created by Giorgi Abelashvili on 2/18/13.
//
//

#import "CCSpriteBatchNode.h"

@interface CCSpriteBatchNodeBlur : CCSpriteBatchNode
{
	CGPoint blur_;
	GLfloat	sub_[4];
	
	GLuint	blurLocation;
	GLuint	subLocation;
    
    BOOL blendIsDisabled;
}
@property (nonatomic,assign) BOOL blendIsDisabled;
-(void) setBlurSize:(CGFloat)f;
@end
