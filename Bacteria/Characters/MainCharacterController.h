//
//  MainCharacterController.h
//  GeorgianTale
//
//  Created by Giorgi Abelashvili on 5/9/11.
//  Copyright 2011 __MyCompanyName__. All rights reserved.
//

#import <Foundation/Foundation.h>
#import "CharacterController.h"
#import "KeyBind.h"
#include <vector>
using namespace std;

@interface MainCharacterController : CharacterController {

    CGPoint startPos;
    
}
//
+(MainCharacterController*) sharedCharacterController;
//
-(void) updateCharacterWithPosition:(CGPoint)position;
//
-(void) simulateCharacterRestartAtPosition:(CGPoint)position;
//
-(void) resetCharacterBody;
//
-(void) releaseCharacter;
//
-(void) moveCharacterToPosition:(CGPoint)position;
//
-(CGPoint) moveCharacterWithVelocity:(CGPoint)velocity;
//
-(void) moveCharacterWithKeys:(int[])keys :(int[])shootKeys;
//
-(void) createCharacterWithDictionary:(NSMutableDictionary*)levelData;
//
-(void) rotateCharacterWithMousePos:(CGPoint)mousePos;
//
-(void) moveCharacterToMousePositions:(CGPoint)mousePositions;
//
-(float) rotateCharacter;
//
-(void) startShoot;
//
-(void) endShoot;
//
-(void) updateCharacterRotation:(float)rotation andDirection:(CGPoint)direction;
//
-(void) update;
//
-(void) pause;
//
-(void) resume;
//
-(void) refresh;
//
-(void) updateCharacterSettingsAfterLevel;
//
@end
