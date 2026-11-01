//
//  Agent.h
//  Game
//
//  Created by Giorgi Abelashvili on 10/15/11.
//  Copyright 2011 __MyCompanyName__. All rights reserved.
//

#import <Foundation/Foundation.h>
#import "Figure.h"

//
@class AiInput;
//
@interface Agent : Figure {

	AgentStateType agentStateType;
	float health;
	float armor;
	float speed;
	BOOL hasShield;
	AiInput *aiInput;
	CGPoint faceDirection;
}
//
@property (nonatomic,assign) float speed;
@property (nonatomic,assign) float health;
@property (nonatomic,assign) float armor;
@property (nonatomic,assign) BOOL hasShield;
@property (nonatomic,assign) CGPoint faceDirection;
@property (nonatomic,assign) AgentStateType agentStateType;
@property (nonatomic,assign) AiInput *aiInput;
//
-(void) evade;
-(void) fastEvade;
-(BOOL) isSafe;
-(void) attack;
-(void) patrol;
-(void) deffence;
-(void) update;
-(void) flee;
-(void) idle;
-(void) randomfly;
-(void) releaseAgent;
@end
