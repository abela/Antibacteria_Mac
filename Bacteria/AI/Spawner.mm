//
//  Spawner.m
//  Bacterium
//
//  Created by Giorgi Abelashvili on 3/6/13.
//
//

#import "Spawner.h"
#import "Loctopus.h"
#import "Adrenalium.h"
#import "RedNoise.h"
#import "SpiralCorosia.h"
#import "BigBlueCorosia.h"
#import "EnemyAgent.h"
#import "EnemyAgentsFactory.h"
#import "Utils.h"
#import "AiInput.h"
//
@interface Spawner (PrivateMethods)
-(void) generateNextSpawn:(NSTimer*)timer;
-(void) updateSpawns;
@end
//
@implementation Spawner
//
-(id) createAtPosition:(CGPoint)position
{
    if((self = [super createAtPosition:position]))
    {
        //
        flag = kDefault;
		agentStateType = kStateWaiting;
		type = kEnemy;
		enemyAgentType = kSpawner;
		speed = 15.0f;
		shootRate = 0.5f;
		shootDistance = 100;
		faceDirection = ccp(0.0f,1.0f);
        hitCount = 10;
        killScore = 160;
        vitaminsCount = 10;
        spawnTime = ([[GameManager sharedGameManager] bulletinTimeIsOn]) ? 1.0f + (1 - [Utils timeScale]) : 1.0f;
        //d
        //
        return self;
    }
    return nil;
}
//
-(void) creatingAnimationHasEnd:(id)sender
{
    [super creatingAnimationHasEnd:sender];
    [super generateNextRandomPoint];
    agentStateType = kStateRandomMovement;
    spawnTimer = [NSTimer scheduledTimerWithTimeInterval:spawnTime
                                                  target:self
                                                selector:@selector(generateNextSpawn:)
                                                userInfo:nil
                                                 repeats:YES];
}
//
-(void) refreshEnemy
{
    if(spawnTimer!=nil)
    {
        [spawnTimer invalidate];
        spawnTimer = nil;
    }
    spawnTime = ([[GameManager sharedGameManager] bulletinTimeIsOn]) ? 0.5f + (1 - [Utils timeScale]) : 0.5f;
    spawnTimer = [NSTimer scheduledTimerWithTimeInterval:spawnTime
                                                  target:self
                                                selector:@selector(generateNextSpawn:)
                                                userInfo:nil
                                                 repeats:YES];
}
//
-(void) update
{
    [super update];
    [self updateSpawns];
}
//
-(void) updateSpawns
{
    //
    for(int i =0;i<(int) spawnuses.size();i++)
    {
        [spawnuses[i] update];
        if(spawnuses[i].flag == kDealloc)
        {
            [spawnuses[i] releaseAgent];
            [spawnuses[i] release];
            spawnuses.erase(spawnuses.begin()+i);
            break;
        }
    }
    //
    if(flag == kDying)
    {
        if (spawnuses.size() == 0) {
            flag = kDealloc;
            return;
        }
    }
    //
}
//
-(void) hit
{
    //
    hitCount -- ;
    if(hitCount <= 0)
    {
        flag = kDealloc;
        glowSprite.opacity = sprite.opacity = 0;
        if(spawnTimer!=nil)
        {
            [spawnTimer invalidate];
            spawnTimer = nil;
        }
    }
    //
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
            glowSprite.opacity = sprite.opacity = 0;
            body->SetActive(false);
            if(spawnTimer!=nil)
            {
                [spawnTimer invalidate];
                spawnTimer = nil;
            }
        }
        //
    }
    
}
//
-(void) generateNextSpawn:(NSTimer*)timer
{
    //
    if([[GameManager sharedGameManager] gameIsPaused])
        return;
    //
    int randomer = arc4random_uniform(4);
    EnemyAgentType agentType;
    switch (randomer) {
        case 0:
            agentType = kLoctopus;
            break;
        case 1:
            agentType = kAdrenalium;
            break;
        case 2:
            agentType = kRedNoise;
            break;
        case 3:
            agentType = kSpiralCorosia;
            break;
        default:
            break;
    }
    //
    EnemyAgent *enemyAgent = [EnemyAgentsFactory getEnemyWithType:agentType withPosition:sprite.position];
    spawnuses.push_back(enemyAgent);
}
//
-(void) releaseAgent
{
    //
    //
    for(int i =0;i<(int) spawnuses.size();i++)
    {
        [spawnuses[i] releaseAgent];
        [spawnuses[i] release];
        spawnuses[i] = nil;
    }
    //
    spawnuses.clear();
    //
    if(spawnTimer!=nil)
    {
        [spawnTimer invalidate];
        spawnTimer = nil;
    }
    //
    for (int i =0; i<vitaminsCount; i++) {
        CGPoint randomPos = [[VitaminManager sharedVitaminManager] getValidBonusSpawnPointAtPoint:sprite.position];
        [[VitaminManager sharedVitaminManager] createVitaminAtPosition:randomPos withScore:1];
    }
    //
    [super releaseAgent];
    //
}
//

//
@end
