//
//  BigRedNoise.m
//  Bacterium
//
//  Created by Giorgi Abelashvili on 3/10/13.
//
//

#import "BigRedNoise.h"
#import "GameResourceManager.h"
#import "RedNoise.h"
#import "Utils.h"
#import "GameManager.h"
#import "EnemyAgentsFactory.h"
//
#define FIRST_CHILD_BORN_LIMIT          300
#define SECOND_CHILD_BORN_LIMIT         200
#define THIRD_CHILD_BORN_LIMIT          100
//
#define WAVE_ENEMIES_COUNT              150
#define FINAL_OPACITY_LIMIT             70
#define MAXIMUM_HIT_COUNT               400
#define MAXIMUM_SPRITE_OPACITY          255
//
const int enemiesWaves[3] =
{
    60,80,90
};
//
@interface BigRedNoise(PrivateMethods)
-(void) updateChildrens;
-(void) bornChilds;
-(void) bornChildTimer:(NSTimer*)timer;
@end
//
@implementation BigRedNoise
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
		enemyAgentType = kBigRedNoise;
		prevSpeed = speed = 12.0f;
        //
        switch ([[GameSettingsManager sharedGameSettingsManager] gameDifficulty]) {
            case kHard:
                prevSpeed = speed = 15.0f;
                break;
            case kImpossible:
                prevSpeed = speed = 18.0f;
                break;
            default:
                break;
        }
        //
		shootRate = 0.5f;
		shootDistance = 100;
		faceDirection = ccp(0.0f,1.0f);
        hitCount = MAXIMUM_HIT_COUNT;
		//
		width = 10.3f;
		height = 10.3f;
        enemiesWavesCounter = 0;
        vitaminsCount = 10;
		//
		sprite = [CCSprite spriteWithSpriteFrameName:@"Enemy1.png"];
        //
		sprite.position = position;
        sprite.scale = glowSprite.scale = 7.0f;
		//
		position.x /= ASPECT_RATIO;
		position.y /= ASPECT_RATIO;
		//
        [super createNewBodyAt:position andDimension:ccp(width,height)];
        //
		[sprite setColor:ccc3(255,0,0)];
        //
        body->GetFixtureList()->SetSensor(true);
        //
        [[[GameResourceManager sharedGameResourceManager] sharedMainCharacterSpriteSheet] addChild:sprite z:4 tag:1];
		//
        CCFadeIn *fadeIn = [CCFadeIn actionWithDuration:1.0f];
        CCCallFunc *fadeInHasDone = [CCCallFunc actionWithTarget:self selector:@selector(creatingAnimationHasEnd:)];
        [sprite runAction:[CCSequence actions:fadeIn,fadeInHasDone, nil]];
        //
        opacityDecreaserStep = (float)sprite.opacity / (float)hitCount;
        //
		return self;
	}
	//
	return nil;
}
//
-(void) creatingAnimationHasEnd:(id)sender
{
    agentStateType = kStatePatrol;
}
//
-(void) bornChilds
{
    //
    //sprite.opacity = 128.0f;
    agentStateType = kStateWaiting;
    [sprite runAction:[CCRepeatForever actionWithAction:[CCRotateBy actionWithDuration:0.1f angle:90.0f]]];
    float timerRate = ([[GameManager sharedGameManager] bulletinTimeIsOn]) ? 0.4f : 0.1f;
    bornChildsTimerValue = [NSTimer scheduledTimerWithTimeInterval:timerRate
                                                            target:self
                                                          selector:@selector(bornChildTimer:)
                                                          userInfo:nil
                                                           repeats:YES];
    bornOfChilds = YES;
}
//
-(void) bornChildTimer:(NSTimer*)timer
{
    //
    if([[GameManager sharedGameManager] gameIsPaused] == YES)
        return;
    //
    //
    RedNoise *redNoise = [[RedNoise alloc] createAtPosition:sprite.position];
    redNoise.body->GetFixtureList()->SetSensor(false);
    redNoise.bornFromBoss = YES;
    childrens.push_back(redNoise);
    if(childrens.size() == enemiesWaves[enemiesWavesCounter])
    {
        redNoise.speed *=1.1f;
        prevSpeed = speed;
        speed/=3.0f;
        bornOfChilds = NO;
        //
        if(bornChildsTimerValue)
        {
            [bornChildsTimerValue invalidate];
            bornChildsTimerValue = nil;
        }
        //
        //sprite.opacity = 255;
        [sprite stopAllActions];
        agentStateType = kStatePatrol;
        enemiesWavesCounter++;
    }
}
//
-(void) update
{
    if(bornOfChilds == NO)
    {
        [super update];
    }
    else body->SetLinearVelocity(b2Vec2(0,0));
    //
    if(flag == kDefault)
    {
        [self updateChildrens];
        if(childrens.size() == 0)
        {
            speed = prevSpeed;
        }
    }
}
//
-(void) updateChildrens
{
    for(int i =0; i<(int)childrens.size();i++)
    {
        if(childrens[i].flag == kDealloc)
        {
            //
            [childrens[i] releaseAgent];
            [childrens[i] release];
            childrens.erase(childrens.begin()+i);
        }
        else [childrens[i] update];
    }
}
//
-(void) refreshEnemy
{
    if(bornOfChilds == YES)
    {
        if(bornChildsTimerValue)
        {
            [bornChildsTimerValue invalidate];
            bornChildsTimerValue = nil;
        }
        //
        float timerRate = ([[GameManager sharedGameManager] bulletinTimeIsOn]) ? 0.4f : 0.1f;
        //
        bornChildsTimerValue = [NSTimer scheduledTimerWithTimeInterval:timerRate
                                                                target:self
                                                              selector:@selector(bornChildTimer:)
                                                              userInfo:nil
                                                               repeats:YES];
    }
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
    //
    if(bornChildsTimerValue)
    {
        [bornChildsTimerValue invalidate];
        bornChildsTimerValue = nil;
    }
    //
    for(int i =0; i<(int)childrens.size();i++)
    {
        [childrens[i] releaseAgent];
        [childrens[i] release];
    }
    childrens.clear();
}
//
-(void) hit
{
    if(bornOfChilds == NO)
    {
        hitCount -- ;
        //
        float spriteOpacity = sprite.opacity;
        spriteOpacity-=opacityDecreaserStep;
        if(sprite.opacity < FINAL_OPACITY_LIMIT)
            spriteOpacity = FINAL_OPACITY_LIMIT;
        sprite.opacity = spriteOpacity;
        //
        //
        flag = (hitCount <= 0) ? kDealloc : kDefault;
        //
        if(flag == kDealloc)
        {
            [[GameManager sharedGameManager] levelHasWonAfterBigBoss];
            return;
        }
        //
        //
        if(hitCount < FIRST_CHILD_BORN_LIMIT && hitCount > SECOND_CHILD_BORN_LIMIT && firstWave == NO)
        {
            [self bornChilds];
            firstWave = YES;
        }
        else if(hitCount < SECOND_CHILD_BORN_LIMIT && hitCount > THIRD_CHILD_BORN_LIMIT && secondWave == NO)
        {
            [self bornChilds];
            secondWave = YES;
        }
        else if(hitCount < THIRD_CHILD_BORN_LIMIT && hitCount > 0 && thirdWave == NO)
        {
            [self bornChilds];
            thirdWave = YES;
        }
        //NSLog(@"decrease op = %d, decrease hit = %d",sprite.opacity,hitCount);
    }
}
@end
