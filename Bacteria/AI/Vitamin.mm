//
//  Vitamin.m
//  AntiBacteria
//
//  Created by Giorgi Abelashvili on 6/20/13.
//
//

#import "Vitamin.h"
#import "GameResourceManager.h"
#import "Utils.h"
#import "AiInput.h"
#import "GameManager.h"
//
#define VITAMIN_GET_DISTANCE            500
//
@interface Vitamin (PrivateMethods)
-(void) runToCharacter;
@end
//
@implementation Vitamin
//
@synthesize vitaminScore;
//
-(id) createAtPosition:(CGPoint)position withScore:(int)score
{
	//
	if((self = [super createAtPosition:position]))
	{
        //
        vitaminScore = score;
        flag = kDefault;
		agentStateType = kStateWaiting;
		type = kVitamin;
		speed = 40.0f;
		shootRate = 0.5f;
		shootDistance = 100;
		faceDirection = ccp(0.0f,1.0f);
        hitCount = 1;
        killScore = 80;
		//
		width = 0.6f;
		height = 0.6f;
		//
		sprite = [CCSprite spriteWithSpriteFrameName:@"WX_circle_white.png"];
        //
		sprite.position = position;
        sprite.scale = 0.3f;
		//
		position.x /= ASPECT_RATIO;
		position.y /= ASPECT_RATIO;
		//
        [super createNewBodyAt:position andDimension:ccp(width,height)];
		//
		//[sprite setColor:ccc3(arc4random()%255,arc4random()%255,arc4random()%255)];
        body->GetFixtureList()->SetSensor(true);
        //
        [[[GameResourceManager sharedGameResourceManager] sharedVitaminsSpriteSheet] addChild:sprite z:1 tag:1];
        [sprite runAction:[CCRepeatForever actionWithAction:[CCRotateBy actionWithDuration:0.1f angle:90.0f]]];
		//
		return self;
	}
	//
	return nil;
}
//
-(void) runToCharacter
{
    CGPoint vector = [[Utils sharedUtils] vectorBetweenTwoPoint:sprite.position :[[AiInput sharedAiInput] mainCharacterPosition]];
    float vectorLength = ccpLength(vector);
    if(vectorLength <= VITAMIN_GET_DISTANCE && [[GameManager sharedGameManager] characterIsDead] == NO)
        agentStateType = kStatePatrol;
}
//
-(void) update
{
    [self runToCharacter];
	[super update];
}
//
-(void) patrol
{
	[super patrol];
}
//
-(void) attack
{
	[super attack];
}
//
-(void) releaseAgent
{
	[super releaseAgent];
}
//

@end
