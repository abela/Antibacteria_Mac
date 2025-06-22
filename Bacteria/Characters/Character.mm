//
//  Character.mm
//  SaveThePig
//
//  Created by Giorgi Abelashvili on 6/11/11.
//  Copyright 2011 __MyCompanyName__. All rights reserved.
//

#import "Character.h"


@implementation Character
//
@synthesize currentSpeed;
//
-(void) initAtPosition:(CGPoint)position
{
	throw [NSException exceptionWithName:@"Ovveriding!" reason:@"Must Oveeride!" userInfo:nil];
}
//
-(void) updateCharacterPosWithNewPosition:(CGPoint)position
{
	throw [NSException exceptionWithName:@"Ovveriding!" reason:@"Must Oveeride!" userInfo:nil];
}
//
-(void) moveCharacterOnXWithDelta:(float)delta
{
	throw [NSException exceptionWithName:@"Ovveriding!" reason:@"Must Oveeride!" userInfo:nil];
}
//
-(void) moveCharacterOnYWithDelta:(float)delta
{
	throw [NSException exceptionWithName:@"Ovveriding!" reason:@"Must Oveeride!" userInfo:nil];
}
//
-(void) jump
{
	throw [NSException exceptionWithName:@"Ovveriding!" reason:@"Must Oveeride!" userInfo:nil];
}
//
-(void) Die
{
    throw [NSException exceptionWithName:@"Ovveriding!" reason:@"Must Oveeride!" userInfo:nil];
}
//
-(void) runWithSpeed:(float)speed
{
	throw [NSException exceptionWithName:@"Ovveriding!" reason:@"Must Oveeride!" userInfo:nil];
}
//
-(void) decideCharacterActionWithFlag:(CharacterDecisionFlag) characterDesisionFlag
{
    throw [NSException exceptionWithName:@"Ovveriding!" reason:@"Must Oveeride!" userInfo:nil]; 
}
//
-(void) reset
{
	throw [NSException exceptionWithName:@"Ovveriding!" reason:@"Must Oveeride!" userInfo:nil];
}
//
@end
