//
//  Bullet.mm
//  Game
//
//  Created by Giorgi Abelashvili on 10/19/11.
//  Copyright 2011 __MyCompanyName__. All rights reserved.
//

#import "Bullet.h"
#import "GameResourceManager.h"

@implementation Bullet
//
@synthesize speed;
@synthesize damage;
@synthesize bulletType;
@synthesize bulletDelta;
@synthesize glowSprite;
@synthesize parentObject;
@synthesize tag;
//
-(id) initAtPosition:(CGPoint)position
         andRotation:(float)angle
        andDirection:(CGPoint)direction
andCharacterVelocity:(CGPoint)characterVelocity
{
    type = kBullet;
    damage = 1.0f;
    speed = 40.0f;
    bulletDelta = 40.0f;
    sprite = [CCSprite spriteWithSpriteFrameName:@"bullet.png"];
    glowSprite = [CCSprite spriteWithSpriteFrameName:@"Shine1.png"];
    glowSprite.position = position;
    //
    sprite.rotation = angle-90.0f;
    glowSprite.rotation = angle - 90.0f;
    glowSprite.scale = 0.5f;
    sprite.scale = 1.5f;
    //
    CGPoint shootDelta = ccpMult(direction, bulletDelta);
    position = ccpAdd(shootDelta, position);
    //
    sprite.position = position;
    //
    position.x /= ASPECT_RATIO;
    position.y /= ASPECT_RATIO;
    //
    [self createNewBodyAt:position andDimension:ccp(0.0005f,0.0005f)];
    //
    float length = sqrt(pow(direction.x,2) + pow(direction.y,2));
    speed = speed / length;
    //
    //
    body->SetLinearVelocity(b2Vec2(direction.x*speed,direction.y*speed));
    //
    [[[GameResourceManager sharedGameResourceManager] sharedMainCharacterSpriteSheet] addChild:sprite z:5];
    [[[GameResourceManager sharedGameResourceManager] sharedMainCharacterSpriteSheet] addChild:glowSprite z:6];
    //
	return self;
}
//
-(id) initAtPosition:(CGPoint)position
         andRotation:(float)angle
        andDirection:(CGPoint)direction
       andStartDelta:(float)startDelta
{
    type = kBullet;
    damage = 1.0f;
    speed = 30.0;
    bulletDelta = startDelta;
    sprite = [CCSprite spriteWithSpriteFrameName:@"bullet.png"];
    glowSprite = [CCSprite spriteWithSpriteFrameName:@"Shine1.png"];
    glowSprite.position = position;
    //
    sprite.rotation = angle-90.0f;
    glowSprite.rotation = angle - 90.0f;
    glowSprite.scale = 0.5f;
    sprite.scale = 1.0f;
    //
    CGPoint shootDelta = ccpMult(direction, bulletDelta);
    position = ccpAdd(shootDelta, position);
    //
    sprite.position = position;
    //
    position.x /= ASPECT_RATIO;
    position.y /= ASPECT_RATIO;
    //
    [self createNewBodyAt:position andDimension:ccp(0.1f,0.1f)];
    //
    float length = sqrt(pow(direction.x,2) + pow(direction.y,2));
    speed = speed / length;
    //
    body->SetLinearVelocity(b2Vec2(direction.x*speed,direction.y*speed));
    //
    [[[GameResourceManager sharedGameResourceManager] sharedMainCharacterSpriteSheet] addChild:sprite z:5];
    [[[GameResourceManager sharedGameResourceManager] sharedMainCharacterSpriteSheet] addChild:glowSprite z:6];
    //
	return self;
}

//
//create new body
- (void) createNewBodyAt:(CGPoint)position andDimension:(CGPoint)dimension
{
    //
	b2BodyDef bodyDef;
	bodyDef.type = b2_dynamicBody;
    //
	bodyDef.position.Set(position.x, position.y);
	bodyDef.userData = self;
    bodyDef.allowSleep = false;
    bodyDef.bullet = true;
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
	fixtureDef.isSensor = false;
	body->CreateFixture(&fixtureDef);
}
//
-(void) updateSpritePositionWithBodyPosition
{
	//
	b2Vec2 bodyPos = body->GetPosition();
	CGPoint newSpritePos = ccp(bodyPos.x*ASPECT_RATIO,bodyPos.y*ASPECT_RATIO);
	glowSprite.position = sprite.position = newSpritePos;
	//
}
//
-(void) bulletLifeCycleTimer:(NSTimer*)timer
{
	flag = kDealloc;
}
//
-(void) resetBody
{
    [[Figure sharedPhysicsWorld] getPhysicsWorld]->DestroyBody(body);
}
//
@end
