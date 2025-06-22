//
//  RushStation.m
//  AntiBacteria
//
//  Created by Giorgi Abelashvili on 5/13/13.
//
//

#import "RushStation.h"
#import "GameModeManager.h"
#import "Utils.h"
//
#define DEFAULT_OPACITY         55.0f
//
@interface RushStation (PrivateMethods)
-(void) createLifeSprite;
@end
//
@implementation RushStation
@synthesize characterIsIn;
@synthesize lifeProgressTimer;
//
-(id) initWithType:(BonusObjectType)bonusType andPosition:(CGPoint)position
{
    if((self = [super initWithType:bonusType andPosition:position]))
    {
        type = kRushStation;
        flag = kDefault;
        bonusObjectType = bonusType;
        sprite = [CCSprite spriteWithFile:@"bonus.png"];
        sprite.scale = 5.0f;
        sprite.position = position;
        agentStateType = kStateFlee;
        width = height = 5.0;
		sprite.opacity = DEFAULT_OPACITY;
        opacityCounterStep = (float)(255.0f - sprite.opacity) / 100.0f / 2.0f;
        //
		position.x /= ASPECT_RATIO;
		position.y /= ASPECT_RATIO;
		//
        [self createNewBodyAt:position andDimension:ccp(width,height)];
        // self createLife Spriet
        [self createLifeSprite];
        return self;
    }
    return nil;
}
//
-(void) createLifeSprite
{
    CCSprite *timerSprite = [CCSprite spriteWithFile:@"bonus.png"];
    //
    lifeProgressTimer = [CCProgressTimer progressWithSprite:timerSprite];
    //
    [lifeProgressTimer setPosition:ccp(sprite.position.x,sprite.position.y + 1.2f *sprite.boundingBox.size.height / 2.0f)];
    [lifeProgressTimer setType:kCCProgressTimerTypeRadial];
    //
    [lifeProgressTimer setPercentage:0];
}
//
//create new body
- (void) createNewBodyAt:(CGPoint)position andDimension:(CGPoint)dimension
{
	//
	b2BodyDef bodyDef;
    //
    b2BodyDef bd;
    bd.position.Set(position.x, position.y);
    bd.type = b2_staticBody;
    bd.fixedRotation = false;
    bd.allowSleep = false;
    bd.userData = self;
	//
    body = [[Figure sharedPhysicsWorld] getPhysicsWorld]->CreateBody(&bd);
    //
    b2CircleShape shape;
    shape.m_radius = dimension.x;
    //
    b2FixtureDef fd;
    fd.shape = &shape;
    fd.density = 1.0f;
    fd.isSensor = true;
    body->CreateFixture(&fd);
    //
}
//
-(void) update
{
    [super update];
    agentStateType = kStateWaiting;
    //
    if(characterIsIn)
    {
        //
        float percentage = lifeProgressTimer.percentage;
        percentage+=0.5f * ([Utils timeScale]);
        if(lifeProgressTimer.percentage >= 100.0f)
        {
            lifeProgressTimer.percentage = 100;
            flag = kDealloc;
        }
        else lifeProgressTimer.percentage = percentage;
        //
    }
    //
    else
    {
        //
        float percentage = lifeProgressTimer.percentage;
        percentage-=0.5f * ([Utils timeScale]);
        if(lifeProgressTimer.percentage <= 0 )
            lifeProgressTimer.percentage = 0;
        else lifeProgressTimer.percentage = percentage;
        //
    }
    //
}
//
-(void) releaseAgent
{
    [super releaseAgent];
    [lifeProgressTimer removeFromParentAndCleanup:YES];
    [[GameModeManager sharedGameModeManager] canGenerateBonus:YES];
    [[GameManager sharedGameManager] updateLScoreWithValue:RUSH_CONS_POINT];
}
//

@end
