//
//  ScenarioManager.h
//  AntiBacteria
//
//  Created by Giorgi Abelashvili on 5/9/13.
//
//

#import <Foundation/Foundation.h>
#import "MainCharacter.h"

@class GameMode;
@interface GameModeManager : NSObject
{
    GameModeType currentGameModeType;
    GameMode *currentGameMode;
}
//
@property (nonatomic,assign) GameModeType currentGameModeType;
@property (nonatomic,retain) GameMode *currentGameMode;
//
+(GameModeManager*) sharedGameModeManager;
-(void) refreshGameModeManager;
-(void) Run;
-(void) setMainCharacter:(MainCharacter*)character;
-(void) update;
-(void) updateCharacterInGameMode:(MainCharacter*)character;
-(void) canGenerateBonus:(BOOL) bonusGenerateFlag;
-(void) simulateGameModeManagerRestart;
-(void) releaseGameModeManager;
-(void) gameModeSpecialAction;
-(void) gameModeActionLoadNextLevel;
-(void) stopGameModeManagerAfterCharacterDeath;
-(void) gameOverInGameMode;
//
@end
