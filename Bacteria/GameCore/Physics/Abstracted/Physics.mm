//
//  Physics.m
//  Funny Physics
//
//  Created by Giorgi Abelashvili on 9/2/10.
//  Copyright 2010 Innotec LLC. All rights reserved.
//

#import "Physics.h"
#import "cocos2d.h"


@implementation Physics

-(id)init
{
	if ([super init]) 
	{
		[self initPhysics];
	}
	return self;
}

-(void)initPhysics
{	
	// Define the gravity vector.
	b2Vec2 gravity;
	gravity.Set(0.0f, 0.0f);
	
	// Do we want to let bodies sleep?
	// This will speed up the physics simulation
	// Construct a world object, which will hold and simulate the rigid bodies.
	world = new b2World(gravity);
	
	world->SetContinuousPhysics(true);
    
	//set contact listeners
    world->SetContactListener(&contactListener);
    
    //set destruction listener
    world->SetDestructionListener(&destructionListener);
	
	// Debug Draw functions
	m_debugDraw = new GLESDebugDraw( PTM_RATIO );
    //set debug draw
	world->SetDebugDraw(m_debugDraw);
	
        
	uint32 flags = 0;
	flags += b2Draw::e_shapeBit;
	//		flags += b2DebugDraw::e_jointBit;
	//		flags += b2DebugDraw::e_aabbBit;
	//		flags += b2DebugDraw::e_pairBit;
	//		flags += b2DebugDraw::e_centerOfMassBit;
	m_debugDraw->SetFlags(flags);	
}

-(b2World*)getPhysicsWorld
{
	return world;
}

-(void)setPhysicsWorld:(b2World*)w
{
	world = w;
}

-(void)destroyBody:(b2Body*)body
{
    world->DestroyBody(body);
}

@end









