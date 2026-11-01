//
//  Agent.mm
//  Game
//
//  Created by Giorgi Abelashvili on 10/15/11.
//  Copyright 2011 __MyCompanyName__. All rights reserved.
//

#import "Agent.h"


@implementation Agent
//
@synthesize health;
@synthesize hasShield;
@synthesize armor;
@synthesize agentStateType;
@synthesize aiInput;
@synthesize speed;
@synthesize faceDirection;
//
-(id) init
{
	if((self = [super init]))
	{
		return self;
	}
	//
	else return nil;
	//
}
//
-(void) deffence
{
	
}
//
//
-(void) evade
{
}
//
-(BOOL) isSafe
{
	return YES;
}
//
-(void) attack
{
}
//
-(void) patrol
{
}
//
-(void) update
{
	
}
//
-(void) flee
{
	
}
//
-(void) randomfly
{
    
}
//
-(void) idle
{
    
}
//
-(void) fastEvade
{
    
}
//
- (void) createNewBodyAt:(CGPoint)position andDimension:(CGPoint)dimension
{
    //
	b2BodyDef bodyDef;
    //
    b2BodyDef bd;
    bd.position.Set(position.x, position.y);
    bd.type = b2_dynamicBody;
    bd.fixedRotation = false;
    bd.allowSleep = false;
	bd.userData = self;
    //
    body = [[Figure sharedPhysicsWorld] getPhysicsWorld]->CreateBody(&bd);
    //
    //
    b2CircleShape shape;
    shape.m_radius = dimension.x / 2.0f;
    //
    //
    b2FixtureDef fd;
    fd.shape = &shape;
    fd.density = 1.0f;
	fd.isSensor = true;
    body->CreateFixture(&fd);
}
//
-(void) releaseAgent
{
}
//
//
@end
