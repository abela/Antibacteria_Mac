//
//  ArmagedonGameMode.m
//  AntiBacteria
//
//  Created by Giorgi Abelashvili on 5/9/13.
//
//

#import "ArmagedonGameMode.h"
#import "GameResourceManager.h"
#import "Utils.h"
#import "LevelManager.h"
#import "AgentsManager.h"
#import "GameManager.h"
#import "MainCharacter.h"
//
#define ARMAGEDON_BACTERIA_START_COUNT      1
//
@implementation ArmagedonGameMode
//
-(void) Run
{
    //
    mainCharacter.canShoot = YES;
    //
    [[GameResourceManager sharedGameResourceManager] updateWithSlowMotion:NO];
    [[AgentsManager sharedAgentsManager] simulateAgentsManagerRestart];
    //
    for (int i = 0; i<ARMAGEDON_BACTERIA_START_COUNT; i++) {
        [[AgentsManager sharedAgentsManager] createRandomAgentAtPosition:ccp(0,0) :kArmagedonBacteria];
    }
    //
}
//
-(void) stopGameModedAfterCharacterDeath
{
    [[GameResourceManager sharedGameResourceManager] updateWithSlowMotion:NO];
    //bc sprite
    // enemy create timer
    [[AgentsManager sharedAgentsManager] simulateAgentsManagerRestart];
}
//
-(void) setMainCharacter:(MainCharacter *)character
{
    mainCharacter = character;
    mainCharacter.canShoot = YES;
}
//
//
-(void) simulateGameModeRestart
{
    [[GameResourceManager sharedGameResourceManager] updateWithSlowMotion:NO];
    [[AgentsManager sharedAgentsManager] simulateAgentsManagerRestart];

}
//
-(void) update
{
    [[AgentsManager sharedAgentsManager] update];
}
//
-(void) releaseGameMode
{
    //
    [[GameResourceManager sharedGameResourceManager] updateWithSlowMotion:NO];
    // enemy create timer
    [[AgentsManager sharedAgentsManager] simulateAgentsManagerRestart];
}
//
@end
