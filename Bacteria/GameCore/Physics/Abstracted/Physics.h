//
//  Physics.h
//  Funny Physics
//
//  Created by Giorgi Abelashvili on 9/2/10.
//  Copyright 2010 Innotec LLC. All rights reserved.
//

#import "Box2D.h"
#import "cocos2d.h"
#import "GLES-Render.h"

#import "ContactListener.h"

#import "DestructionListener.h"


#define PTM_RATIO 32


@interface Physics : NSObject {

	b2World* world;
    
    
	GLESDebugDraw *m_debugDraw;
    
    
    DestructionListener destructionListener;
    
    ContactListener contactListener;
	
}

-(void)initPhysics;

-(b2World*)getPhysicsWorld;

-(void)destroyBody:(b2Body*)body;

-(void)setPhysicsWorld:(b2World*)w;

@end
