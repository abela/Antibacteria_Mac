//
//  BonusManager.m
//  Bacterium
//
//  Created by Giorgi Abelashvili on 3/30/13.
//
//

#import "BonusManager.h"
#import "MainGameLayer.h"
#import "BonusObject.h"
#import "Flag.h"
#import "PeaceFlag.h"
#import "SpawnManager.h"
#import "RushStation.h"
#import "UsableBonus.h"
//
#define MAXIMUM_BONUS_RAND_CONST        8
//
BonusManager *_sharedBonusManager = nil;
//
@implementation BonusManager
//
@synthesize gameLayer;
@synthesize bonusObjects;
//
+(BonusManager*) sharedBonusManager
{
    @synchronized([BonusManager class])
    {
        if(!_sharedBonusManager)
            _sharedBonusManager = [[BonusManager alloc] init];
        return _sharedBonusManager;
    }
}
//
-(id) init
{
    if((self = [super init]))
    {
        return self;
    }
    return nil;
}
//
-(void) generateLifeBonus
{
    BonusObjectType bonusObjectType = kLifeBonusBonus;
    CGPoint validPos = [[SpawnManager sharedSpawnManager] generateValidBonusSpawn];
    BonusObject *bonusObject = [[UsableBonus alloc] initWithType:bonusObjectType andPosition:validPos];
    [gameLayer addChild:bonusObject.sprite z:10];
    bonusObjects.push_back(bonusObject);
}
//
-(void) generateRandomBonus
{
    BonusObjectType bonusObjectType = (BonusObjectType)(arc4random()%MAXIMUM_BONUS_RAND_CONST);
    CGPoint validPos = [[SpawnManager sharedSpawnManager] generateValidBonusSpawn];
    BonusObject *bonusObject = [[UsableBonus alloc] initWithType:bonusObjectType andPosition:validPos];
    [gameLayer addChild:bonusObject.sprite z:10];
    bonusObjects.push_back(bonusObject);
}
//
-(void) generateBonusWithType:(BonusObjectType)bonusObjectType
{
    CGPoint validPos = [[SpawnManager sharedSpawnManager] generateValidBonusSpawn];
    BonusObject *bonusObject = [[UsableBonus alloc] initWithType:bonusObjectType andPosition:validPos];
    [gameLayer addChild:bonusObject.sprite z:10];
    bonusObjects.push_back(bonusObject);
}
//
-(void) generateFlag
{
    CGPoint validPos = [[SpawnManager sharedSpawnManager] generateValidBonusSpawn];
    BonusObject *flagObject = [[Flag alloc] initWithType:kFlagBonus andPosition:validPos];
    [gameLayer addChild:flagObject.sprite z:10];
    bonusObjects.push_back(flagObject);
}
//
-(void) generateRushStation
{
    CGPoint validPos = [[SpawnManager sharedSpawnManager]
                        getRandomPointAroundPoint:ccp([CCDirector sharedDirector].winSize.width/2.0f,
                                                      [CCDirector sharedDirector].winSize.height/2.0f)];
    //
    RushStation *rushObject = [[RushStation alloc] initWithType:kRushBonus andPosition:validPos];
    [gameLayer addChild:rushObject.sprite z:10];
    [gameLayer addChild:rushObject.lifeProgressTimer z:1000];
    bonusObjects.push_back(rushObject);
    //
}
//
-(void) generateRandomPeaceWalkerBonus
{
    //
    CGPoint validPos = [[SpawnManager sharedSpawnManager]
                        getRandomPointAroundPoint:ccp([CCDirector sharedDirector].winSize.width/2.0f,
                                                      [CCDirector sharedDirector].winSize.height/2.0f)];
    //
    PeaceFlag *rushObject = [[PeaceFlag alloc] initWithType:kPeaceFlagBonus andPosition:validPos];
    [gameLayer addChild:rushObject.sprite z:10];
    bonusObjects.push_back(rushObject);
    //
}
//
-(void) update
{
    for(int i = 0; i < (int)bonusObjects.size(); i++)
    {
        if(bonusObjects[i].flag == kDealloc)
        {
           [bonusObjects[i] releaseAgent];
            bonusObjects.erase(bonusObjects.begin() + i);
        }
        else [bonusObjects[i] update];
    }
}
//
-(void) releaseBonusManager
{
    for(int i = 0; i < (int)bonusObjects.size(); i++)
    {
        [bonusObjects[i] releaseAgent];
    }
    bonusObjects.clear();
}
//
@end
