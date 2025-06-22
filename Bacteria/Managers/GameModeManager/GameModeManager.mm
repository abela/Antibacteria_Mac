//
//  ScenarioManager.m
//  AntiBacteria
//
//  Created by Giorgi Abelashvili on 5/9/13.
//
//

#import "GameModeManager.h"
#import "GameMode.h"
#import "BonusManager.h"
#import "VitaminManager.h"
#import "InputManager.h"

GameModeManager *_sharedGameModeManager;

@implementation GameModeManager
//
@synthesize currentGameModeType;
@synthesize currentGameMode;
//
+(GameModeManager*) sharedGameModeManager
{
    //
    @synchronized([GameModeManager class])
    {
        if(!_sharedGameModeManager)
            _sharedGameModeManager = [[GameModeManager alloc] init];
        return _sharedGameModeManager;
    }
    //
}
//
-(id) init
{
    if((self = [super init]))
    {
        //
        currentGameModeType = kCompaignMode;
        return self;
        //
    }
    return nil;
}
//
-(void) Run
{
    //
    NSString *currentGameModeName = @"";
    //
    switch (currentGameModeType) {
        case kCompaignMode:
            currentGameModeName = @"CompaignGameMode";
            break;
        case kTakeTheFlagMode:
            currentGameModeName = @"TakeTheFlagGameMode";
            break;
        case kRushMode:
            currentGameModeName = @"RushGameMode";
            break;
        case kPeaceWalkerMode:
            currentGameModeName = @"PeceWalkerGameMode";
            break;
        case kArmagedonMode:
            currentGameModeName = @"ArmagedonGameMode";
            break;
        default:
            break;
    }
    currentGameMode = [[NSClassFromString(currentGameModeName) alloc] init];
    currentGameMode.gameModeManager = self;
    [currentGameMode Run];
}
//
-(void) setMainCharacter:(MainCharacter*)character
{
    [currentGameMode setMainCharacter:character];
}
//
-(void) updateCharacterInGameMode:(MainCharacter*)character
{
    [currentGameMode setMainCharacter:character];
}
//
-(void) update
{
    [currentGameMode update];
    [[BonusManager sharedBonusManager] update];
    [[VitaminManager sharedVitaminManager] update];
}
//
-(void) refreshGameModeManager
{
    [currentGameMode refreshGameMode];
}
//
-(void) simulateGameModeManagerRestart
{
    [currentGameMode simulateGameModeRestart];
    [[BonusManager sharedBonusManager] releaseBonusManager];
    [[VitaminManager sharedVitaminManager] releaseVitaminManager];
    [[InputManager sharedInputManager] restartInputManager];
}
//
-(void) releaseGameModeManager
{
    [currentGameMode releaseGameMode];
    [[BonusManager sharedBonusManager] releaseBonusManager];
    [currentGameMode release];
    currentGameMode = nil;
    [[VitaminManager sharedVitaminManager] releaseVitaminManager];
}
//
-(void) gameModeActionLoadNextLevel
{
    [currentGameMode gameModeActionLoadNextLevel];
}
//
-(void) gameModeSpecialAction
{
    [currentGameMode gameModeSpecialAction];
}
//
-(void) stopGameModeManagerAfterCharacterDeath
{
    [currentGameMode stopGameModedAfterCharacterDeath];
}
//
-(void) canGenerateBonus:(BOOL) bonusGenerateFlag
{
    currentGameMode.canGenerateBonus = bonusGenerateFlag;
}
//
-(void) gameOverInGameMode
{
    [currentGameMode gameOverInGameMode];
}
//
@end
