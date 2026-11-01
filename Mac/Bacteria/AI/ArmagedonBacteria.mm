//
//  ArmagedonBacteria.m
//  AntiBacteria
//
//  Created by Giorgi Abelashvili on 5/27/13.
//
//
//
#import "ArmagedonBacteria.h"
#import "SpawnManager.h"
#import "Utils.h"
#import "GameResourceManager.h"
//
@implementation ArmagedonBacteria
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
		enemyAgentType = kArmagedonBacteria;
		speed = 8.0f;
		fleeSpeed = 5.0f;
		shootRate = 0.5f;
		shootDistance = 100;
		fleeDistance = 70.0f;
		faceDirection = ccp(0.0f,1.0f);
        hitCount = 1;
        killScore = 50;
		//
		width = 2.0f;
		height = 2.0f;
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
    randomMovePoint = [[SpawnManager sharedSpawnManager] getRandomPointAroundPoint:ccp(randX,randY)];
    //
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
    if(hitCount <= 0)
    {
        [[VitaminManager sharedVitaminManager] createVitaminAtPosition:sprite.position withScore:1];
    }
    //
	[super releaseAgent];
}
//
@end
