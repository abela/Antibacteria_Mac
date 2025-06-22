//
//  Character.h
//  SaveThePig
//
//  Created by Giorgi Abelashvili on 6/11/11.
//  Copyright 2011 __MyCompanyName__. All rights reserved.
//

#import <Foundation/Foundation.h>
#import "Figure.h"

@interface Character : Figure {
	float currentSpeed;
}
//
@property (nonatomic,assign) float currentSpeed;
//
-(void) updateCharacterPosWithNewPosition:(CGPoint)position;
//
-(void) decideCharacterActionWithFlag:(CharacterDecisionFlag) characterDesisionFlag;
//
-(void) moveCharacterOnXWithDelta:(float)delta;
//
-(void) moveCharacterOnYWithDelta:(float)delta;
//
-(void) jump;
//
-(void) runWithSpeed:(float)speed;
//
-(void) Die;
//
-(void) reset;
//
@end
