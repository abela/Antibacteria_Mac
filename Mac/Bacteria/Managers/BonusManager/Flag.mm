//
//  Flag.m
//  AntiBacteria
//
//  Created by Giorgi Abelashvili on 5/12/13.
//
//

#import "Flag.h"
#import "GameResourceManager.h"

@implementation Flag
-(id) initWithType:(BonusObjectType)bonusType andPosition:(CGPoint)position
{
    if((self = [super initWithType:bonusType andPosition:position]))
    {
        type = kBonusObject;
        flag = kDefault;
        bonusObjectType = bonusType;
        sprite = [CCSprite spriteWithFile:@"bonus.png"];
        sprite.position = position;
        agentStateType = kStateWaiting;
        width = 1.3;
		height = 1.3f;
		//
		position.x /= ASPECT_RATIO;
		position.y /= ASPECT_RATIO;
		//
        [self createNewBodyAt:position andDimension:ccp(width,height)];
        return self;
    }
    return nil;
}
//
//create new body
- (void) createNewBodyAt:(CGPoint)position andDimension:(CGPoint)dimension
{
	b2BodyDef bodyDef;
	bodyDef.type = b2_staticBody;
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
//
-(void) update
{
    [super update];
    agentStateType = kStateWaiting;
}
//
-(void) releaseAgent
{
    [super releaseAgent];
}
//
@end
