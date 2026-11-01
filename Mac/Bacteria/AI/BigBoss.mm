//
//  BigBoss.mm
//  Game
//
//  Created by Giorgi Abelashvili on 1/15/12.
//  Copyright 2012 __MyCompanyName__. All rights reserved.
//
//
#import "BigBoss.h"
#import "AiInput.h"
#import "Utils.h"
#import "GameResourceManager.h"
#import "SpawnManager.h"
#import "GameManager.h"
//
@implementation BigBoss
//
//
-(id) createAtPosition:(CGPoint)position
{
	//
	if((self = [super createAtPosition:position]))
	{
		//
        flag = kDefault;
		agentStateType = kStateWaiting;
		type = kEnemy;
		enemyAgentType = kBigBoss;
		speed = 17.0f;
        //
        switch ([[GameSettingsManager sharedGameSettingsManager] gameDifficulty]) {
            case kHard:
                speed = 20.0f;
                break;
            case kImpossible:
                speed = 23.0f;
                break;
            default:
                break;
        }
		fleeSpeed = 5.0f;
		shootRate = 0.5f;
		shootDistance = 100;
		fleeDistance = 70.0f;
		faceDirection = ccp(0.0f,1.0f);
        hitCount = 255;
		//
		width = 13.55f;
		height = 13.55f;
        vitaminsCount = 10;
		//
		sprite = [CCSprite spriteWithSpriteFrameName:@"Enemy3.png"];
        glowSprite = [CCSprite spriteWithSpriteFrameName:@"Shine3.png"];
		//
		glowSprite.position = sprite.position = position;
		//
		position.x /= ASPECT_RATIO;
		position.y /= ASPECT_RATIO;
		//
		[super createNewBodyAt:position andDimension:ccp(width,height)];
		//
        sprite.opacity = 0.0f;
        sprite.scale = glowSprite.scale = 10.0f;
        body->SetActive(false);
        //
		[[[GameResourceManager sharedGameResourceManager] sharedMainCharacterSpriteSheet] addChild:sprite z:1 tag:1];
        [[[GameResourceManager sharedGameResourceManager] sharedMainCharacterSpriteSheet] addChild:glowSprite z:0 tag:1];
		//
        CCFadeIn *fadeIn = [CCFadeIn actionWithDuration:0.1f];
        CCCallFunc *fadeInHasDone = [CCCallFunc actionWithTarget:self selector:@selector(creatingAnimationHasEnd:)];
        [sprite runAction:[CCSequence actions:fadeIn,fadeInHasDone, nil]];
        //
        body->GetFixtureList()->SetSensor(true);
        //
		return self;
	}
	//
	return nil;
}
//
-(void) generateNextRandomPoint
{
    //
    int randX = [Utils getRandomNumber:[[CoreSettings sharedCoreSettings] upperLeft].x
                                    to:[[CoreSettings sharedCoreSettings] upperRight].x];
    //
    int randY = [Utils getRandomNumber:[[CoreSettings sharedCoreSettings] bottomRight].y
                                    to:[[CoreSettings sharedCoreSettings] upperRight].y];
    //
    randomMovePoint = (arc4random_uniform(3) == 1) ?
    [[AiInput sharedAiInput] mainCharacterPosition] :
    [[SpawnManager sharedSpawnManager] getRandomPointAroundPoint:ccp(randX,randY)];
}
//
-(void) creatingAnimationHasEnd:(id)sender
{
    [super creatingAnimationHasEnd:sender];
    [self generateNextRandomPoint];
    agentStateType = kStateRandomMovement;
    [sprite runAction:[CCRepeatForever actionWithAction:[CCRotateBy actionWithDuration:0.3f angle:90.0f]]];
}
//
-(void) update
{
	[super update];
}
//
-(void) randomfly
{
    [super randomfly];
}
//
-(void) moveToPosition:(CGPoint)position
{
    [super moveToPosition:position];
    if(ccpDistance(sprite.position, randomMovePoint) < 10.0f)
        [self generateNextRandomPoint];
}
//
-(void) releaseAgent
{
    //
    if(hitCount<=0)
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
-(void) hit
{
    [super hit];
    sprite.color = ccc3(hitCount, hitCount, hitCount);
    if(flag == kDealloc)
       [[GameManager sharedGameManager] levelHasWonAfterBigBoss];
}
//

@end
