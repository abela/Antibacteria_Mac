//
//  CharacterController.h
//  Tanks
//
//  Created by Eaymon Latif on 11/22/10.
//  Copyright 2010 fvpi llc. All rights reserved.
//
//
#import <Foundation/Foundation.h>
#import "cocos2d.h"
#define MOVEMENT_SPEED 2
//
@class MainGameLayer;
@class Figure;
@interface CharacterController : NSObject {
	MainGameLayer *gameLayer;
	Figure * character;
}

@property (nonatomic, retain) MainGameLayer *gameLayer;
@property (nonatomic, retain) Figure *character;

- (id)initWithgameLayer:(MainGameLayer*)g;
//
- (void) createCharacter;
//
- (void) updateCharacterWith:(ObjectMovingFlag)direction;
//
- (ObjectMovingFlag) updatePosition;
//
- (int) shoot;
//
-(void) Die;
//
- (void)resetCharacter;
//
@end
