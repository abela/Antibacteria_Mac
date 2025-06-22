//
//  Vehicle.mm
//  Tanks
//
//  Created by Giorgi Abelashvili on 11/17/10.
//  Copyright 2010 __MyCompanyName__. All rights reserved.
//

#import "Figure.h"

@implementation Figure

@synthesize sprite,body,flag,type;
@synthesize startPosition,lastPosition,tileMap,layer;
@synthesize batchNode, frameAnimationCounter;
@synthesize objectMovingFlag, currentDirection, collisionDirectionFlag;
@synthesize isCollided;
@synthesize objectSize;
@synthesize width;
@synthesize height;
//
//
- (id) init
{
    //
	if (self == [super init])
	{
		//create physics world
        physics = [Figure sharedPhysicsWorld];
		flag = kDefault;
	}
	return self;
}
//
//shared box2d physics world
static Physics* _physics = nil;
//
+(Physics*)sharedPhysicsWorld
{
    @synchronized([CCDirector class])
	{
		if (!_physics)
		{
			_physics = [[Physics alloc] init];
			return _physics;
		}
        return _physics;
	}
	// to avoid compiler warning
	return nil;
}

-(void)initAtPosition:(CGPoint)position
{
}

//create new body
- (void) createNewBodyAt:(CGPoint)position andDimension:(CGPoint)dimension
{
	b2BodyDef bodyDef;
	bodyDef.type = b2_dynamicBody;
    //
	bodyDef.position.Set(position.x, position.y);
	bodyDef.userData = self;
    bodyDef.allowSleep = false;
    //
    //create shape
    body = [[Figure sharedPhysicsWorld] getPhysicsWorld]->CreateBody(&bodyDef);
    //
    //body fixture.
	b2FixtureDef fixtureDef;
    //
    // Define another box shape for our dynamic body.
    b2PolygonShape dynamicBox;
	dynamicBox.SetAsBox(dimension.x, dimension.y);
	//
	fixtureDef.shape = &dynamicBox;
	fixtureDef.density = 1.0f;
	fixtureDef.friction = 0.3f;
	fixtureDef.isSensor = true;
	body->CreateFixture(&fixtureDef);
}

-(void) createNewGroundBodyAt:(CGPoint)position andDimension:(CGPoint)dimension
{
	b2BodyDef groundBodyDef;
	groundBodyDef.position.Set(0, 0); 
	groundBodyDef.userData = self;
	//craete body
	body = [physics getPhysicsWorld]->CreateBody(&groundBodyDef);
	
	b2EdgeShape groundBox;
	b2FixtureDef fixtureDef;
	//
	float x1 = position.x - TILE_WIDTH/2;
	float y1 = position.y + TILE_HEIGHT/2;
	//
	float x2 = position.x + TILE_WIDTH/2;
	float y2 = position.y + TILE_HEIGHT/2;
	//
	float x3 = position.x + TILE_WIDTH/2;
	float y3 = position.y - TILE_HEIGHT/2;
	//
	float x4 = position.x - TILE_WIDTH/2;
	float y4 = position.y - TILE_HEIGHT/2;
	//
	groundBox.Set(b2Vec2(x1, y1), b2Vec2(x2,y2));
	body->CreateFixture(&groundBox,1.0f);
	groundBox.Set(b2Vec2(x2, y2), b2Vec2(x3,y3));
	body->CreateFixture(&groundBox,1.0f);
	groundBox.Set(b2Vec2(x3, y3), b2Vec2(x4,y4));
	body->CreateFixture(&groundBox,1.0f);
	groundBox.Set(b2Vec2(x4, y4), b2Vec2(x1,y1));
	body->CreateFixture(&groundBox,1.0f);
	//
}
// update an sprite position
-(void)updatePhysicsBodyPosition:(CGPoint)position
{
	b2Vec2 b2Position = b2Vec2(position.x,position.y);
	float32 b2Angle = -1 * CC_DEGREES_TO_RADIANS(sprite.rotation);
	body->SetTransform(b2Position, b2Angle);
}
//
-(void) updateSpritePositionWithBodyPosition
{
	//
	b2Vec2 bodyPos = body->GetPosition();
	CGPoint newSpritePos = ccp(bodyPos.x*ASPECT_RATIO,bodyPos.y*ASPECT_RATIO);
	[sprite setPosition:newSpritePos];
	//
}
//
-(void)updateRotation:(float)rotation
{
    sprite.rotation = rotation;
}
//
-(void) resetBody
{
    body->SetActive(false);
    [[Figure sharedPhysicsWorld] getPhysicsWorld]->DestroyBody(body);
}
//
-(void)dealloc
{
	[super dealloc];
}

@end
