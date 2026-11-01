//
//  MainCharacterController.mm
//  GeorgianTale
//
//  Created by Giorgi Abelashvili on 5/9/11.
//  Copyright 2011 __MyCompanyName__. All rights reserved.
//

#import "MainCharacterController.h"
#import "MainCharacter.h"
#import "KeyBind.h"
//
static MainCharacterController *_sharedCharacterController = nil;
//
@implementation MainCharacterController
//
//
-(id) initWithgameLayer:(MainGameLayer *)g
{
	//
	gameLayer = g;
	//
    CGPoint startInitPos = CGPointZero;
    //
    float bcPosX = [CCDirector sharedDirector].winSize.width / 2.0f;
    float bcPosY = [CCDirector sharedDirector].winSize.height / 2.0f;
    //
    //
    startInitPos = CGPointMake(bcPosX, bcPosY);
    //
    startPos = startInitPos;
    character = [[MainCharacter alloc] createAtPosition:startPos withGameLayer:gameLayer];
    //
	return _sharedCharacterController = self;
	//
}
//
+(MainCharacterController*) sharedCharacterController
{
    return _sharedCharacterController;
}
//
-(void) createCharacterWithDictionary:(NSMutableDictionary*)levelData
{
    
}
//
-(void) decideCharacterActionWithFlag:(CharacterDecisionFlag) characterDesisionFlag
{
    
}
//
-(void) moveCharacterToMousePositions:(CGPoint)mousePositions
{
    [(MainCharacter*)character moveCharacterToMousePositions:mousePositions];
}
//
-(void) moveCharacterWithKeys:(int[])keys :(int[])shootKeys
{
    [(MainCharacter*)character moveCharacterWithKeys:keys :shootKeys];
}
//
-(void) rotateCharacterWithMousePos:(CGPoint)mousePos
{
    [(MainCharacter*)character rotateCharacterWithMousePos:mousePos];
}
//
-(void) moveCharacterToPosition:(CGPoint)position
{
    [(MainCharacter*)character moveCharacterWithPosition:position];
}
//
//
-(void) updateCharacterWithPosition:(CGPoint)position
{
    [(Character*)character updateCharacterPosWithNewPosition:position];
}
//
-(void) simulateCharacterRestartAtPosition:(CGPoint)position
{
    [(MainCharacter*)character simulateCharacterRestartAtPosition:startPos];
}
//
//
-(CGPoint) moveCharacterWithVelocity:(CGPoint)velocity
{
	return [(MainCharacter*)character moveCharacterWithVelocity:velocity];
}
//
-(void) startShoot
{
	//
	if([(MainCharacter*)character shooting] == NO)
	{
		[(MainCharacter*)character setShooting:YES];
		[(MainCharacter*)character startShoot];
	}
}
//
-(void) updateCharacterSettingsAfterLevel
{
    [(MainCharacter*)character updateCharacterSettingsAfterLevel];
}
//
-(void) updateCharacterRotation:(float)rotation andDirection:(CGPoint)direction
{
	[(MainCharacter*)character setCharacterRotation:rotation];
	[(MainCharacter*)character setCharacterDirection:direction];
}
//
-(void) endShoot
{
    [(MainCharacter*)character endShoot];
}
//
-(float) rotateCharacter
{
	return 0.0f;
}
//
//
-(void) resetCharacterBody
{
    [(MainCharacter*)character resetBody];
}
//
-(void) releaseCharacter
{
    [(MainCharacter*)character releaseCharacter];
    [character release];
    character = nil;
}
//
-(void) pause
{
    [(MainCharacter*)character pause];
}
//
-(void) resume
{
    [(MainCharacter*)character resume];
}
//
-(void) update
{
	[(MainCharacter*)character update];
}
//
-(void) refresh
{
    [(MainCharacter*)character refresh];
}
//
-(void) Die
{
    [self resetCharacterBody];
    character.sprite.opacity = 0;
}
//
@end
