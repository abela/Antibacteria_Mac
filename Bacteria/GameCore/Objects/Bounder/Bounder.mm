//
//  Bounder.m
//  Turtle
//
//  Created by Giorgi Abelashvili on 4/8/11.
//  Copyright 2011 __MyCompanyName__. All rights reserved.
//

#import "Bounder.h"

@implementation Bounder
//
-(id)initAtPosition:(CGPoint)position andWithDimension:(CGPoint)dimension withBounderType:(ObjectType)objectType
{
	type = objectType;
	//
    b2BodyDef bodyDef;
	bodyDef.type = b2_staticBody;
    //
	bodyDef.position.Set(position.x*CC_CONTENT_SCALE_FACTOR(), position.y*CC_CONTENT_SCALE_FACTOR());
	bodyDef.userData = self;
    
    //create shape
    body = [[Figure sharedPhysicsWorld] getPhysicsWorld]->CreateBody(&bodyDef);
    //
    //body fixture.
	b2FixtureDef fixtureDef;
    //
    // Define another box shape for our dynamic body.
    b2PolygonShape dynamicBox;
    //
    dynamicBox.SetAsBox(dimension.x*CC_CONTENT_SCALE_FACTOR(), dimension.y*CC_CONTENT_SCALE_FACTOR());
	//
    //NSLog(@"bounder world center = %f,%f",body->GetWorldCenter().x,body->GetWorldCenter().y);
    //NSLog(@"bounder world point = %f,%f",body->GetWorldPoint(body->GetPosition()).x,body->GetWorldPoint(body->GetPosition()).y);
    
    //body->GetWorldPoint(body->GetPosition())
    //
	fixtureDef.shape = &dynamicBox;
    fixtureDef.density = 1.0f;
    fixtureDef.friction = 0.8f;
    fixtureDef.isSensor = false;
    //
	body->CreateFixture(&fixtureDef);
	//
	return self;
}
-(void) resetBounder
{
	[[Figure sharedPhysicsWorld] getPhysicsWorld]->DestroyBody(body);
}
@end
