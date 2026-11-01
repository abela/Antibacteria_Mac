//
//  Chorux.m
//  Bacterium
//
//  Created by Giorgi Abelashvili on 3/5/13.
//
//

#import "Chorux.h"
#import "GameResourceManager.h"
#import "Utils.h"
#import "SpawnManager.h"
#import "ChoruxDefender.h"
#import "AiInput.h"
//
#define CHORUX_DEFENDERS_COUNT              2
//
@interface Chorux (PrivateMethods)
-(void) updateDeffenders;
@end
//
@implementation Chorux
//
-(id) createAtPosition:(CGPoint)position
{
	//
    CGPoint localPos = position;
    //
	if((self = [super createAtPosition:localPos]))
	{
        //
        flag = kDefault;
		agentStateType = kStateWaiting;
		type = kEnemy;
		enemyAgentType = kChorux;
		speed = 6.0f;
		shootRate = 0.5f;
		shootDistance = 100;
		faceDirection = ccp(0.0f,1.0f);
        hitCount = 1;
        killScore = 200;
        vitaminsCount = 3;
        //
        CCFadeIn *fadeIn = [CCFadeIn actionWithDuration:0.1f];
        CCCallFunc *fadeInHasDone = [CCCallFunc actionWithTarget:self selector:@selector(creatingAnimationHasEnd:)];
        [sprite runAction:[CCSequence actions:fadeIn,fadeInHasDone, nil]];
        //
        for(int i =0;i<CHORUX_DEFENDERS_COUNT;i++)
        {
            ChoruxDefender *defender = [[ChoruxDefender alloc] createAtPosition:position];
            defender.targetToDeffend = self;
            defenders.push_back(defender);
        }
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
    int randX = [Utils getRandomNumber:[[CoreSettings sharedCoreSettings] upperLeft].x
                                    to:[[CoreSettings sharedCoreSettings] upperRight].x];
    //
    int randY = [Utils getRandomNumber:[[CoreSettings sharedCoreSettings] bottomRight].y
                                    to:[[CoreSettings sharedCoreSettings] upperRight].y];
    randomMovePoint = [[SpawnManager sharedSpawnManager] getRandomPointAroundPoint:ccp(randX,randY)];
}
//
-(void) creatingAnimationHasEnd:(id)sender
{
    [super creatingAnimationHasEnd:sender];
    [self generateNextRandomPoint];
    agentStateType = kStatePatrol;
    [sprite runAction:[CCRepeatForever actionWithAction:[CCRotateBy actionWithDuration:0.3f angle:90.0f]]];
}
//
//
-(void) updateDeffenders
{
    if(defenders.size() == 0)
        flag = kDealloc;
    for(int i =0;i<(int)defenders.size();i++)
    {
        if(defenders[i].flag == kDealloc)
        {
            [defenders[i] releaseAgent];
            [defenders[i] release];
            defenders.erase(defenders.begin()+i);
            return;
        }
        else [defenders[i] update];
    }
}
//
//
-(void) update
{
	[super update];
    [self updateDeffenders];
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
-(void) hit
{
    //
    if(flag == kDying)
        return;
    //
    hitCount -- ;
    if(hitCount <= 0)
    {
        flag = kDying;
        sprite.opacity = 0;
    }
}
//
-(void) bigExploded
{
    if(ccpDistance([[AiInput sharedAiInput] mainCharacterPosition], sprite.position) <= BIG_EXPLOSION_DISTANCE)
    {
        hitCount -=BIG_EXPLOSION_HIT_CONST ;
        if(hitCount <= 0)
        {
            flag = kDying;
            sprite.opacity = 0;
        }
    }
}
//
-(void) releaseAgent
{
    if(hitCount<=0)
    {
        for (int i =0; i<vitaminsCount; i++) {
            CGPoint randomPos = [[VitaminManager sharedVitaminManager] getValidBonusSpawnPointAtPoint:sprite.position];
            [[VitaminManager sharedVitaminManager] createVitaminAtPosition:randomPos withScore:1];
        }
	}
    [super releaseAgent];
    for(int i =0;i<(int)defenders.size();i++)
    {
        [defenders[i] releaseAgent];
        [defenders[i] release];
    }
    defenders.clear();
}
//
@end
