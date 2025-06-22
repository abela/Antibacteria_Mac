//
//  TestEnemy1.mm
//  Game
//
//  Created by Giorgi Abelashvili on 10/22/11.
//  Copyright 2011 __MyCompanyName__. All rights reserved.
//

#import "RedNoise.h"
#import "GameResourceManager.h"
//
@implementation RedNoise
//
@synthesize bornFromBoss;
//
-(id) createAtPosition:(CGPoint)position
{	
	//
	if((self = [super createAtPosition:position]))
	{
        //
        flag = kDefault;
		agentStateType = kStatePatrol;
		type = kEnemy;
		enemyAgentType = kRedNoise;
		speed = 10.0f;
		shootRate = 0.5f;
		shootDistance = 100;
		faceDirection = ccp(0.0f,1.0f);
        hitCount = 1;
        killScore = 80;
        vitaminsCount = 3;
		//
		width = 2.0f;
		height = 2.0f;
		//
		sprite = [CCSprite spriteWithSpriteFrameName:@"Enemy1.png"];
        //
		sprite.position = position;
		//
		position.x /= ASPECT_RATIO;
		position.y /= ASPECT_RATIO;
		//
        [super createNewBodyAt:position andDimension:ccp(width,height)];
		//
		[sprite setColor:ccc3(255,0,0)];
        //
        [[[GameResourceManager sharedGameResourceManager] sharedMainCharacterSpriteSheet] addChild:sprite z:1 tag:1];
		//
		return self;
	}
	//
	return nil;
}
//
-(void) update
{
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
    if(hitCount <= 0 && bornFromBoss == NO)
    {
        for (int i =0; i<vitaminsCount; i++) {
            CGPoint randomPos = [[VitaminManager sharedVitaminManager] getValidBonusSpawnPointAtPoint:sprite.position];
            [[VitaminManager sharedVitaminManager] createVitaminAtPosition:randomPos withScore:1];
        }
    }
    //
	[super releaseAgent];
}
//
@end
